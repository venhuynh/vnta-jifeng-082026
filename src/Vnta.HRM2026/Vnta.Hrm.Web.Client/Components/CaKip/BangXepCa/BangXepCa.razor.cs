using System.Globalization;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.CaKip.BangXepCa;

public partial class BangXepCa : IDisposable
{
    private const int MaximumVisibleDays = 31;
    private const int ActiveShiftStatus = 1;
    private const int EmployeeClassificationType = 5;
    private const int RangeScopeMode = 2;
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private IAttendanceShiftAssignmentReadService ShiftAssignmentReadService { get; set; } = default!;

    [Inject]
    private IAttendanceShiftAssignmentEnsureService ShiftAssignmentEnsureService { get; set; } = default!;

    [Inject]
    private IInteractiveAuditCommandScopeFactory AuditCommandScopeFactory { get; set; } = default!;

    [Inject]
    private IShiftSchedulingSettingService ShiftSchedulingSettingService { get; set; } = default!;

    [Inject]
    private IAttendanceShiftService ShiftService { get; set; } = default!;

    private IGrid? Grid { get; set; }
    private DateTime? FromDate { get; set; }
    private DateTime? ToDate { get; set; }
    private string? searchText;
    private string? SearchText
    {
        get => searchText;
        set
        {
            if (string.Equals(searchText, value, StringComparison.Ordinal))
            {
                return;
            }

            searchText = value;
            RefreshVisibleRows();
        }
    }

    private IReadOnlyList<ShiftRosterDateColumn> DateColumns { get; set; } = [];
    private IReadOnlyList<ShiftRosterMatrixRow> Rows { get; set; } = [];
    private IReadOnlyList<ShiftRosterMatrixRow> VisibleRows { get; set; } = [];
    private string? LoadErrorMessage { get; set; }
    private string? InfoMessage { get; set; }
    private int VisibleAssignmentCount { get; set; }
    private bool HasRequestedLoad { get; set; }
    private bool IsLoading { get; set; }
    private bool IsSyncingShiftAssignments { get; set; }
    private bool IsShiftEditPopupVisible { get; set; }
    private bool IsShiftEditBusy { get; set; }
    private string? ShiftEditErrorMessage { get; set; }
    private ShiftEditState? ShiftEditModel { get; set; }
    private Guid? ShiftEditSelectedShiftId { get; set; }
    private DateTime? ShiftEditFromDate { get; set; }
    private DateTime? ShiftEditToDate { get; set; }
    private IReadOnlyList<ShiftEditOption> ShiftEditOptions { get; set; } = [];

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchText);
    private bool IsBusy => IsLoading || IsSyncingShiftAssignments;
    private bool CanChangeFilters => !IsBusy;
    private bool CanView => !IsBusy;
    private bool CanSync => !IsBusy;
    private bool CanOperateOnCurrentDataset => !IsBusy && HasRequestedLoad;
    private bool CanExport => CanOperateOnCurrentDataset && VisibleRows.Count > 0;
    private bool CanSaveShiftEdit => !IsShiftEditBusy
        && ShiftEditModel is not null
        && ShiftEditSelectedShiftId.HasValue
        && ShiftEditSelectedShiftId.Value != Guid.Empty
        && ShiftEditFromDate.HasValue
        && ShiftEditToDate.HasValue;
    private string LoadingText => IsSyncingShiftAssignments
        ? "Đang đồng bộ ca làm việc..."
        : "Đang chuẩn bị khung bảng xếp ca...";
    private string ShiftEditLoadingText => IsShiftEditBusy
        ? "Dang luu thay doi ca lam viec..."
        : "Dang chuan bi danh sach ca...";
    private string CurrentRangeText => BuildCurrentRangeText();
    private string DataSummaryText => BuildDataSummaryText();
    private string EmptyStateTitle => HasActiveSearch && HasRequestedLoad && Rows.Count > 0
        ? "Không tìm thấy kết quả"
        : HasRequestedLoad
            ? "Chưa có dữ liệu xếp ca"
            : "Bảng xếp ca đã sẵn sàng";
    private string EmptyStateMessage => HasActiveSearch && HasRequestedLoad && Rows.Count > 0
        ? "Không có nhân viên, phòng ban hoặc ca làm nào khớp với từ khóa tìm kiếm."
        : HasRequestedLoad
            ? "Không tìm thấy dữ liệu xếp ca trong khoảng ngày đã chọn."
            : "Chọn khoảng ngày rồi bấm Xem để tạo ma trận cột ngày cho bảng xếp ca.";

    protected override void OnInitialized()
    {
        var (defaultFromDate, defaultToDate) = CreateDefaultRange(DateTime.Today);
        FromDate = defaultFromDate;
        ToDate = defaultToDate;
        DateColumns = BuildDateColumns(
            DateOnly.FromDateTime(defaultFromDate),
            DateOnly.FromDateTime(defaultToDate));
    }

    private async Task LoadRosterAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (!TryGetNormalizedDateRange(out var fromDate, out var toDate, out var errorMessage))
        {
            ToastService.ShowWarning(errorMessage);
            return;
        }

        LoadErrorMessage = null;
        InfoMessage = null;
        IsLoading = true;

        try
        {
            var snapshot = await ShiftAssignmentReadService.GetRosterAsync(
                new AttendanceShiftRosterFilter(fromDate, toDate),
                disposalTokenSource.Token);

            DateColumns = snapshot.Columns
                .Select(column => new ShiftRosterDateColumn(
                    column.WorkDate,
                    column.HeaderText,
                    column.WeekdayText,
                    column.IsSunday))
                .ToArray();
            Rows = snapshot.Rows
                .Select(MapRow)
                .ToArray();
            RefreshVisibleRows();
            HasRequestedLoad = true;
            InfoMessage = Rows.Count == 0
                ? "Khoảng ngày đã chọn hiện chưa có dữ liệu xếp ca để hiển thị."
                : null;
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            LoadErrorMessage = "Có lỗi khi chuẩn bị khung Bảng xếp ca. Vui lòng thử lại.";
            ToastService.ShowError("Không thể chuẩn bị khung Bảng xếp ca.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(BuildExportFileName()),
        "Đã bắt đầu xuất Excel bảng xếp ca.");

    private Task ExportAllDataToCsv() => ExportAsync(
        () => Grid!.ExportToCsvAsync(BuildExportFileName()),
        "Đã bắt đầu xuất CSV bảng xếp ca.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(BuildExportFileName()),
        "Đã bắt đầu xuất PDF bảng xếp ca.");

    private async Task OnSyncShiftAssignmentsClick()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (!TryGetNormalizedDateRange(out var fromDate, out var toDate, out var errorMessage))
        {
            ToastService.ShowWarning(errorMessage);
            return;
        }

        LoadErrorMessage = null;
        InfoMessage = null;
        IsSyncingShiftAssignments = true;

        try
        {
            var ensureRequest = new AttendanceShiftAssignmentEnsureRequest(
                fromDate,
                toDate,
                "ShiftRosterManualSync");
            var result = await AuditCommandScopeFactory.ExecuteAsync(
                AuditActions.ShiftAssignment.BatchGenerate,
                token => ShiftAssignmentEnsureService.EnsureFromSchedulingSettingsAsync(
                    ensureRequest,
                    token),
                captureMode: AuditCaptureMode.OperationOnly,
                cancellationToken: disposalTokenSource.Token);

            if (result.Issues.Count > 0)
            {
                InfoMessage = BuildEnsureIssueMessage(result);
                ToastService.ShowWarning("Chưa thể đồng bộ ca. Hãy kiểm tra cấu hình xếp ca.");
                return;
            }

            ToastService.ShowSuccess(BuildEnsureSuccessMessage(result));
            await LoadRosterAsync();
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            InfoMessage = ex.Message;
            ToastService.ShowWarning("Chưa thể đồng bộ ca.");
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể đồng bộ ca làm việc.");
        }
        finally
        {
            IsSyncingShiftAssignments = false;
        }
    }

    private Task OnSearchTextChanged(string? value)
    {
        SearchText = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return Task.CompletedTask;
    }

    private Task OnColumnChooserRequested()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private string GetColumnName(ShiftRosterDateColumn column) =>
        $"work-date-{column.WorkDate:yyyyMMdd}";

    private string GetHeaderCssClass(ShiftRosterDateColumn column) => column.IsSunday
        ? "shift-roster-column-header shift-roster-column-header-sunday"
        : "shift-roster-column-header";

    private string GetCellCssClass(ShiftRosterDateColumn column, ShiftRosterCell? cell)
    {
        var classes = new List<string> { "shift-roster-cell" };

        if (column.IsSunday)
        {
            classes.Add("shift-roster-cell-sunday");
        }

        if (cell is null)
        {
            classes.Add("shift-roster-cell-empty");
        }
        else
        {
            classes.Add("shift-roster-cell-filled");
            if (cell.HasConflict)
            {
                classes.Add("shift-roster-cell-conflict");
            }
        }

        return string.Join(" ", classes);
    }

    private string GetCellDisplayText(ShiftRosterCell? cell) =>
        string.IsNullOrWhiteSpace(cell?.DisplayText) ? "--" : cell.DisplayText!;

    private string? BuildCellCssVariable(ShiftRosterCell? cell)
    {
        if (cell is null || !TryNormalizeHexColor(cell.ColorHex, out var textColor))
        {
            return null;
        }

        return $"--shift-roster-cell-text-color: {textColor};";
    }

    private static ShiftRosterCell? GetCell(ShiftRosterMatrixRow row, DateOnly workDate) =>
        row.Cells.TryGetValue(workDate, out var cell) ? cell : null;

    private string BuildCellTitle(
        ShiftRosterMatrixRow row,
        ShiftRosterDateColumn column,
        ShiftRosterCell? cell)
    {
        var shiftText = GetCellDisplayText(cell);
        var creationTypeText = string.IsNullOrWhiteSpace(cell?.CreationType)
            ? null
            : $"Nguon: {cell.CreationType}";
        var conflictText = cell?.HasConflict == true
            ? "O nay dang co nhieu ca, chua cho phep sua truc tiep."
            : "Bam de doi ca lam viec.";

        return string.Join(
            " | ",
            new[]
            {
                row.EmployeeDisplay,
                column.WorkDate.ToString("dd/MM/yyyy", DisplayCulture),
                $"Ca: {shiftText}",
                creationTypeText,
                conflictText
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private bool IsCellEditDisabled(ShiftRosterCell? cell) =>
        IsBusy || IsShiftEditBusy || cell?.HasConflict == true;

    private async Task OpenShiftEditPopupAsync(
        ShiftRosterMatrixRow row,
        ShiftRosterDateColumn column,
        ShiftRosterCell? cell)
    {
        if (disposalTokenSource.IsCancellationRequested || IsCellEditDisabled(cell))
        {
            return;
        }

        ShiftEditErrorMessage = null;
        ShiftEditModel = new ShiftEditState
        {
            EmployeeId = row.EmployeeId,
            EmployeeDisplay = row.EmployeeDisplay,
            WorkDate = column.WorkDate,
            WorkDateText = column.WorkDate.ToString("dd/MM/yyyy", DisplayCulture),
            CurrentShiftText = GetCellDisplayText(cell)
        };
        ShiftEditSelectedShiftId = cell?.ShiftId;
        ShiftEditFromDate = column.WorkDate.ToDateTime(TimeOnly.MinValue);
        ShiftEditToDate = column.WorkDate.ToDateTime(TimeOnly.MinValue);
        IsShiftEditBusy = true;
        IsShiftEditPopupVisible = true;

        try
        {
            await InvokeAsync(StateHasChanged);

            if (ShiftEditOptions.Count == 0)
            {
                var shifts = await ShiftService.GetAsync(disposalTokenSource.Token);
                ShiftEditOptions = shifts
                    .Where(shift => shift.Status == ActiveShiftStatus)
                    .OrderBy(shift => shift.Code, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(shift => shift.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(shift => new ShiftEditOption(
                        shift.Id,
                        BuildShiftOptionText(shift)))
                    .ToArray();

                if (ShiftEditOptions.Count == 0)
                {
                    ShiftEditErrorMessage = "Chua co ca lam viec dang hoat dong de lua chon.";
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            ShiftEditErrorMessage = "Khong the chuan bi danh sach ca de doi.";
            ToastService.ShowError("Khong the mo popup doi ca.");
        }
        finally
        {
            IsShiftEditBusy = false;
        }
    }

    private void OnShiftEditPopupVisibleChanged(bool visible)
    {
        if (visible)
        {
            IsShiftEditPopupVisible = true;
            return;
        }

        if (!IsShiftEditBusy)
        {
            CloseShiftEditPopup();
        }
    }

    private void CloseShiftEditPopup()
    {
        if (IsShiftEditBusy)
        {
            return;
        }

        IsShiftEditPopupVisible = false;
        ShiftEditErrorMessage = null;
        ShiftEditModel = null;
        ShiftEditSelectedShiftId = null;
        ShiftEditFromDate = null;
        ShiftEditToDate = null;
    }

    private Task OnShiftEditSelectedShiftChanged(Guid? shiftId)
    {
        ShiftEditSelectedShiftId = shiftId;
        return Task.CompletedTask;
    }

    private Task OnShiftEditFromDateChanged(DateTime? date)
    {
        ShiftEditFromDate = date;
        return Task.CompletedTask;
    }

    private Task OnShiftEditToDateChanged(DateTime? date)
    {
        ShiftEditToDate = date;
        return Task.CompletedTask;
    }

    private async Task SaveShiftEditAsync()
    {
        if (disposalTokenSource.IsCancellationRequested || ShiftEditModel is null || !CanSaveShiftEdit)
        {
            return;
        }

        ShiftEditErrorMessage = null;
        IsShiftEditBusy = true;

        try
        {
            if (!TryGetNormalizedShiftEditDateRange(out var effectiveFromDate, out var effectiveToDate, out var shiftEditError))
            {
                ShiftEditErrorMessage = shiftEditError;
                return;
            }

            var existingSettings = await ShiftSchedulingSettingService.GetAsync(disposalTokenSource.Token);
            var existingSetting = existingSettings.FirstOrDefault(setting =>
                setting.ClassificationType == EmployeeClassificationType
                && setting.AssignmentScopeMode == RangeScopeMode
                && string.Equals(setting.Value, ShiftEditModel.EmployeeDisplay, StringComparison.OrdinalIgnoreCase)
                && setting.EffectiveFromDate == effectiveFromDate
                && setting.EffectiveToDate == effectiveToDate);

            await ShiftSchedulingSettingService.SaveAsync(
                new UpsertShiftSchedulingSettingRequest
                {
                    Id = existingSetting?.Id ?? Guid.NewGuid(),
                    ShiftId = ShiftEditSelectedShiftId!.Value,
                    ClassificationType = EmployeeClassificationType,
                    Value = ShiftEditModel.EmployeeDisplay,
                    AssignmentScopeMode = RangeScopeMode,
                    EffectiveFromDate = effectiveFromDate,
                    EffectiveToDate = effectiveToDate,
                    IsActive = true,
                    CreatedAtUtc = existingSetting?.CreatedAtUtc ?? DateTime.UtcNow,
                    UpdatedAtUtc = existingSetting?.UpdatedAtUtc
                },
                existingSetting is null,
                disposalTokenSource.Token);

            var ensureRequest = new AttendanceShiftAssignmentEnsureRequest(
                effectiveFromDate,
                effectiveToDate,
                "ShiftRosterPopupEdit");
            var ensureResult = await AuditCommandScopeFactory.ExecuteAsync(
                AuditActions.ShiftAssignment.BatchGenerate,
                token => ShiftAssignmentEnsureService.EnsureFromSchedulingSettingsAsync(
                    ensureRequest,
                    token),
                captureMode: AuditCaptureMode.OperationOnly,
                cancellationToken: disposalTokenSource.Token);

            if (ensureResult.Issues.Count > 0)
            {
                ShiftEditErrorMessage = BuildEnsureIssueMessage(ensureResult);
                ToastService.ShowWarning("Đã lưu cài đặt nhưng chưa thể đồng bộ ca.");
                return;
            }

            IsShiftEditBusy = false;
            CloseShiftEditPopup();
            ToastService.ShowSuccess(BuildEnsureSuccessMessage(ensureResult));
            await LoadRosterAsync();
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            ShiftEditErrorMessage = ex.Message;
            ToastService.ShowWarning("Chua the luu thay doi ca.");
        }
        catch (Exception)
        {
            ShiftEditErrorMessage = "Khong the luu thay doi ca lam viec.";
            ToastService.ShowError("Khong the luu thay doi ca.");
        }
        finally
        {
            IsShiftEditBusy = false;
        }
    }

    private bool TryGetNormalizedShiftEditDateRange(
        out DateOnly fromDate,
        out DateOnly toDate,
        out string errorMessage)
    {
        fromDate = default;
        toDate = default;
        errorMessage = string.Empty;

        if (!ShiftEditFromDate.HasValue || !ShiftEditToDate.HasValue)
        {
            errorMessage = "Hãy chọn đầy đủ Từ ngày và Đến ngày cho cài đặt ca.";
            return false;
        }

        fromDate = DateOnly.FromDateTime(ShiftEditFromDate.Value.Date);
        toDate = DateOnly.FromDateTime(ShiftEditToDate.Value.Date);
        if (toDate < fromDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
            ShiftEditFromDate = fromDate.ToDateTime(TimeOnly.MinValue);
            ShiftEditToDate = toDate.ToDateTime(TimeOnly.MinValue);
        }

        return true;
    }

    private bool TryGetNormalizedDateRange(
        out DateOnly fromDate,
        out DateOnly toDate,
        out string errorMessage)
    {
        fromDate = default;
        toDate = default;
        errorMessage = string.Empty;

        if (!FromDate.HasValue || !ToDate.HasValue)
        {
            errorMessage = "Hãy chọn đầy đủ Từ ngày và Đến ngày.";
            return false;
        }

        fromDate = DateOnly.FromDateTime(FromDate.Value.Date);
        toDate = DateOnly.FromDateTime(ToDate.Value.Date);

        if (toDate < fromDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
            FromDate = fromDate.ToDateTime(TimeOnly.MinValue);
            ToDate = toDate.ToDateTime(TimeOnly.MinValue);
        }

        var visibleDayCount = toDate.DayNumber - fromDate.DayNumber + 1;
        if (visibleDayCount > MaximumVisibleDays)
        {
            errorMessage = $"Khoảng ngày vượt quá {MaximumVisibleDays} ngày. Hãy chia nhỏ để Bảng xếp ca dễ quan sát hơn.";
            return false;
        }

        return true;
    }

    private string BuildCurrentRangeText()
    {
        if (!FromDate.HasValue || !ToDate.HasValue)
        {
            return "Chưa chọn khoảng ngày";
        }

        var fromDate = FromDate.Value.Date;
        var toDate = ToDate.Value.Date;
        return $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
    }

    private string BuildDataSummaryText()
    {
        if (!HasRequestedLoad)
        {
            return "Chưa tải dữ liệu";
        }

        if (Rows.Count == 0)
        {
            return "0 nhân viên có xếp ca";
        }

        var visibleRows = VisibleRows;

        if (HasActiveSearch)
        {
            return $"{visibleRows.Count}/{Rows.Count} nhân viên, {VisibleAssignmentCount} ô ca đang hiển thị";
        }

        return $"{visibleRows.Count} nhân viên, {VisibleAssignmentCount} ô ca trong khoảng ngày";
    }

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        if (!CanExport)
        {
            ToastService.ShowWarning("Chưa có dữ liệu bảng xếp ca để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu bảng xếp ca.");
        }
    }

    private string BuildExportFileName()
    {
        if (!FromDate.HasValue || !ToDate.HasValue)
        {
            return "bang-xep-ca";
        }

        var fromDate = FromDate.Value.Date;
        var toDate = ToDate.Value.Date;
        return $"bang-xep-ca-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}";
    }

    private static string BuildEnsureSuccessMessage(AttendanceShiftAssignmentEnsureResult result)
    {
        var message = $"Đã đồng bộ ca: thêm {result.InsertedCount:N0}, cập nhật {result.UpdatedCount:N0}, giữ nguyên {result.UnchangedCount:N0}, không ghi đè {result.ProtectedCount:N0}.";
        if (result.SkippedNonWorkingDateCount > 0)
        {
            message += $" Bỏ qua {result.SkippedNonWorkingDateCount:N0} ngày nghỉ/ngày lễ.";
        }

        if (result.DeletedNonWorkingAutoRuleCount > 0)
        {
            message += $" Đã xóa {result.DeletedNonWorkingAutoRuleCount:N0} ca tự động trên ngày nghỉ/ngày lễ.";
        }

        return message;
    }

    private static string BuildEnsureIssueMessage(AttendanceShiftAssignmentEnsureResult result)
    {
        var issuePreview = result.Issues
            .Take(5)
            .Select(issue => issue.Message)
            .ToArray();
        var suffix = result.Issues.Count > issuePreview.Length
            ? $" Còn {result.Issues.Count - issuePreview.Length:N0} vấn đề khác."
            : string.Empty;

        var assignableDateCount = result.DateCount - result.SkippedNonWorkingDateCount;
        var message = $"Chưa thể đồng bộ ca cho {result.EligibleEmployeeCount:N0} nhân viên trong {assignableDateCount:N0} ngày làm việc. {string.Join(" ", issuePreview)}{suffix}";
        if (result.SkippedNonWorkingDateCount > 0)
        {
            message += $" Đã bỏ qua {result.SkippedNonWorkingDateCount:N0} ngày nghỉ/ngày lễ.";
        }

        return message;
    }

    private static IReadOnlyList<ShiftRosterMatrixRow> ApplySearch(
        IReadOnlyList<ShiftRosterMatrixRow> rows,
        string? searchText)
    {
        var normalizedSearchText = NormalizeForSearch(searchText);
        if (string.IsNullOrEmpty(normalizedSearchText))
        {
            return rows;
        }

        return rows
            .Where(row => MatchesSearch(row, normalizedSearchText))
            .ToArray();
    }

    private void RefreshVisibleRows()
    {
        VisibleRows = ApplySearch(Rows, SearchText);
        VisibleAssignmentCount = VisibleRows.Sum(row =>
            row.Cells.Values.Count(cell => !string.IsNullOrWhiteSpace(cell.DisplayText)));
    }

    private static bool MatchesSearch(ShiftRosterMatrixRow row, string normalizedSearchText)
    {
        if (ContainsSearch(row.EmployeeDisplay, normalizedSearchText)
            || ContainsSearch(row.DepartmentPath, normalizedSearchText))
        {
            return true;
        }

        return row.Cells.Values.Any(cell =>
            ContainsSearch(cell.DisplayText, normalizedSearchText));
    }

    private static bool ContainsSearch(string? source, string normalizedSearchText) =>
        NormalizeForSearch(source).Contains(normalizedSearchText, StringComparison.Ordinal);

    private static string NormalizeForSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant();
    }

    private static IReadOnlyList<ShiftRosterDateColumn> BuildDateColumns(DateOnly fromDate, DateOnly toDate)
    {
        var columns = new List<ShiftRosterDateColumn>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            columns.Add(new ShiftRosterDateColumn(
                date,
                date.ToString("dd-MM", DisplayCulture),
                GetWeekdayText(date.DayOfWeek),
                date.DayOfWeek == DayOfWeek.Sunday));
        }

        return columns;
    }

    private static (DateTime FromDate, DateTime ToDate) CreateDefaultRange(DateTime today)
    {
        var normalizedToday = today.Date;
        var offset = normalizedToday.DayOfWeek switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            _ => 6
        };

        var fromDate = normalizedToday.AddDays(-offset);
        return (fromDate, fromDate.AddDays(6));
    }

    private static string GetWeekdayText(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        _ => "CN"
    };

    private static bool TryNormalizeHexColor(string? value, out string normalizedValue)
    {
        normalizedValue = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length != 7 || trimmedValue[0] != '#')
        {
            return false;
        }

        if (!int.TryParse(trimmedValue.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        normalizedValue = trimmedValue.ToUpperInvariant();
        return true;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }

    private static ShiftRosterMatrixRow MapRow(AttendanceShiftRosterRowDto source)
    {
        var cells = source.Cells.ToDictionary(
            cell => cell.WorkDate,
            cell => new ShiftRosterCell(
                cell.ShiftId,
                ResolveCellDisplayText(cell),
                cell.ShiftColorHex,
                cell.CreationType,
                cell.HasConflict));

        return new ShiftRosterMatrixRow
        {
            EmployeeId = source.EmployeeId,
            EmployeeDisplay = source.EmployeeDisplay,
            DepartmentPath = string.IsNullOrWhiteSpace(source.DepartmentPath) ? "--" : source.DepartmentPath.Trim(),
            Cells = cells
        };
    }

    private static string? ResolveCellDisplayText(AttendanceShiftRosterCellDto source)
    {
        if (source.HasConflict)
        {
            return source.ShiftShortName;
        }

        return Normalize(source.ShiftShortName)
            ?? Normalize(source.ShiftCode)
            ?? Normalize(source.ShiftName);
    }

    private sealed record ShiftRosterDateColumn(
        DateOnly WorkDate,
        string HeaderText,
        string WeekdayText,
        bool IsSunday);

    private sealed class ShiftRosterMatrixRow
    {
        public Guid EmployeeId { get; init; }

        public string EmployeeDisplay { get; init; } = "--";

        public string DepartmentPath { get; init; } = "--";

        public Dictionary<DateOnly, ShiftRosterCell> Cells { get; init; } = [];
    }

    private sealed record ShiftRosterCell(
        Guid? ShiftId,
        string? DisplayText,
        string? ColorHex,
        string? CreationType,
        bool HasConflict = false);

    private static string BuildShiftOptionText(AttendanceShiftListItemDto shift)
    {
        var shiftName = Normalize(shift.Name);
        var fallbackName = Normalize(shift.ShortName) ?? Normalize(shift.Code) ?? "Ca khong xac dinh";
        var primaryText = shiftName ?? fallbackName;
        var startTime = Normalize(shift.StartTime) ?? "--:--";
        var endTime = Normalize(shift.EndTime) ?? "--:--";
        return $"{primaryText} ({startTime} - {endTime})";
    }
}
