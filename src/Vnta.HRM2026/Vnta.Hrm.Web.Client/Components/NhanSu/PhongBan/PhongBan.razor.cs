using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.PhongBan {
    public partial class PhongBan : IDisposable {
        readonly CancellationTokenSource disposalTokenSource = new();

        const string SummaryBlockKey = "block";
        const string SummaryDepartmentKey = "department";
        const string SummaryTeamKey = "team";
        const string SummaryGroupKey = "group";
        const string SummaryAllKey = "all";

        [Inject]
        AttendanceDepartmentDataProvider DataProvider { get; set; } = default!;

        [Inject]
        IHrmDialogService DialogService { get; set; } = default!;

        [Inject]
        IHrmToastService ToastService { get; set; } = default!;

        IReadOnlyList<AttendanceDepartmentRecord> Departments { get; set; } = [];
        IReadOnlyList<AttendanceDepartmentTreeNode> DepartmentTreeNodes { get; set; } = [];
        IReadOnlyList<DepartmentSummaryBadge> SummaryBadges { get; set; } = BuildSummaryBadges([]);
        IReadOnlyList<string> BlockOptions { get; set; } = [];
        IReadOnlyList<object> SelectedDataItems { get; set; } = [];
        ITreeList? TreeList { get; set; }
        string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
        string? SearchText { get; set; }
        string? LoadErrorMessage { get; set; }
        bool IsLoading { get; set; }
        bool IsCreatingNewDepartment { get; set; }
        IReadOnlyList<AttendanceDepartmentTreeNode> VisibleDepartmentTreeNodes => FilterTreeNodes(DepartmentTreeNodes, ActiveSummaryBadgeKey);

        bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
        bool CanInteract => !IsLoading && !HasLoadError;
        bool CanCreate => CanInteract;
        bool CanEditSelected => CanInteract && GetSelectedDataNodes().Count == 1;
        bool CanDeleteSelected => CanInteract && GetSelectedDataNodes().Count > 0;
        bool CanExport => !IsLoading && VisibleDepartmentTreeNodes.Count > 0;
        bool CanExportSelected => CanExport && GetSelectedDataNodes().Count > 0;
        string ActiveSummaryBadgeLabel => SummaryBadges
            .FirstOrDefault(badge => string.Equals(badge.Key, ActiveSummaryBadgeKey, StringComparison.Ordinal))
            ?.Label ?? "đã chọn";
        string EmptyStateTitle => !string.IsNullOrWhiteSpace(SearchText)
            ? "Không tìm thấy phòng ban phù hợp"
            : ActiveSummaryBadgeKey == SummaryAllKey
                ? "Chưa có phòng ban"
                : $"Không có dữ liệu {ActiveSummaryBadgeLabel.ToLowerInvariant()}";
        string EmptyStateMessage => !string.IsNullOrWhiteSpace(SearchText)
            ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem lại toàn bộ danh sách phòng ban."
            : ActiveSummaryBadgeKey == SummaryAllKey
                ? "Bắt đầu bằng cách tạo phòng ban đầu tiên hoặc kiểm tra dữ liệu đồng bộ từ gateway."
                : "Hãy chuyển sang nhóm tổ chức khác hoặc quay về tất cả để xem đầy đủ danh sách.";
        string EmptyStateActionText => !string.IsNullOrWhiteSpace(SearchText)
            ? "Xóa tìm kiếm"
            : ActiveSummaryBadgeKey == SummaryAllKey
                ? "Tạo phòng ban"
                : "Xem tất cả";

        protected override async Task OnInitializedAsync() {
            await ReloadAsync();
            await base.OnInitializedAsync();
        }

        async Task ReloadAsync() {
            if(disposalTokenSource.IsCancellationRequested)
                return;

            LoadErrorMessage = null;
            IsLoading = true;

            try {
                await ClearSelectionAsync();
                SetDepartments(await DataProvider.GetAsync(disposalTokenSource.Token));
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;
            } catch(Exception) {
                SetDepartments([]);
                LoadErrorMessage = "Có lỗi khi tải dữ liệu phòng ban. Vui lòng thử lại.";
                ToastService.ShowError("Không thể tải danh sách phòng ban.");
            } finally {
                IsLoading = false;
            }
        }

        Task OnSelectedDataItemsChanged(IReadOnlyList<object> items) {
            SelectedDataItems = items;
            return Task.CompletedTask;
        }

        async Task OnSummaryBadgeClick(string badgeKey) {
            if(string.Equals(badgeKey, ActiveSummaryBadgeKey, StringComparison.Ordinal))
                return;

            ActiveSummaryBadgeKey = badgeKey;
            await ClearSelectionAsync();
        }

        Task OnSearchTextChanged(string? value) {
            SearchText = string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
            return Task.CompletedTask;
        }

        async Task OnEmptyStateActionClick() {
            if(!string.IsNullOrWhiteSpace(SearchText)) {
                SearchText = null;
                return;
            }

            if(ActiveSummaryBadgeKey != SummaryAllKey) {
                ActiveSummaryBadgeKey = SummaryAllKey;
                await ClearSelectionAsync();
                return;
            }

            await OnAddDepartmentClick();
        }

        async Task OnAddDepartmentClick() {
            if(!CanCreate || TreeList is null)
                return;

            await TreeList.StartEditNewRowAsync(nameof(AttendanceDepartmentTreeNode.BlockName));
        }

        async Task OnEditDepartmentClick() {
            if(TreeList is null)
                return;

            var node = GetSingleSelectedDataNode();
            if(node is null) {
                ToastService.ShowWarning("Hãy chọn đúng một dòng phòng ban để điều chỉnh.");
                return;
            }

            var focusedRowIndex = TreeList.GetFocusedRowIndex();
            if(focusedRowIndex < 0) {
                ToastService.ShowWarning("Hãy chọn dòng phòng ban cần điều chỉnh.");
                return;
            }

            await TreeList.StartEditRowAsync(focusedRowIndex, nameof(AttendanceDepartmentTreeNode.BlockName));
        }

        async Task OnCancelDepartmentEditClick() {
            if(TreeList is not null)
                await TreeList.CancelEditAsync();
        }

        async Task OnDeleteDepartmentsClick() {
            var selectedNodes = GetSelectedDataNodes();
            if(selectedNodes.Count == 0) {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng phòng ban để xóa.");
                return;
            }

            var confirmed = await DialogService.ConfirmAsync(
                selectedNodes.Count == 1
                    ? $"Bạn có chắc muốn xóa phòng ban `{selectedNodes[0].Code}`?"
                    : $"Bạn có chắc muốn xóa {selectedNodes.Count} phòng ban đã chọn?",
                title: "Xác nhận xóa",
                okText: "Xóa",
                cancelText: "Hủy",
                renderStyle: MessageBoxRenderStyle.Danger);

            if(!confirmed)
                return;

            try {
                SetDepartments(await DataProvider.DeleteAsync(
                    selectedNodes.Select(node => node.DepartmentId!.Value),
                    disposalTokenSource.Token));
                await ClearSelectionAsync();
                ToastService.ShowSuccess("Đã xóa phòng ban đã chọn.");
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;
            } catch(Exception) {
                ToastService.ShowError("Không thể xóa phòng ban đã chọn.");
            }
        }

        void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => TreeList?.ShowColumnChooser();

        Task ExportAllDataToExcel() => ExportAsync(
            () => TreeList!.ExportToXlsxAsync("phong-ban"),
            "Đã bắt đầu xuất Excel.");

        Task ExportSelectedRowsToExcel() => ExportAsync(
            () => TreeList!.ExportToXlsxAsync(
                "phong-ban-da-chon",
                new TreeListXlExportOptions { ExportSelectedRowsOnly = true }),
            "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

        Task ExportAllDataToPdf() => ExportAsync(
            () => TreeList!.ExportToPdfAsync("phong-ban"),
            "Đã bắt đầu xuất PDF.");

        Task ExportSelectedRowsToPdf() => ExportAsync(
            () => TreeList!.ExportToPdfAsync(
                "phong-ban-da-chon",
                new TreeListPdfExportOptions { ExportSelectedRowsOnly = true }),
            "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

        async Task ExportAsync(Func<Task> exportAction, string successMessage) {
            if(TreeList is null) {
                ToastService.ShowWarning("Cây dữ liệu chưa sẵn sàng để xuất.");
                return;
            }

            try {
                await exportAction();
                ToastService.ShowInfo(successMessage);
            } catch(Exception) {
                ToastService.ShowError("Không thể xuất dữ liệu phòng ban.");
            }
        }

        void OnCustomizeEditModel(TreeListCustomizeEditModelEventArgs e) {
            IsCreatingNewDepartment = e.IsNew;
            var model = (AttendanceDepartmentTreeNode)e.EditModel;

            if(e.IsNew) {
                InitializeNewDepartmentDefaults(model);
                EnsureInternalCode(model, isNew: true);
                return;
            }

            if(e.DataItem is AttendanceDepartmentTreeNode source) {
                CopyNode(source, model);
                EnsureInternalCode(model, isNew: false);
            }
        }

        async Task OnEditModelSaving(TreeListEditModelSavingEventArgs e) {
            try {
                var editModel = (AttendanceDepartmentTreeNode)e.EditModel;
                NormalizeEditModel(editModel);
                EnsureInternalCode(editModel, e.IsNew);

                var now = DateTime.UtcNow;
                if(editModel.CreatedAtUtc == default)
                    editModel.CreatedAtUtc = now;
                editModel.UpdatedAtUtc = now;

                var record = MapRecord(editModel);
                var validationMessage = await DataProvider.ValidateAsync(record, disposalTokenSource.Token);
                if(!string.IsNullOrWhiteSpace(validationMessage)) {
                    e.Cancel = true;
                    ToastService.ShowWarning(validationMessage);
                    return;
                }

                SetDepartments(await DataProvider.SaveAsync(record, e.IsNew, disposalTokenSource.Token));
                e.Reload = false;
                await ClearSelectionAsync();
                ToastService.ShowSuccess(e.IsNew ? "Đã thêm phòng ban." : "Đã cập nhật phòng ban.");
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;

                e.Cancel = true;
            } catch(InvalidOperationException ex) {
                e.Cancel = true;
                ToastService.ShowError(ex.Message);
            } catch(Exception) {
                e.Cancel = true;
                ToastService.ShowError("Không thể lưu dữ liệu phòng ban. Vui lòng thử lại.");
            }
        }

        async Task ClearSelectionAsync() {
            SelectedDataItems = [];

            if(TreeList is null)
                return;

            await TreeList.DeselectAllAsync();
            TreeList.SetFocusedRowIndex(-1);
        }

        List<AttendanceDepartmentTreeNode> GetSelectedDataNodes() =>
            SelectedDataItems
                .OfType<AttendanceDepartmentTreeNode>()
                .Where(node => node.IsDataNode)
                .ToList();

        AttendanceDepartmentTreeNode? GetSingleSelectedDataNode() {
            var selectedNodes = GetSelectedDataNodes();
            return selectedNodes.Count == 1 ? selectedNodes[0] : null;
        }

        void SetDepartments(IReadOnlyList<AttendanceDepartmentRecord> departments) {
            Departments = departments;
            DepartmentTreeNodes = BuildTreeNodes(departments);
            SummaryBadges = BuildSummaryBadges(DepartmentTreeNodes);
            BlockOptions = BuildBlockOptions(departments);
        }

        static IReadOnlyList<string> BuildBlockOptions(IReadOnlyList<AttendanceDepartmentRecord> departments) =>
            departments
                .Select(department => NormalizeNullable(department.CenterName))
                .Where(blockName => blockName is not null)
                .Select(blockName => blockName!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(blockName => blockName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        static IReadOnlyList<DepartmentSummaryBadge> BuildSummaryBadges(IReadOnlyList<AttendanceDepartmentTreeNode> nodes) {
            return
            [
                new(SummaryBlockKey, "Khối", nodes.Count(IsBlockSummaryNode)),
                new(SummaryDepartmentKey, "Phòng ban", nodes.Count(IsDepartmentSummaryNode)),
                new(SummaryTeamKey, "Tổ", nodes.Count(IsTeamSummaryNode)),
                new(SummaryGroupKey, "Nhóm", nodes.Count(IsGroupLevelDataNode)),
                new(SummaryAllKey, "Tất cả", nodes.Count(node => node.IsDataNode))
            ];
        }

        static IReadOnlyList<AttendanceDepartmentTreeNode> FilterTreeNodes(
            IReadOnlyList<AttendanceDepartmentTreeNode> nodes,
            string badgeKey) {
            if(nodes.Count == 0 || string.Equals(badgeKey, SummaryAllKey, StringComparison.Ordinal))
                return nodes;

            var matchingNodes = nodes.Where(node => MatchesSummaryBadge(node, badgeKey)).ToArray();
            if(matchingNodes.Length == 0)
                return [];

            var nodeIndex = nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
            var visibleNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach(var matchingNode in matchingNodes) {
                AttendanceDepartmentTreeNode? currentNode = matchingNode;

                while(currentNode is not null) {
                    if(!visibleNodeIds.Add(currentNode.Id))
                        break;

                    currentNode = currentNode.ParentId is not null && nodeIndex.TryGetValue(currentNode.ParentId, out var parentNode)
                        ? parentNode
                        : null;
                }
            }

            return nodes
                .Where(node => visibleNodeIds.Contains(node.Id))
                .ToArray();
        }

        static bool MatchesSummaryBadge(AttendanceDepartmentTreeNode node, string badgeKey) {
            return badgeKey switch {
                SummaryBlockKey => IsBlockSummaryNode(node),
                SummaryDepartmentKey => IsDepartmentSummaryNode(node) || IsDepartmentLevelDataNode(node),
                SummaryTeamKey => IsTeamSummaryNode(node) || IsTeamLevelDataNode(node),
                SummaryGroupKey => IsGroupLevelDataNode(node),
                _ => true
            };
        }

        static bool IsBlockSummaryNode(AttendanceDepartmentTreeNode node) =>
            !node.IsDataNode
            && !string.IsNullOrWhiteSpace(node.BlockName)
            && string.IsNullOrWhiteSpace(node.DepartmentName)
            && string.IsNullOrWhiteSpace(node.TeamName)
            && string.IsNullOrWhiteSpace(node.GroupName);

        static bool IsDepartmentSummaryNode(AttendanceDepartmentTreeNode node) =>
            !node.IsDataNode
            && !string.IsNullOrWhiteSpace(node.DepartmentName)
            && string.IsNullOrWhiteSpace(node.TeamName)
            && string.IsNullOrWhiteSpace(node.GroupName);

        static bool IsTeamSummaryNode(AttendanceDepartmentTreeNode node) =>
            !node.IsDataNode
            && !string.IsNullOrWhiteSpace(node.TeamName)
            && string.IsNullOrWhiteSpace(node.GroupName);

        static bool IsDepartmentLevelDataNode(AttendanceDepartmentTreeNode node) =>
            node.IsDataNode
            && string.IsNullOrWhiteSpace(node.TeamName)
            && string.IsNullOrWhiteSpace(node.GroupName);

        static bool IsTeamLevelDataNode(AttendanceDepartmentTreeNode node) =>
            node.IsDataNode
            && !string.IsNullOrWhiteSpace(node.TeamName)
            && string.IsNullOrWhiteSpace(node.GroupName);

        static bool IsGroupLevelDataNode(AttendanceDepartmentTreeNode node) =>
            node.IsDataNode && !string.IsNullOrWhiteSpace(node.GroupName);

        static IReadOnlyList<AttendanceDepartmentTreeNode> BuildTreeNodes(IReadOnlyList<AttendanceDepartmentRecord> departments) {
            var nodes = new List<AttendanceDepartmentTreeNode>();
            var nodeIndex = new Dictionary<string, AttendanceDepartmentTreeNode>(StringComparer.OrdinalIgnoreCase);

            foreach(var department in departments) {
                var blockName = NormalizeNullable(department.CenterName) ?? "(Chưa có khối)";
                var departmentName = NormalizeNullable(department.DepartmentOrWorkshopName) ?? "(Chưa có phòng ban)";
                var teamName = NormalizeNullable(department.TeamName);
                var groupName = NormalizeNullable(department.GroupName);

                var blockNodeId = BuildNodeId("block", blockName);
                var blockNode = GetOrCreateNode(
                    nodes,
                    nodeIndex,
                    blockNodeId,
                    parentNodeId: null,
                    blockName: blockName,
                    departmentName: null,
                    teamName: null,
                    groupName: null);

                var departmentNodeId = BuildNodeId("department", blockName, departmentName);
                var departmentNode = GetOrCreateNode(
                    nodes,
                    nodeIndex,
                    departmentNodeId,
                    blockNodeId,
                    blockName: blockName,
                    departmentName: departmentName,
                    teamName: null,
                    groupName: null);

                var parentNodeId = departmentNodeId;
                AttendanceDepartmentTreeNode parentNode = departmentNode;

                if(!string.IsNullOrWhiteSpace(teamName)) {
                    var teamNodeId = BuildNodeId("team", blockName, departmentName, teamName);
                    parentNode = GetOrCreateNode(
                        nodes,
                        nodeIndex,
                        teamNodeId,
                        departmentNodeId,
                        blockName: blockName,
                        departmentName: departmentName,
                        teamName: teamName,
                        groupName: null);
                    parentNodeId = teamNodeId;
                }

                var dataNode = new AttendanceDepartmentTreeNode {
                    Id = $"department-row:{department.Id:N}",
                    ParentId = parentNodeId,
                    DepartmentId = department.Id,
                    Code = department.Code,
                    BlockName = blockName,
                    DepartmentName = departmentName,
                    TeamName = teamName,
                    GroupName = groupName,
                    Notes = department.Notes,
                    EmployeeCount = department.EmployeeCount,
                    Status = department.Status,
                    StatusText = FormatStatusText(department.Status),
                    CreatedAtUtc = department.CreatedAtUtc,
                    UpdatedAtUtc = department.UpdatedAtUtc
                };

                nodes.Add(dataNode);
                foreach(var aggregateNode in new[] { blockNode, departmentNode, parentNode }.Distinct()) {
                    AddEmployeeCount(aggregateNode, department.EmployeeCount);
                }
            }

            return nodes;
        }

        static AttendanceDepartmentTreeNode GetOrCreateNode(
            List<AttendanceDepartmentTreeNode> nodes,
            Dictionary<string, AttendanceDepartmentTreeNode> nodeIndex,
            string nodeId,
            string? parentNodeId,
            string? blockName,
            string? departmentName,
            string? teamName,
            string? groupName) {
            if(nodeIndex.TryGetValue(nodeId, out var existingNode))
                return existingNode;

            var node = new AttendanceDepartmentTreeNode {
                Id = nodeId,
                ParentId = parentNodeId,
                BlockName = blockName,
                DepartmentName = departmentName,
                TeamName = teamName,
                GroupName = groupName,
                StatusText = "Tổng hợp"
            };

            nodes.Add(node);
            nodeIndex[nodeId] = node;
            return node;
        }

        static void AddEmployeeCount(AttendanceDepartmentTreeNode node, int employeeCount) =>
            node.EmployeeCount += employeeCount;

        static void InitializeNewDepartmentDefaults(AttendanceDepartmentTreeNode model) {
            var utcNow = DateTime.UtcNow;

            model.Id = $"new:{Guid.NewGuid():N}";
            model.ParentId = null;
            model.DepartmentId = Guid.NewGuid();
            model.Code = string.Empty;
            model.BlockName = string.Empty;
            model.DepartmentName = string.Empty;
            model.TeamName = null;
            model.GroupName = null;
            model.Notes = null;
            model.EmployeeCount = 0;
            model.Status = 0;
            model.StatusText = FormatStatusText(model.Status);
            model.CreatedAtUtc = utcNow;
            model.UpdatedAtUtc = utcNow;
        }

        static void CopyNode(AttendanceDepartmentTreeNode source, AttendanceDepartmentTreeNode target) {
            target.Id = source.Id;
            target.ParentId = source.ParentId;
            target.DepartmentId = source.DepartmentId;
            target.Code = source.Code;
            target.BlockName = source.BlockName;
            target.DepartmentName = source.DepartmentName;
            target.TeamName = source.TeamName;
            target.GroupName = source.GroupName;
            target.Notes = source.Notes;
            target.EmployeeCount = source.EmployeeCount;
            target.Status = source.Status;
            target.StatusText = source.StatusText;
            target.CreatedAtUtc = source.CreatedAtUtc;
            target.UpdatedAtUtc = source.UpdatedAtUtc;
        }

        static void NormalizeEditModel(AttendanceDepartmentTreeNode model) {
            model.Code = NormalizeNullable(model.Code);
            model.BlockName = NormalizeNullable(model.BlockName);
            model.DepartmentName = NormalizeNullable(model.DepartmentName);
            model.TeamName = NormalizeNullable(model.TeamName);
            model.GroupName = NormalizeNullable(model.GroupName);
            model.Notes = NormalizeNullable(model.Notes);
            model.StatusText = FormatStatusText(model.Status);
        }

        static void EnsureInternalCode(AttendanceDepartmentTreeNode model, bool isNew) {
            if(!isNew && !string.IsNullOrWhiteSpace(model.Code))
                return;

            var departmentId = model.DepartmentId ?? Guid.NewGuid();
            model.DepartmentId = departmentId;
            model.Code = BuildInternalCode(model, departmentId);
        }

        static string BuildInternalCode(AttendanceDepartmentTreeNode model, Guid departmentId) {
            var source = FirstNonEmpty(model.GroupName, model.TeamName, model.DepartmentName);
            if(string.IsNullOrWhiteSpace(source))
                source = "DEPARTMENT";

            var normalized = new string(source
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());

            if(normalized.Length == 0)
                normalized = "DEPARTMENT";

            if(normalized.Length > 37)
                normalized = normalized[..37];

            var suffix = departmentId.ToString("N")[..8].ToUpperInvariant();
            return $"DEP-{normalized}-{suffix}";
        }

        static AttendanceDepartmentRecord MapRecord(AttendanceDepartmentTreeNode node) =>
            new() {
                Id = node.DepartmentId ?? Guid.NewGuid(),
                Code = node.Code,
                CenterName = node.BlockName,
                DepartmentOrWorkshopName = node.DepartmentName,
                TeamName = node.TeamName,
                GroupName = node.GroupName,
                Notes = node.Notes,
                Name = FirstNonEmpty(node.GroupName, node.TeamName, node.DepartmentName),
                FullPath = string.Join(
                    " / ",
                    new[] {
                        node.BlockName,
                        node.DepartmentName,
                        node.TeamName,
                        node.GroupName
                    }.Where(x => !string.IsNullOrWhiteSpace(x))),
                EmployeeCount = node.EmployeeCount,
                Status = node.Status,
                CreatedAtUtc = node.CreatedAtUtc,
                UpdatedAtUtc = node.UpdatedAtUtc
            };

        static string FormatStatusText(int status) => $"Nguồn {status}";

        static string FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        static string BuildNodeId(string prefix, params string?[] values) =>
            $"{prefix}:{string.Join("|", values.Select(value => NormalizeNullable(value)?.ToUpperInvariant() ?? string.Empty))}";

        static string? NormalizeNullable(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public void Dispose() {
            disposalTokenSource.Cancel();
            disposalTokenSource.Dispose();
        }

        sealed record DepartmentSummaryBadge(string Key, string Label, int Count);
    }
}
