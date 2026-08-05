using System.Globalization;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;
using AttendanceOvertimeEmployeeAssignmentType = Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa.OvertimeEmployeeAssignmentType;
using AttendanceOvertimeRegistrationStatus = Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa.OvertimeRegistrationStatus;

namespace Vnta.Hrm.Web.Client.Components.DangKyPheDuyet.DangKyTangCa;

public partial class DangKyTangCa : IDisposable
{
    private const string PopupLoadingDefaultText = "Đang lưu phiếu đăng ký tăng ca...";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly int[] PageSizeOptions = [50, 100, 200];
    private static readonly IReadOnlyList<RequestDayTypeFilterOption> DayTypeFilterOptions =
    [
        new(OvertimeRequestDayTypeFilter.All, "Tất cả"),
        new(OvertimeRequestDayTypeFilter.Regular, "Ngày thường"),
        new(OvertimeRequestDayTypeFilter.Special, "Ngày nghỉ/ngày lễ"),
        new(OvertimeRequestDayTypeFilter.DayOff, "Ngày nghỉ"),
        new(OvertimeRequestDayTypeFilter.Holiday, "Ngày lễ")
    ];
    private static readonly IReadOnlyList<RequestStatusFilterOption> StatusFilterOptions =
    [
        new(OvertimeRequestStatusFilter.All, "Tất cả"),
        new(OvertimeRequestStatusFilter.Draft, "Nháp"),
        new(OvertimeRequestStatusFilter.PendingApproval, "Chờ phê duyệt"),
        new(OvertimeRequestStatusFilter.Returned, "Trả lại chỉnh sửa"),
        new(OvertimeRequestStatusFilter.Approved, "Đã phê duyệt"),
        new(OvertimeRequestStatusFilter.Rejected, "Từ chối")
    ];
    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private OvertimeRegistrationDataProvider OvertimeRegistrationDataProvider { get; set; } = default!;

    private IGrid? Grid { get; set; }
    private IGrid? ExportGrid { get; set; }
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    private List<OvertimeRequestRecord> AllRequests { get; set; } = [];
    private IReadOnlyList<OvertimeRequestRecord> VisibleRequests { get; set; } = [];
    private IReadOnlyList<OvertimeRequestRecord> ExportRequests { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private DateTime? ToolbarDate { get; set; } = DateTime.Today;
    private OvertimeRequestDayTypeFilter SelectedDayTypeFilter { get; set; } = OvertimeRequestDayTypeFilter.All;
    private OvertimeRequestStatusFilter SelectedStatusFilter { get; set; } = OvertimeRequestStatusFilter.All;
    private string? SearchText { get; set; }
    private int pageSize = PageSizeOptions[0];
    private int currentPageIndex;
    private bool IsLoading { get; set; } = true;
    private bool IsRefreshing { get; set; }
    private bool IsProcessingAction { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsExporting { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsSavingPopup { get; set; }
    private bool IsCreatingNewRequest { get; set; }
    private string PopupLoadingText { get; set; } = PopupLoadingDefaultText;
    private string? PopupValidationMessage { get; set; }
    private string? LoadErrorMessage { get; set; }
    private OvertimeRequestEditModel? EditRequest { get; set; }

    private IReadOnlyList<OvertimeRequestRecord> PagedRequests =>
        VisibleRequests
            .Skip(CurrentPageIndex * PageSize)
            .Take(PageSize)
            .ToArray();

    private int PageSize => pageSize;
    private int CurrentPageIndex => currentPageIndex;
    private int TotalRequestCount => VisibleRequests.Count;
    private int TotalPageCount => TotalRequestCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalRequestCount / (double)PageSize);
    private int CurrentPageStartRecord => TotalRequestCount == 0
        ? 0
        : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalRequestCount == 0
        ? 0
        : Math.Min(TotalRequestCount, CurrentPageIndex * PageSize + PagedRequests.Count);
    private string PagerSummaryText => HasLoadError || TotalRequestCount == 0
        ? "Chưa có trang dữ liệu"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRequestCount:N0} dòng";
    private bool HasActiveListFilter => !string.IsNullOrWhiteSpace(SearchText)
        || SelectedDayTypeFilter != OvertimeRequestDayTypeFilter.All
        || SelectedStatusFilter != OvertimeRequestStatusFilter.All;
    private bool ShowLoadingPanel => IsLoading || IsRefreshing || IsProcessingAction || IsChangingPageSize || IsExporting;
    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanInteract => !ShowLoadingPanel && !IsSavingPopup && !HasLoadError;
    private bool CanReload => !ShowLoadingPanel && !IsSavingPopup;
    private bool CanRefresh => !ShowLoadingPanel && !IsSavingPopup;
    private bool CanCreate => !ShowLoadingPanel && !IsSavingPopup;
    private bool CanChangeFilters => !ShowLoadingPanel && !IsSavingPopup;
    private bool CanEmptyStateAction => !ShowLoadingPanel && !IsSavingPopup;
    private bool CanBrowsePages => CanInteract && TotalRequestCount > 0;
    private int PendingApprovalCount => VisibleRequests.Count(request => request.Status == OvertimeRequestStatus.PendingApproval);
    private int ApprovedCount => VisibleRequests.Count(request => request.Status == OvertimeRequestStatus.Approved);
    private int RegisteredEmployeeCount => VisibleRequests.Sum(request => request.RegisteredEmployeeCount);
    private string LoadingPanelText => IsProcessingAction
        ? "Đang cập nhật trạng thái phiếu đăng ký tăng ca..."
        : IsRefreshing
            ? "Đang làm mới danh sách đăng ký tăng ca..."
            : IsChangingPageSize
                ? "Đang cập nhật số dòng hiển thị..."
                : IsExporting
                    ? "Đang chuẩn bị dữ liệu đăng ký tăng ca để xuất tệp..."
            : "Đang tải danh sách đăng ký tăng ca...";
    private string EmptyStateTitle => HasActiveListFilter
        ? "Không tìm thấy phiếu đăng ký tăng ca phù hợp"
        : "Chưa có phiếu đăng ký tăng ca";
    private string EmptyStateMessage => HasActiveListFilter
        ? "Hãy thay đổi điều kiện tìm kiếm hoặc bộ lọc để xem thêm dữ liệu."
        : "Bắt đầu bằng cách tạo phiếu đăng ký tăng ca đầu tiên cho xưởng của bạn.";
    private string EmptyStateActionText => !string.IsNullOrWhiteSpace(SearchText)
        ? "Xóa tìm kiếm"
        : "Tạo phiếu";
    private string CurrentFilterSummary => BuildCurrentFilterSummary();

    private bool CanEditSelected
    {
        get
        {
            var request = GetSingleSelectedRequest();
            return CanInteract
                   && request is not null
                   && (request.Status == OvertimeRequestStatus.Draft || request.Status == OvertimeRequestStatus.Returned);
        }
    }

    private bool CanSubmitSelected =>
        CanInteract
        && GetSelectedRequests().Any()
        && GetSelectedRequests().All(request =>
            request.Status is OvertimeRequestStatus.Draft or OvertimeRequestStatus.Returned);

    private bool CanApproveSelected =>
        CanInteract
        && GetSelectedRequests().Any()
        && GetSelectedRequests().All(request => request.Status == OvertimeRequestStatus.PendingApproval);

    private bool CanReturnSelected =>
        CanInteract
        && GetSelectedRequests().Any()
        && GetSelectedRequests().All(request => request.Status == OvertimeRequestStatus.PendingApproval);

    private bool CanRejectSelected =>
        CanInteract
        && GetSelectedRequests().Any()
        && GetSelectedRequests().All(request => request.Status == OvertimeRequestStatus.PendingApproval);

    private bool CanExport => CanInteract && VisibleRequests.Count > 0;

    private bool CanExportSelected => CanExport && GetSelectedRequests().Count > 0;

    private bool CanOperateOnRequest(OvertimeRequestRecord request) =>
        CanInteract && VisibleRequests.Any(item => item.Id == request.Id);

    private bool CanEditRequest(OvertimeRequestRecord request) =>
        CanOperateOnRequest(request)
        && request.Status is OvertimeRequestStatus.Draft or OvertimeRequestStatus.Returned;

    private bool CanSubmitRequest(OvertimeRequestRecord request) =>
        CanOperateOnRequest(request)
        && request.Status is OvertimeRequestStatus.Draft or OvertimeRequestStatus.Returned;

    private bool CanApproveRequest(OvertimeRequestRecord request) =>
        CanOperateOnRequest(request)
        && request.Status == OvertimeRequestStatus.PendingApproval;

    private bool CanReturnRequest(OvertimeRequestRecord request) =>
        CanOperateOnRequest(request)
        && request.Status == OvertimeRequestStatus.PendingApproval;

    private bool CanRejectRequest(OvertimeRequestRecord request) =>
        CanOperateOnRequest(request)
        && request.Status == OvertimeRequestStatus.PendingApproval;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
        await base.OnInitializedAsync();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task ReloadAsync()
    {
        var lockTaken = false;

        try
        {
            await reloadGate.WaitAsync(disposalTokenSource.Token);
            lockTaken = true;
            LoadErrorMessage = null;
            IsLoading = true;
            await LoadRequestsAsync(disposalTokenSource.Token);
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            LoadErrorMessage = "Có lỗi khi tải dữ liệu đăng ký tăng ca. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách đăng ký tăng ca.");
        }
        finally
        {
            IsLoading = false;
            if (lockTaken)
            {
                reloadGate.Release();
            }
        }
    }

    private Task OnRetryAsync() => ReloadAsync();

    private async Task RefreshAsync()
    {
        if (!CanRefresh)
        {
            return;
        }

        var lockTaken = false;

        try
        {
            await reloadGate.WaitAsync(disposalTokenSource.Token);
            lockTaken = true;
            LoadErrorMessage = null;
            IsRefreshing = true;
            await LoadRequestsAsync(disposalTokenSource.Token);
            ToastService.ShowInfo("Đã làm mới khung dữ liệu đăng ký tăng ca.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            LoadErrorMessage = "Có lỗi khi làm mới dữ liệu đăng ký tăng ca. Vui lòng thử lại.";
            ToastService.ShowError("Không thể làm mới danh sách đăng ký tăng ca.");
        }
        finally
        {
            IsRefreshing = false;
            if (lockTaken)
            {
                reloadGate.Release();
            }
        }
    }

    private Task OnToolbarDateChanged(DateTime? value)
    {
        ToolbarDate = value?.Date;
        return Task.CompletedTask;
    }

    private Task OnDayTypeFilterChanged(OvertimeRequestDayTypeFilter value)
    {
        SelectedDayTypeFilter = value;
        return Task.CompletedTask;
    }

    private Task OnStatusFilterChanged(OvertimeRequestStatusFilter value)
    {
        SelectedStatusFilter = value;
        return Task.CompletedTask;
    }

    private async Task OnSearchTextChanged(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (string.Equals(SearchText, normalized, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalized;
        currentPageIndex = 0;
        await ClearSelectionAsync();
        ApplyFilters();
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task OnEmptyStateActionClick()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            SearchText = null;
            await ClearSelectionAsync();
            ApplyFilters();
            return;
        }

        await OpenCreatePopupAsync();
    }

    private Task ShowColumnChooserAsync()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private Task ExportAllDataToExcel() => ExportAsync(
        VisibleRequests,
        () => ExportGrid!.ExportToXlsxAsync("dang-ky-tang-ca"),
        "Đã xuất dữ liệu đăng ký tăng ca ra Excel.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        GetSelectedRequests(),
        () => ExportGrid!.ExportToXlsxAsync("dang-ky-tang-ca-selected"),
        "Đã xuất các phiếu đã chọn ra Excel.");

    private Task ExportAllDataToPdf() => ExportAsync(
        VisibleRequests,
        () => ExportGrid!.ExportToPdfAsync("dang-ky-tang-ca"),
        "Đã xuất dữ liệu đăng ký tăng ca ra PDF.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        GetSelectedRequests(),
        () => ExportGrid!.ExportToPdfAsync("dang-ky-tang-ca-selected"),
        "Đã xuất các phiếu đã chọn ra PDF.");

    private async Task ExportAsync(
        IReadOnlyList<OvertimeRequestRecord> requests,
        Func<Task> exportAction,
        string successMessage)
    {
        if (requests.Count == 0 || disposalTokenSource.IsCancellationRequested)
        {
            ToastService.ShowWarning("Không có dữ liệu đăng ký tăng ca để xuất.");
            return;
        }

        IsExporting = true;
        try
        {
            ExportRequests = requests;
            exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await InvokeAsync(StateHasChanged);
            await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

            if (ExportGrid is null)
            {
                throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
            }

            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu đăng ký tăng ca.");
        }
        finally
        {
            ExportRequests = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;

            if (!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : PageSizeOptions[0];
        if (PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        try
        {
            var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
            pageSize = normalizedValue;
            currentPageIndex = firstVisibleRecordIndex / PageSize;
            ClampCurrentPageIndex();
            await ClearSelectionAsync();
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
        }
    }

    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if (!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, TotalPageCount - 1);
        if (CurrentPageIndex == normalizedValue)
        {
            return;
        }

        currentPageIndex = normalizedValue;
        await ClearSelectionAsync();
    }

    private async Task OpenCreatePopupAsync()
    {
        if (!CanCreate)
        {
            return;
        }

        var workDate = ResolveCreateWorkDate();
        var dayType = ResolveCreateDayType(workDate);

        try
        {
            var draft = await OvertimeRegistrationDataProvider.CreateDraftAsync(
                new CreateOvertimeRegistrationDraftRequest(
                    DateOnly.FromDateTime(workDate),
                    dayType),
                disposalTokenSource.Token);

            EditRequest = CreateEditModel(draft);
            IsCreatingNewRequest = true;
            IsEditPopupVisible = true;
            PopupValidationMessage = null;
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể khởi tạo phiếu đăng ký tăng ca mới.");
        }
    }

    private Task OpenEditPopupAsync() => OpenEditPopupAsync(GetSingleSelectedRequest());

    private async Task OpenEditPopupAsync(OvertimeRequestRecord? request)
    {
        if (request is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một phiếu để điều chỉnh.");
            return;
        }

        if (request.Status is not OvertimeRequestStatus.Draft and not OvertimeRequestStatus.Returned)
        {
            ToastService.ShowWarning("Chỉ phiếu nháp hoặc trả lại mới được điều chỉnh.");
            return;
        }

        EditRequest = CreateEditModel(request);
        IsCreatingNewRequest = false;
        IsEditPopupVisible = true;
        PopupValidationMessage = null;
        await InvokeAsync(StateHasChanged);
    }

    private Task CloseEditPopupAsync()
    {
        EditRequest = null;
        PopupValidationMessage = null;
        IsEditPopupVisible = false;
        IsCreatingNewRequest = false;
        return Task.CompletedTask;
    }

    private Task OnEditPopupVisibleChangedAsync(bool visible)
    {
        return visible
            ? Task.CompletedTask
            : CloseEditPopupAsync();
    }

    private Task SaveDraftAsync() => PersistEditRequestAsync(submitAfterSave: false);

    private Task SaveAndSubmitAsync() => PersistEditRequestAsync(submitAfterSave: true);

    private async Task PersistEditRequestAsync(bool submitAfterSave)
    {
        if (EditRequest is null)
        {
            return;
        }

        PopupValidationMessage = ValidateEditRequest(EditRequest);
        if (!string.IsNullOrWhiteSpace(PopupValidationMessage))
        {
            ToastService.ShowWarning("Phiếu đăng ký tăng ca còn thiếu thông tin.");
            return;
        }

        IsSavingPopup = true;
        PopupLoadingText = submitAfterSave
            ? "Đang lưu và gửi phiếu đăng ký tăng ca..."
            : PopupLoadingDefaultText;

        try
        {
            await OvertimeRegistrationDataProvider.SaveAsync(
                MapUpsertRequest(EditRequest),
                submitAfterSave,
                disposalTokenSource.Token);

            await LoadRequestsAsync(disposalTokenSource.Token);
            EditRequest = null;
            PopupValidationMessage = null;
            IsEditPopupVisible = false;
            IsCreatingNewRequest = false;

            ToastService.ShowSuccess(
                submitAfterSave
                    ? "Đã lưu và gửi phiếu đăng ký tăng ca sang chờ phê duyệt."
                    : "Đã lưu phiếu đăng ký tăng ca ở trạng thái nháp.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            PopupValidationMessage = "Không thể lưu phiếu đăng ký tăng ca. Vui lòng kiểm tra dữ liệu và thử lại.";
            ToastService.ShowError(PopupValidationMessage);
        }
        finally
        {
            IsSavingPopup = false;
            PopupLoadingText = PopupLoadingDefaultText;
        }
    }

    private Task SubmitSelectedAsync() => SubmitRequestsAsync(GetSelectedRequests());

    private Task SubmitRequestAsync(OvertimeRequestRecord request) => SubmitRequestsAsync([request]);

    private async Task SubmitRequestsAsync(IReadOnlyList<OvertimeRequestRecord> requests)
    {
        if (requests.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một phiếu để gửi phê duyệt.");
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            requests.Count == 1
                ? "Gửi phiếu đăng ký tăng ca đã chọn sang chờ phê duyệt?"
                : $"Gửi {requests.Count} phiếu đăng ký tăng ca đã chọn sang chờ phê duyệt?",
            "Gửi phê duyệt",
            "Gửi",
            "Hủy");
        if (!confirmed)
        {
            return;
        }

        await ChangeRequestStatusesAsync(
            requests,
            OvertimeRequestStatus.PendingApproval,
            "Đã chuyển phiếu sang chờ phê duyệt.");
    }

    private Task ApproveSelectedAsync() => ApproveRequestsAsync(GetSelectedRequests());

    private Task ApproveRequestAsync(OvertimeRequestRecord request) => ApproveRequestsAsync([request]);

    private async Task ApproveRequestsAsync(IReadOnlyList<OvertimeRequestRecord> requests)
    {
        if (requests.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một phiếu để phê duyệt.");
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            requests.Count == 1
                ? "Phê duyệt phiếu đăng ký tăng ca đã chọn?"
                : $"Phê duyệt {requests.Count} phiếu đăng ký tăng ca đã chọn?",
            "Phê duyệt phiếu",
            "Duyệt",
            "Hủy");
        if (!confirmed)
        {
            return;
        }

        await ChangeRequestStatusesAsync(
            requests,
            OvertimeRequestStatus.Approved,
            "Đã phê duyệt phiếu đăng ký tăng ca.");
    }

    private Task ReturnSelectedAsync() => ReturnRequestsAsync(GetSelectedRequests());

    private Task ReturnRequestAsync(OvertimeRequestRecord request) => ReturnRequestsAsync([request]);

    private async Task ReturnRequestsAsync(IReadOnlyList<OvertimeRequestRecord> requests)
    {
        if (requests.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một phiếu để trả lại.");
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            requests.Count == 1
                ? "Trả lại phiếu đăng ký tăng ca đã chọn để xưởng trưởng chỉnh sửa?"
                : $"Trả lại {requests.Count} phiếu đăng ký tăng ca để xưởng trưởng chỉnh sửa?",
            "Trả lại phiếu",
            "Trả lại",
            "Hủy");
        if (!confirmed)
        {
            return;
        }

        await ChangeRequestStatusesAsync(
            requests,
            OvertimeRequestStatus.Returned,
            "Đã trả lại phiếu để chỉnh sửa.");
    }

    private Task RejectSelectedAsync() => RejectRequestsAsync(GetSelectedRequests());

    private Task RejectRequestAsync(OvertimeRequestRecord request) => RejectRequestsAsync([request]);

    private async Task RejectRequestsAsync(IReadOnlyList<OvertimeRequestRecord> requests)
    {
        if (requests.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một phiếu để từ chối.");
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            requests.Count == 1
                ? "Từ chối phiếu đăng ký tăng ca đã chọn?"
                : $"Từ chối {requests.Count} phiếu đăng ký tăng ca đã chọn?",
            "Từ chối phiếu",
            "Từ chối",
            "Hủy",
            MessageBoxRenderStyle.Danger);
        if (!confirmed)
        {
            return;
        }

        await ChangeRequestStatusesAsync(
            requests,
            OvertimeRequestStatus.Rejected,
            "Đã từ chối phiếu đăng ký tăng ca.");
    }

    private async Task ChangeRequestStatusesAsync(
        IReadOnlyList<OvertimeRequestRecord> requests,
        OvertimeRequestStatus targetStatus,
        string successMessage)
    {
        if (requests.Count == 0)
        {
            return;
        }

        IsProcessingAction = true;

        try
        {
            var requestIds = requests
                .Select(request => request.Id)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            await OvertimeRegistrationDataProvider.ChangeStatusAsync(
                new ChangeOvertimeRegistrationStatusRequest
                {
                    Ids = requestIds,
                    TargetStatus = MapStatus(targetStatus)
                },
                disposalTokenSource.Token);

            await LoadRequestsAsync(disposalTokenSource.Token);
            ToastService.ShowSuccess(successMessage);
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể cập nhật trạng thái phiếu đăng ký tăng ca.");
        }
        finally
        {
            IsProcessingAction = false;
        }
    }

    private async Task LoadRequestsAsync(CancellationToken cancellationToken = default)
    {
        var results = await OvertimeRegistrationDataProvider.SearchAsync(BuildSearchFilter(), cancellationToken);
        AllRequests = results.Select(MapRecord).ToList();
        currentPageIndex = 0;
        await ClearSelectionAsync();
        ApplyFilters();
    }

    private OvertimeRegistrationFilter BuildSearchFilter() =>
        new(
            ToolbarDate is DateTime workDate ? DateOnly.FromDateTime(workDate) : null,
            MapDayTypeFilter(SelectedDayTypeFilter),
            MapStatusFilter(SelectedStatusFilter),
            SearchText);

    private static AttendanceWorkCalendarDayType? MapDayTypeFilter(
        OvertimeRequestDayTypeFilter filter) => filter switch
    {
        OvertimeRequestDayTypeFilter.Regular => AttendanceWorkCalendarDayType.Regular,
        OvertimeRequestDayTypeFilter.DayOff => AttendanceWorkCalendarDayType.DayOff,
        OvertimeRequestDayTypeFilter.Holiday => AttendanceWorkCalendarDayType.Holiday,
        _ => null
    };

    private static AttendanceOvertimeRegistrationStatus? MapStatusFilter(
        OvertimeRequestStatusFilter filter) => filter switch
    {
        OvertimeRequestStatusFilter.Draft => AttendanceOvertimeRegistrationStatus.Draft,
        OvertimeRequestStatusFilter.PendingApproval => AttendanceOvertimeRegistrationStatus.PendingApproval,
        OvertimeRequestStatusFilter.Returned => AttendanceOvertimeRegistrationStatus.Returned,
        OvertimeRequestStatusFilter.Approved => AttendanceOvertimeRegistrationStatus.Approved,
        OvertimeRequestStatusFilter.Rejected => AttendanceOvertimeRegistrationStatus.Rejected,
        _ => null
    };

    private void ApplyFilters()
    {
        IEnumerable<OvertimeRequestRecord> query = AllRequests;

        if (ToolbarDate is DateTime dateFilter)
        {
            var normalizedDate = dateFilter.Date;
            query = query.Where(request => request.WorkDate.Date == normalizedDate);
        }

        query = SelectedDayTypeFilter switch
        {
            OvertimeRequestDayTypeFilter.Regular => query.Where(request => request.DayType == AttendanceWorkCalendarDayType.Regular),
            OvertimeRequestDayTypeFilter.Special => query.Where(request => AttendanceWorkCalendarDayTypes.IsSpecialDay(request.DayType)),
            OvertimeRequestDayTypeFilter.DayOff => query.Where(request => request.DayType == AttendanceWorkCalendarDayType.DayOff),
            OvertimeRequestDayTypeFilter.Holiday => query.Where(request => request.DayType == AttendanceWorkCalendarDayType.Holiday),
            _ => query
        };

        query = SelectedStatusFilter switch
        {
            OvertimeRequestStatusFilter.Draft => query.Where(request => request.Status == OvertimeRequestStatus.Draft),
            OvertimeRequestStatusFilter.PendingApproval => query.Where(request => request.Status == OvertimeRequestStatus.PendingApproval),
            OvertimeRequestStatusFilter.Returned => query.Where(request => request.Status == OvertimeRequestStatus.Returned),
            OvertimeRequestStatusFilter.Approved => query.Where(request => request.Status == OvertimeRequestStatus.Approved),
            OvertimeRequestStatusFilter.Rejected => query.Where(request => request.Status == OvertimeRequestStatus.Rejected),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = NormalizeText(SearchText);
            query = query.Where(request => MatchesSearch(request, keyword));
        }

        VisibleRequests = query
            .OrderByDescending(request => request.WorkDate)
            .ThenBy(request => request.WorkshopName)
            .ToArray();

        ClampCurrentPageIndex();
    }

    private void ClampCurrentPageIndex()
    {
        currentPageIndex = Math.Clamp(currentPageIndex, 0, Math.Max(0, TotalPageCount - 1));
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private IReadOnlyList<OvertimeRequestRecord> GetSelectedRequests()
    {
        var visibleIds = VisibleRequests.Select(request => request.Id).ToHashSet();

        return SelectedDataItems
            .OfType<OvertimeRequestRecord>()
            .Where(request => visibleIds.Contains(request.Id))
            .DistinctBy(request => request.Id)
            .ToArray();
    }

    private OvertimeRequestRecord? GetSingleSelectedRequest()
    {
        var selectedRequests = GetSelectedRequests();
        return selectedRequests.Count == 1 ? selectedRequests[0] : null;
    }

    private DateTime ResolveCreateWorkDate() => ToolbarDate?.Date ?? DateTime.Today;

    private string BuildCurrentFilterSummary()
    {
        var parts = new List<string>();

        if (ToolbarDate is DateTime workDate)
        {
            parts.Add($"Ngày: {FormatDate(workDate)}");
        }
        else
        {
            parts.Add("Ngày: Tất cả");
        }

        parts.Add(SelectedDayTypeFilter switch
        {
            OvertimeRequestDayTypeFilter.Regular => "Loại ngày: Ngày thường",
            OvertimeRequestDayTypeFilter.Special => "Loại ngày: Ngày nghỉ/ngày lễ",
            OvertimeRequestDayTypeFilter.DayOff => "Loại ngày: Ngày nghỉ",
            OvertimeRequestDayTypeFilter.Holiday => "Loại ngày: Ngày lễ",
            _ => "Loại ngày: Tất cả"
        });

        if (SelectedStatusFilter != OvertimeRequestStatusFilter.All)
        {
            var statusText = SelectedStatusFilter switch
            {
                OvertimeRequestStatusFilter.Draft => GetStatusDisplayName(OvertimeRequestStatus.Draft),
                OvertimeRequestStatusFilter.PendingApproval => GetStatusDisplayName(OvertimeRequestStatus.PendingApproval),
                OvertimeRequestStatusFilter.Returned => GetStatusDisplayName(OvertimeRequestStatus.Returned),
                OvertimeRequestStatusFilter.Approved => GetStatusDisplayName(OvertimeRequestStatus.Approved),
                OvertimeRequestStatusFilter.Rejected => GetStatusDisplayName(OvertimeRequestStatus.Rejected),
                _ => string.Empty
            };
            parts.Add($"Trạng thái: {statusText}");
        }

        return string.Join(" · ", parts);
    }

    private static bool MatchesSearch(OvertimeRequestRecord request, string keyword)
    {
        var target = NormalizeText(
            $"{request.WorkshopName} {request.TeamSummary} {request.RequestedBy} {request.Note} {request.StatusDisplay} {request.DayTypeDisplay} {request.RegistrationModeDisplay} {string.Join(' ', request.EmployeeAssignments.Select(employee => employee.EmployeeDisplay))}");
        return target.Contains(keyword, StringComparison.Ordinal);
    }

    internal static string NormalizeText(string? value)
    {
        var builder = new StringBuilder();
        foreach (var character in (value ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Trim();
    }

    private static string FormatDate(DateTime value) => value.ToString("dd/MM/yyyy", DisplayCulture);

    private static string FormatDateTime(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", DisplayCulture);

    internal static string GetStatusDisplayName(OvertimeRequestStatus status) => status switch
    {
        OvertimeRequestStatus.Draft => "Nháp",
        OvertimeRequestStatus.PendingApproval => "Chờ phê duyệt",
        OvertimeRequestStatus.Returned => "Trả lại để chỉnh sửa",
        OvertimeRequestStatus.Approved => "Đã phê duyệt",
        OvertimeRequestStatus.Rejected => "Từ chối",
        _ => "Nháp"
    };

    private static string GetDayTypeBadgeCssClass(AttendanceWorkCalendarDayType dayType) => dayType switch
    {
        AttendanceWorkCalendarDayType.Regular => "day-type-badge day-type-badge-regular",
        AttendanceWorkCalendarDayType.DayOff => "day-type-badge day-type-badge-day-off",
        AttendanceWorkCalendarDayType.Holiday => "day-type-badge day-type-badge-holiday",
        _ => "day-type-badge day-type-badge-regular"
    };

    internal static string GetStatusBadgeCssClass(OvertimeRequestStatus status) => status switch
    {
        OvertimeRequestStatus.Draft => "status-badge status-badge-neutral",
        OvertimeRequestStatus.PendingApproval => "status-badge status-badge-warning",
        OvertimeRequestStatus.Returned => "status-badge status-badge-info",
        OvertimeRequestStatus.Approved => "status-badge status-badge-success",
        OvertimeRequestStatus.Rejected => "status-badge status-badge-danger",
        _ => "status-badge status-badge-neutral"
    };

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    private static string? ValidateEditRequest(OvertimeRequestEditModel request)
    {
        if (request.EmployeeAssignments.Count == 0)
        {
            return "Phiếu đăng ký tăng ca chưa có nhân viên nào trong danh sách xưởng.";
        }

        if (request.EmployeeAssignments.All(employee => employee.AssignmentType == OvertimeEmployeeAssignmentType.None))
        {
            return "Hãy chọn ít nhất một nhân viên tham gia tăng ca.";
        }

        if (AttendanceWorkCalendarDayTypes.IsSpecialDay(request.DayType))
        {
            return null;
        }

        if (request.EmployeeAssignments.Any(employee => employee.AssignmentType == OvertimeEmployeeAssignmentType.SpecialDayRegistered))
        {
            return "Ngày thường chỉ cho phép chọn mức tăng ca đến 19:00 hoặc 21:00.";
        }

        return null;
    }

    private static OvertimeRequestRecord MapRecord(OvertimeRegistrationListItemDto source) =>
        new()
        {
            Id = source.Id,
            WorkDate = source.WorkDate.ToDateTime(TimeOnly.MinValue),
            DayType = source.DayType,
            WorkshopCode = source.WorkshopCode,
            WorkshopName = source.WorkshopName,
            RequestedBy = source.RequestedBy,
            ApprovedBy = string.IsNullOrWhiteSpace(source.ApprovedBy) ? "Giám đốc sản xuất" : source.ApprovedBy,
            Status = MapStatus(source.Status),
            Note = source.Note,
            LastActionAt = ToDisplayTime(source.LastActionAtUtc),
            SubmittedAt = source.SubmittedAtUtc.HasValue ? ToDisplayTime(source.SubmittedAtUtc.Value) : null,
            ApprovedAt = source.ApprovedAtUtc.HasValue ? ToDisplayTime(source.ApprovedAtUtc.Value) : null,
            EmployeeAssignments = source.EmployeeAssignments
                .Select(MapEmployeeAssignmentRecord)
                .ToList()
        };

    private static OvertimeRequestEditModel CreateEditModel(OvertimeRegistrationDraftDto draft) =>
        new()
        {
            Id = draft.Id,
            WorkDate = draft.WorkDate.ToDateTime(TimeOnly.MinValue),
            DayType = draft.DayType,
            WorkshopCode = draft.WorkshopCode,
            WorkshopName = draft.WorkshopName,
            RequestedBy = draft.RequestedBy,
            ApprovedBy = string.IsNullOrWhiteSpace(draft.ApprovedBy) ? "Giám đốc sản xuất" : draft.ApprovedBy,
            Status = MapStatus(draft.Status),
            Note = draft.Note,
            EmployeeAssignments = draft.EmployeeAssignments
                .Select(MapEmployeeAssignmentRecord)
                .ToList()
        };

    private static UpsertOvertimeRegistrationRequest MapUpsertRequest(
        OvertimeRequestEditModel source) =>
        new()
        {
            Id = source.Id,
            WorkDate = DateOnly.FromDateTime(source.WorkDate),
            DayType = source.DayType,
            Note = NormalizeOptional(source.Note),
            EmployeeAssignments = source.EmployeeAssignments
                .Select(employee => new UpsertOvertimeRegistrationEmployeeAssignmentRequest
                {
                    EmployeeId = employee.EmployeeId,
                    AssignmentType = MapAssignmentType(employee.AssignmentType)
                })
                .ToArray()
        };

    private static OvertimeRequestEditModel CreateEditModel(OvertimeRequestRecord request) =>
        new()
        {
            Id = request.Id,
            WorkDate = request.WorkDate,
            DayType = request.DayType,
            WorkshopCode = request.WorkshopCode,
            WorkshopName = request.WorkshopName,
            RequestedBy = request.RequestedBy,
            ApprovedBy = request.ApprovedBy,
            Status = request.Status,
            Note = request.Note,
            EmployeeAssignments = request.EmployeeAssignments
                .Select(employee => employee.Clone())
                .ToList()
        };

    private static OvertimeEmployeeAssignmentRecord MapEmployeeAssignmentRecord(
        OvertimeRegistrationEmployeeAssignmentDto source) =>
        new()
        {
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            PositionName = source.PositionName,
            TeamCode = source.TeamCode,
            TeamName = source.TeamName,
            AssignmentType = MapAssignmentType(source.AssignmentType),
            RegistrationHint = source.RegistrationHint
        };

    private AttendanceWorkCalendarDayType ResolveCreateDayType(DateTime workDate) => SelectedDayTypeFilter switch
    {
        OvertimeRequestDayTypeFilter.DayOff => AttendanceWorkCalendarDayType.DayOff,
        OvertimeRequestDayTypeFilter.Holiday => AttendanceWorkCalendarDayType.Holiday,
        OvertimeRequestDayTypeFilter.Special => AttendanceWorkCalendarDayType.DayOff,
        OvertimeRequestDayTypeFilter.Regular => AttendanceWorkCalendarDayType.Regular,
        _ => AttendanceWorkCalendarDayTypes.ResolveDefaultDayType(DateOnly.FromDateTime(workDate))
    };

    internal static void NormalizeAssignmentsForDayType(
        IReadOnlyList<OvertimeEmployeeAssignmentRecord> employees,
        AttendanceWorkCalendarDayType dayType)
    {
        if (AttendanceWorkCalendarDayTypes.IsSpecialDay(dayType))
        {
            foreach (var employee in employees)
            {
                employee.AssignmentType = employee.AssignmentType == OvertimeEmployeeAssignmentType.None
                    ? OvertimeEmployeeAssignmentType.None
                    : OvertimeEmployeeAssignmentType.SpecialDayRegistered;
            }

            return;
        }

        foreach (var employee in employees)
        {
            employee.AssignmentType = employee.AssignmentType switch
            {
                OvertimeEmployeeAssignmentType.SpecialDayRegistered => OvertimeEmployeeAssignmentType.Until1900,
                OvertimeEmployeeAssignmentType.Until2100 => OvertimeEmployeeAssignmentType.Until2100,
                OvertimeEmployeeAssignmentType.Until1900 => OvertimeEmployeeAssignmentType.Until1900,
                _ => OvertimeEmployeeAssignmentType.None
            };
        }
    }

    private static OvertimeRequestStatus MapStatus(AttendanceOvertimeRegistrationStatus status) => status switch
    {
        AttendanceOvertimeRegistrationStatus.Draft => OvertimeRequestStatus.Draft,
        AttendanceOvertimeRegistrationStatus.PendingApproval => OvertimeRequestStatus.PendingApproval,
        AttendanceOvertimeRegistrationStatus.Returned => OvertimeRequestStatus.Returned,
        AttendanceOvertimeRegistrationStatus.Approved => OvertimeRequestStatus.Approved,
        AttendanceOvertimeRegistrationStatus.Rejected => OvertimeRequestStatus.Rejected,
        _ => OvertimeRequestStatus.Draft
    };

    private static AttendanceOvertimeRegistrationStatus MapStatus(OvertimeRequestStatus status) => status switch
    {
        OvertimeRequestStatus.Draft => AttendanceOvertimeRegistrationStatus.Draft,
        OvertimeRequestStatus.PendingApproval => AttendanceOvertimeRegistrationStatus.PendingApproval,
        OvertimeRequestStatus.Returned => AttendanceOvertimeRegistrationStatus.Returned,
        OvertimeRequestStatus.Approved => AttendanceOvertimeRegistrationStatus.Approved,
        OvertimeRequestStatus.Rejected => AttendanceOvertimeRegistrationStatus.Rejected,
        _ => AttendanceOvertimeRegistrationStatus.Draft
    };

    private static OvertimeEmployeeAssignmentType MapAssignmentType(AttendanceOvertimeEmployeeAssignmentType type) => type switch
    {
        AttendanceOvertimeEmployeeAssignmentType.Until1900 => OvertimeEmployeeAssignmentType.Until1900,
        AttendanceOvertimeEmployeeAssignmentType.Until2100 => OvertimeEmployeeAssignmentType.Until2100,
        AttendanceOvertimeEmployeeAssignmentType.SpecialDayRegistered => OvertimeEmployeeAssignmentType.SpecialDayRegistered,
        _ => OvertimeEmployeeAssignmentType.None
    };

    private static AttendanceOvertimeEmployeeAssignmentType MapAssignmentType(OvertimeEmployeeAssignmentType type) => type switch
    {
        OvertimeEmployeeAssignmentType.Until1900 => AttendanceOvertimeEmployeeAssignmentType.Until1900,
        OvertimeEmployeeAssignmentType.Until2100 => AttendanceOvertimeEmployeeAssignmentType.Until2100,
        OvertimeEmployeeAssignmentType.SpecialDayRegistered => AttendanceOvertimeEmployeeAssignmentType.SpecialDayRegistered,
        _ => AttendanceOvertimeEmployeeAssignmentType.None
    };

    private static DateTime ToDisplayTime(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private enum OvertimeRequestDayTypeFilter
    {
        All = 0,
        Regular = 1,
        Special = 2,
        DayOff = 3,
        Holiday = 4
    }

    private enum OvertimeRequestStatusFilter
    {
        All = 0,
        Draft = 1,
        PendingApproval = 2,
        Returned = 3,
        Approved = 4,
        Rejected = 5
    }

    private sealed class OvertimeRequestRecord
    {
        public Guid Id { get; init; }

        public DateTime WorkDate { get; set; }

        public AttendanceWorkCalendarDayType DayType { get; set; }

        public string WorkshopCode { get; init; } = string.Empty;

        public string WorkshopName { get; init; } = string.Empty;

        public string RequestedBy { get; init; } = string.Empty;

        public string ApprovedBy { get; init; } = string.Empty;

        public OvertimeRequestStatus Status { get; set; }

        public string Note { get; set; } = string.Empty;

        public DateTime LastActionAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public List<OvertimeEmployeeAssignmentRecord> EmployeeAssignments { get; init; } = [];

        public string DayTypeDisplay => AttendanceWorkCalendarDayTypes.GetDisplayName(DayType);

        public string TeamSummary => string.Join(", ",
            EmployeeAssignments
                .Select(employee => employee.TeamName)
                .Distinct(StringComparer.OrdinalIgnoreCase));

        public string StatusDisplay => GetStatusDisplayName(Status);

        public int TotalEmployeeCount => EmployeeAssignments.Count;

        public int RegisteredEmployeeCount => EmployeeAssignments.Count(employee => employee.AssignmentType != OvertimeEmployeeAssignmentType.None);

        public int Until1900Count => EmployeeAssignments.Count(employee => employee.AssignmentType == OvertimeEmployeeAssignmentType.Until1900);

        public int Until2100Count => EmployeeAssignments.Count(employee => employee.AssignmentType == OvertimeEmployeeAssignmentType.Until2100);

        public string RegistrationModeDisplay => AttendanceWorkCalendarDayTypes.IsSpecialDay(DayType)
            ? "Danh sách tăng ca"
            : "19:00 / 21:00";

        public string RegistrationCutoffDisplay => AttendanceWorkCalendarDayTypes.IsSpecialDay(DayType)
            ? "Trước ngày làm thêm 1 ngày"
            : "Trước 15:00 cùng ngày";

        public string ApprovalCutoffDisplay => AttendanceWorkCalendarDayTypes.IsSpecialDay(DayType)
            ? "Duyệt toàn bộ trước ngày làm thêm"
            : "Trước 16:30 cùng ngày";
    }

    private sealed record RequestDayTypeFilterOption(OvertimeRequestDayTypeFilter Value, string Text);

    private sealed record RequestStatusFilterOption(OvertimeRequestStatusFilter Value, string Text);

}
