using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Text;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Employees;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.NhanVien;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.NhanVien;

public partial class NhanVien : IDisposable
{
    #region Constants

    private const string AddEmployeeToolbarItemName = "add-employee";
    private const string SummaryWorkingKey = "working";
    private const string SummaryAllKey = "all";
    private const string SummaryProbationKey = "probation";
    private const string SummaryOfficialKey = "official";
    private const string SummaryResignedKey = "resigned";
    private static readonly int[] PageSizeOptions = [50, 100, 200];

    #endregion

    #region Dependencies

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private CancellationTokenSource? activeLoadTokenSource;

    [Inject]
    private NhanVienDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private AttendanceDepartmentDataProvider DepartmentDataProvider { get; set; } = default!;

    [Inject]
    private AttendancePositionDataProvider PositionDataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private IHrmDialogService DialogService { get; set; } = default!;

    #endregion

    #region State

    private IReadOnlyList<EmployeeRecord> Employees { get; set; } = [];
    private IReadOnlyList<EmployeeRecord> ExportRows { get; set; } = [];
    private IReadOnlyList<AttendanceDepartmentRecord> DepartmentOptions { get; set; } = [];
    private IReadOnlyList<AttendancePositionRecord> PositionOptions { get; set; } = [];
    private IReadOnlyList<EmployeeSummaryBadge> SummaryBadges { get; set; } = BuildSummaryBadges(EmptySummary);
    private CreateEmployeeFormModel? CreateEmployeeModel { get; set; }
    private EmployeeRecord? EditingEmployee { get; set; }
    private EmployeeRecord? StatusEmployee { get; set; }
    private IGrid? Grid { get; set; }
    private IGrid? ExportGrid { get; set; }
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    private string ActiveSummaryBadgeKey { get; set; } = SummaryWorkingKey;
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private string? CreateLookupErrorMessage { get; set; }
    private string? StatusChangeErrorMessage { get; set; }
    private bool IsLoading { get; set; }
    private bool IsRefreshing { get; set; }
    private bool IsExporting { get; set; }
    private bool IsSavingEmployee { get; set; }
    private bool IsDeletingEmployee { get; set; }
    private bool IsChangingEmployeeStatus { get; set; }
    private bool IsLoadingCreateLookups { get; set; }
    private bool IsCreatePopupVisible { get; set; }
    private bool IsStatusPopupVisible { get; set; }
    private bool IsChangingPageSize { get; set; }
    private int PageSize { get; set; } = 100;
    private int CurrentPageIndex { get; set; }
    private int TotalEmployeeCount { get; set; }
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;
    private bool HasRequestedListLoad { get; set; }
    private EmployeeEmploymentStatus PendingStatus { get; set; } = EmployeeEmploymentStatus.Probation;
    private DateTime StatusEffectiveDate { get; set; } = DateTime.Today;

    #endregion

    #region Derived State

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool IsScreenOperationInProgress =>
        IsRefreshing
        || IsExporting
        || IsSavingEmployee
        || IsDeletingEmployee
        || IsChangingEmployeeStatus
        || IsLoadingCreateLookups;
    private bool CanInteract => !IsLoading && !IsChangingPageSize && !IsScreenOperationInProgress && !HasLoadError;
    private bool CanCreate => !IsLoading && !IsChangingPageSize && !IsScreenOperationInProgress;
    private bool CanRefreshEmployees => !IsLoading && !IsChangingPageSize && !IsScreenOperationInProgress;
    private bool CanExport => !IsLoading && !IsChangingPageSize && !IsScreenOperationInProgress && HasRequestedListLoad && TotalEmployeeCount > 0;
    private bool ShowLoadingPanel =>
        IsLoading
        || IsRefreshing
        || IsExporting
        || IsDeletingEmployee
        || IsChangingEmployeeStatus
        || IsChangingPageSize;
    private bool CanSaveStatusChange =>
        StatusEmployee is not null
        && !IsChangingEmployeeStatus;
    private bool CanBrowsePages => !ShowLoadingPanel && !HasLoadError && HasRequestedListLoad && TotalEmployeeCount > 0;
    private IReadOnlyList<EmployeeRecord> VisibleEmployees => Employees;
    private int TotalPageCount => TotalEmployeeCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalEmployeeCount / (double)PageSize);
    private int CurrentPageNumber => TotalEmployeeCount == 0 ? 0 : CurrentPageIndex + 1;
    private int CurrentPageStartRecord => TotalEmployeeCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalEmployeeCount == 0
        ? 0
        : Math.Min(TotalEmployeeCount, CurrentPageIndex * PageSize + VisibleEmployees.Count);
    private string PagerSummaryText => TotalEmployeeCount == 0
        ? "Chưa có dữ liệu nhân viên"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalEmployeeCount:N0} nhân viên";
    private string LoadingText => IsRefreshing
        ? "Đang quét dữ liệu người dùng từ hồ sơ máy chấm công..."
        : IsDeletingEmployee
            ? "Đang xóa nhân viên..."
        : IsChangingEmployeeStatus
            ? "Đang cập nhật tình trạng nhân viên..."
        : IsExporting
            ? "Đang chuẩn bị toàn bộ dữ liệu nhân viên để xuất file..."
        : IsChangingPageSize
            ? "Đang cập nhật số dòng hiển thị..."
            : HrmUiDefaults.LoadingText;
    private string EmptyStateTitle => !HasRequestedListLoad
        ? "Chưa tải danh sách nhân viên"
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Không tìm thấy nhân viên phù hợp"
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Chưa có nhân viên"
            : "Không có nhân viên ở trạng thái đã chọn";
    private string EmptyStateMessage => !HasRequestedListLoad
        ? "Chọn trạng thái hoặc nhập từ khóa nếu cần, rồi bấm Xem để tải danh sách từ server."
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Hãy thử từ khóa khác hoặc xóa bộ lọc tìm kiếm để xem thêm dữ liệu."
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Danh sách nhân viên sẽ hiển thị tại đây khi dữ liệu đã được đồng bộ vào hệ thống."
            : "Hãy chuyển sang nhóm trạng thái khác hoặc tải lại danh sách để xem thêm dữ liệu.";
    private string EmptyStateActionText => !HasRequestedListLoad
        ? "Xem danh sách"
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Xóa tìm kiếm"
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Tải lại"
            : "Xem tất cả";

    #endregion

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    #region UI Entry Points

    #endregion

    #region Data Loading

    private Task RequestListLoadAsync(bool allowDuringScreenOperation = false)
    {
        // Chỉ entry point do người dùng chủ động gọi mới mở danh sách. ReloadAsync tự nó
        // không được phép biến lần render đầu thành một lần tải dữ liệu.
        HasRequestedListLoad = true;
        return ReloadAsync(allowDuringScreenOperation);
    }

    private async Task ReloadAsync(bool allowDuringScreenOperation = false)
    {
        if (!HasRequestedListLoad
            || disposalTokenSource.IsCancellationRequested
            || (!allowDuringScreenOperation && IsScreenOperationInProgress))
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        CancelActiveLoad();
        if (!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
                   && reloadProcessedVersion < Volatile.Read(ref reloadRequestedVersion))
            {
                var requestVersion = Volatile.Read(ref reloadRequestedVersion);
                reloadProcessedVersion = requestVersion;
                await ReloadCoreAsync(requestVersion, CaptureLoadRequest());
            }
        }
        finally
        {
            reloadGate.Release();
        }
    }

    private async Task ReloadCoreAsync(int requestVersion, NhanVienLoadRequest request)
    {
        LoadErrorMessage = null;
        IsLoading = true;

        using var requestTokenSource = BeginLoad();
        try
        {
            var summary = await DataProvider.GetSummaryAsync(
                request.SearchText,
                requestTokenSource.Token);
            var page = await DataProvider.LoadPageAsync(
                new NhanVienListQuery(
                    request.SearchText,
                    GetStatusesForBadge(request.ActiveSummaryBadgeKey),
                    request.PageIndex * request.PageSize,
                    request.PageSize),
                requestTokenSource.Token);

            if (ShouldDiscardLoadResult(requestVersion, request))
            {
                return;
            }

            if (page.TotalCount > 0)
            {
                var maximumPageIndex = Math.Max(0, (int)Math.Ceiling(page.TotalCount / (double)request.PageSize) - 1);
                if (request.PageIndex > maximumPageIndex)
                {
                    CurrentPageIndex = maximumPageIndex;
                    Interlocked.Increment(ref reloadRequestedVersion);
                    return;
                }
            }

            SummaryBadges = BuildSummaryBadges(summary);
            Employees = page.Rows;
            TotalEmployeeCount = page.TotalCount;
        }
        catch (OperationCanceledException) when (
            disposalTokenSource.IsCancellationRequested || ShouldDiscardLoadResult(requestVersion, request))
        {
            // Request bị thay thế hoặc component đã dispose là luồng bình thường, không hiển thị lỗi cho người dùng.
        }
        catch (Exception)
        {
            if (ShouldDiscardLoadResult(requestVersion, request))
            {
                return;
            }

            Employees = [];
            TotalEmployeeCount = 0;
            SummaryBadges = BuildSummaryBadges(EmptySummary);
            LoadErrorMessage = "Có lỗi khi tải dữ liệu nhân viên. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách nhân viên.");
        }
        finally
        {
            if (ReferenceEquals(activeLoadTokenSource, requestTokenSource))
            {
                activeLoadTokenSource = null;
            }

            IsLoading = false;
        }
    }

    private NhanVienLoadRequest CaptureLoadRequest() =>
        new(SearchText, ActiveSummaryBadgeKey, CurrentPageIndex, PageSize);

    private bool ShouldDiscardLoadResult(int requestVersion, NhanVienLoadRequest request) =>
        requestVersion != Volatile.Read(ref reloadRequestedVersion)
        || CaptureLoadRequest() != request;

    private CancellationTokenSource BeginLoad()
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        activeLoadTokenSource = cancellationTokenSource;
        return cancellationTokenSource;
    }

    private void CancelActiveLoad() => activeLoadTokenSource?.Cancel();

    private async Task RefreshEmployeesFromAttendanceAsync()
    {
        if (disposalTokenSource.IsCancellationRequested || !CanRefreshEmployees)
        {
            return;
        }

        LoadErrorMessage = null;
        IsRefreshing = true;

        try
        {
            CurrentPageIndex = 0;
            var result = await DataProvider.RefreshFromDeviceUserProfilesAsync(disposalTokenSource.Token);
            await RequestListLoadAsync(allowDuringScreenOperation: true);

            if (!result.SourceAvailable)
            {
                ToastService.ShowWarning(result.Note ?? "Không tìm thấy nguồn hồ sơ máy chấm công để đồng bộ nhân viên.");
                return;
            }

            if (result.SourceRowCount == 0)
            {
                ToastService.ShowInfo(BuildRefreshSummaryMessage(result));
                return;
            }

            ToastService.ShowSuccess(BuildRefreshSummaryMessage(result));
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
            ToastService.ShowError("Không thể làm mới danh sách nhân viên từ hồ sơ máy chấm công.");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #endregion

    #region Toolbar And Screen Actions

    private Task RetryListLoadAsync() => ReloadAsync();

    private async Task OnAddEmployeeClick()
    {
        if (!CanCreate)
        {
            return;
        }

        try
        {
            EditErrorMessage = null;
            CreateLookupErrorMessage = null;
            EditingEmployee = null;
            CreateEmployeeModel = new CreateEmployeeFormModel();
            DepartmentOptions = [];
            PositionOptions = [];
            IsCreatePopupVisible = true;
            IsLoadingCreateLookups = true;
            await InvokeAsync(StateHasChanged);

            await LoadCreateLookupOptionsAsync();
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
            CreateLookupErrorMessage = "Không thể tải danh mục phòng ban/chức vụ. Vui lòng thử lại.";
            ToastService.ShowError(CreateLookupErrorMessage);
        }
        finally
        {
            IsLoadingCreateLookups = false;
        }
    }

    private async Task OnToolbarItemClick(ToolbarItemClickEventArgs args)
    {
        if (args.ItemName == AddEmployeeToolbarItemName)
        {
            await OnAddEmployeeClick();
        }
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private async Task OnSummaryBadgeClick(string badgeKey)
    {
        if (!CanInteract)
        {
            return;
        }

        if (string.Equals(badgeKey, ActiveSummaryBadgeKey, StringComparison.Ordinal))
        {
            return;
        }

        ActiveSummaryBadgeKey = badgeKey;
        CurrentPageIndex = 0;
        if (HasRequestedListLoad)
        {
            await ReloadAsync();
        }
    }

    private async Task OnEmptyStateActionClick()
    {
        if (!HasRequestedListLoad)
        {
            CurrentPageIndex = 0;
            await RequestListLoadAsync();
            return;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            SearchText = null;
            CurrentPageIndex = 0;
            await ReloadAsync(allowDuringScreenOperation: true);
            return;
        }

        if (ActiveSummaryBadgeKey != SummaryAllKey)
        {
            ActiveSummaryBadgeKey = SummaryAllKey;
            CurrentPageIndex = 0;
            await ReloadAsync(allowDuringScreenOperation: true);
            return;
        }

        await ReloadAsync();
    }

    private Task OnSearchTextChanged(string? value)
    {
        if (!CanInteract)
        {
            return Task.CompletedTask;
        }

        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        CurrentPageIndex = 0;
        return HasRequestedListLoad ? ReloadAsync() : Task.CompletedTask;
    }

    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : PageSizeOptions[0];
        if (PageSize == normalizedValue)
        {
            return;
        }

        // Giữ bản ghi đầu đang xem khi thay page size để người dùng không bị nhảy về đầu danh sách.
        var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
        IsChangingPageSize = true;
        PageSize = normalizedValue;
        CurrentPageIndex = firstVisibleRecordIndex / PageSize;

        try
        {
            if (HasRequestedListLoad)
            {
                await ReloadAsync(allowDuringScreenOperation: true);
            }
            else
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        finally
        {
            IsChangingPageSize = false;
        }
    }

    private async Task OnActivePageIndexChanged(int value)
    {
        if (!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if (CurrentPageIndex == normalizedValue)
        {
            return;
        }

        CurrentPageIndex = normalizedValue;
        await ReloadAsync();
    }

    #endregion

    #region Detail Popup Actions

    private async Task OnCreateEmployeeRequested(CreateEmployeeFormModel model)
    {
        if (IsSavingEmployee || IsLoadingCreateLookups || !string.IsNullOrWhiteSpace(CreateLookupErrorMessage))
        {
            return;
        }

        EditErrorMessage = null;
        IsSavingEmployee = true;

        try
        {
            await DataProvider.CreateAsync(model, disposalTokenSource.Token);
            await RequestListLoadAsync(allowDuringScreenOperation: true);
            IsCreatePopupVisible = false;
            ResetCreatePopupState();
            ToastService.ShowSuccess("Đã thêm mới nhân viên.");
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
            EditErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể thêm mới nhân viên.";
            ToastService.ShowError(EditErrorMessage);
        }
        finally
        {
            IsSavingEmployee = false;
        }
    }

    private async Task OpenEditEmployeePopupAsync(EmployeeRecord employee)
    {
        if (!CanInteract)
        {
            return;
        }

        try
        {
            EditErrorMessage = null;
            CreateLookupErrorMessage = null;
            EditingEmployee = employee;
            CreateEmployeeModel = MapToEditorModel(employee);
            DepartmentOptions = [];
            PositionOptions = [];
            IsCreatePopupVisible = true;
            IsLoadingCreateLookups = true;
            await InvokeAsync(StateHasChanged);
            await LoadCreateLookupOptionsAsync();
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            // Component đã dispose; không tiếp tục thao tác UI.
        }
        catch (Exception)
        {
            CreateLookupErrorMessage = "Không thể tải danh mục phòng ban/chức vụ. Vui lòng thử lại.";
            ToastService.ShowError(CreateLookupErrorMessage);
        }
        finally
        {
            IsLoadingCreateLookups = false;
        }
    }

    private Task OnEmployeePopupVisibleChanged(bool visible)
    {
        IsCreatePopupVisible = visible;
        if (!visible && !IsSavingEmployee)
        {
            ResetCreatePopupState();
        }

        return Task.CompletedTask;
    }

    private async Task OnUpdateEmployeeRequested(CreateEmployeeFormModel model)
    {
        if (IsSavingEmployee || IsLoadingCreateLookups || !string.IsNullOrWhiteSpace(CreateLookupErrorMessage))
        {
            return;
        }

        if (EditingEmployee is null)
        {
            EditErrorMessage = "Không xác định được nhân viên cần điều chỉnh.";
            ToastService.ShowError(EditErrorMessage);
            return;
        }

        EditErrorMessage = null;
        IsSavingEmployee = true;
        try
        {
            await DataProvider.UpdateAsync(EditingEmployee, model, disposalTokenSource.Token);
            await RequestListLoadAsync(allowDuringScreenOperation: true);
            IsCreatePopupVisible = false;
            ResetCreatePopupState();
            ToastService.ShowSuccess("Đã cập nhật thông tin nhân viên.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            // Component đã dispose; không tiếp tục thao tác UI.
        }
        catch (InvalidOperationException ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể cập nhật thông tin nhân viên.";
            ToastService.ShowError(EditErrorMessage);
        }
        finally
        {
            IsSavingEmployee = false;
        }
    }

    private async Task DeleteEmployeeAsync(EmployeeRecord employee)
    {
        if (!CanInteract)
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có chắc muốn xóa nhân viên `{employee.EmployeeLookupText}`?",
            title: "Xác nhận xóa",
            okText: "Xóa",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Danger);
        if (!confirmed || !CanInteract)
        {
            return;
        }

        IsDeletingEmployee = true;
        try
        {
            await DataProvider.DeleteAsync(employee, disposalTokenSource.Token);
            await RequestListLoadAsync(allowDuringScreenOperation: true);
            ToastService.ShowSuccess("Đã xóa nhân viên.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            // Component đã dispose; không tiếp tục thao tác UI.
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xóa nhân viên.");
        }
        finally
        {
            IsDeletingEmployee = false;
        }
    }

    private bool CanChangeStatus(EmployeeRecord employee) =>
        CanInteract && employee.EmploymentStatus.HasValue;

    private void OpenStatusPopup(EmployeeRecord employee)
    {
        if (!CanChangeStatus(employee))
        {
            return;
        }

        StatusEmployee = employee;
        PendingStatus = employee.EmploymentStatus ?? EmployeeEmploymentStatus.Probation;
        StatusEffectiveDate = PendingStatus switch
        {
            EmployeeEmploymentStatus.Resigned => employee.ResignedDate ?? DateTime.Today,
            EmployeeEmploymentStatus.Official => employee.SeniorityStartDate ?? DateTime.Today,
            _ => DateTime.Today
        };
        StatusChangeErrorMessage = null;
        IsStatusPopupVisible = true;
    }

    private void CloseStatusPopup()
    {
        if (IsChangingEmployeeStatus)
        {
            return;
        }

        StatusEmployee = null;
        StatusChangeErrorMessage = null;
        IsStatusPopupVisible = false;
    }

    private Task OnPendingStatusChanged(EmployeeEmploymentStatus status)
    {
        if (PendingStatus != status)
        {
            PendingStatus = status;
            StatusEffectiveDate = DateTime.Today;
        }

        StatusChangeErrorMessage = null;
        return Task.CompletedTask;
    }

    private async Task SaveStatusChangeAsync()
    {
        if (!CanSaveStatusChange || StatusEmployee is null)
        {
            return;
        }

        StatusChangeErrorMessage = null;
        IsChangingEmployeeStatus = true;
        try
        {
            DateTime? seniorityStartDate = PendingStatus == EmployeeEmploymentStatus.Official
                ? StatusEffectiveDate
                : null;
            DateTime? resignedDate = PendingStatus == EmployeeEmploymentStatus.Resigned
                ? StatusEffectiveDate
                : null;
            await DataProvider.ChangeStatusAsync(
                StatusEmployee,
                PendingStatus,
                seniorityStartDate,
                resignedDate,
                disposalTokenSource.Token);
            await RequestListLoadAsync(allowDuringScreenOperation: true);
            StatusEmployee = null;
            IsStatusPopupVisible = false;
            ToastService.ShowSuccess("Đã cập nhật tình trạng nhân viên.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            // Component đã dispose; không tiếp tục thao tác UI.
        }
        catch (InvalidOperationException ex)
        {
            StatusChangeErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            StatusChangeErrorMessage = "Không thể cập nhật tình trạng nhân viên.";
            ToastService.ShowError(StatusChangeErrorMessage);
        }
        finally
        {
            IsChangingEmployeeStatus = false;
        }
    }

    #endregion

    #region Export Actions

    private Task ExportAllDataToExcelAsync() => ExportAllAsync(
        () => ExportGrid!.ExportToXlsxAsync("nhan-vien"),
        "Đã bắt đầu xuất toàn bộ danh sách nhân viên ra Excel.");

    private Task ExportAllDataToPdfAsync() => ExportAllAsync(
        () => ExportGrid!.ExportToPdfAsync("nhan-vien"),
        "Đã bắt đầu xuất toàn bộ danh sách nhân viên ra PDF.");

    private async Task ExportAllAsync(Func<Task> exportAction, string successMessage)
    {
        if (!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsExporting = true;
        try
        {
            ExportRows = await DataProvider.LoadAllForExportAsync(disposalTokenSource.Token);
            if (ExportRows.Count == 0)
            {
                ToastService.ShowInfo("Không có dữ liệu nhân viên đang hoạt động để xuất file.");
                return;
            }

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
            // Component đã được dispose; không hiển thị lỗi cho người dùng.
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu nhân viên.");
        }
        finally
        {
            ExportRows = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;

            if (!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    #endregion

    #region Create Popup Helpers

    private void ResetCreatePopupState()
    {
        CreateEmployeeModel = null;
        EditingEmployee = null;
        EditErrorMessage = null;
        CreateLookupErrorMessage = null;
        IsSavingEmployee = false;
        IsLoadingCreateLookups = false;
    }

    private async Task LoadCreateLookupOptionsAsync()
    {
        DepartmentOptions = await DepartmentDataProvider.GetAsync(disposalTokenSource.Token);
        PositionOptions = await PositionDataProvider.GetAsync(disposalTokenSource.Token);
    }

    #endregion

    #region Query And Mapping Helpers

    private static string FormatOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();

    private static CreateEmployeeFormModel MapToEditorModel(EmployeeRecord employee) =>
        new()
        {
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            DepartmentId = employee.DepartmentId,
            PositionId = employee.PositionId,
            Status = employee.EmploymentStatus ?? EmployeeEmploymentStatus.Probation,
            HireDate = employee.HireDate,
            OriginalUpdatedAtUtc = employee.UpdatedAtUtc ?? employee.CreatedAtUtc
        };

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = FormatOptional(value);
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        if (searchText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while (true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"employee-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if (builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    private static IReadOnlyList<EmployeeSummaryBadge> BuildSummaryBadges(
        EmployeeSummaryDto summary)
    {
        return
        [
            new(SummaryWorkingKey, "Đang làm việc", "ĐLV", summary.WorkingCount),
            new(SummaryProbationKey, "Thử việc", "TV", summary.ProbationCount),
            new(SummaryOfficialKey, "Chính thức", "CT", summary.OfficialCount),
            new(SummaryResignedKey, "Nghỉ việc", "NV", summary.ResignedCount),
            new(SummaryAllKey, "Tất cả", "TC", summary.TotalCount)
        ];
    }

    private static IReadOnlyList<int>? GetStatusesForBadge(string badgeKey) =>
        badgeKey switch
        {
            SummaryWorkingKey => [(int)EmployeeEmploymentStatus.Probation, (int)EmployeeEmploymentStatus.Official],
            SummaryProbationKey => [(int)EmployeeEmploymentStatus.Probation],
            SummaryOfficialKey => [(int)EmployeeEmploymentStatus.Official],
            SummaryResignedKey => [(int)EmployeeEmploymentStatus.Resigned],
            _ => null
        };

    #endregion

    #region Message Builders

    private static string BuildRefreshSummaryMessage(EmployeeRefreshResult result)
    {
        if (!result.SourceAvailable)
        {
            return result.Note ?? "Không tìm thấy nguồn hồ sơ máy chấm công để đồng bộ.";
        }

        if (result.SourceRowCount == 0)
        {
            return result.Note ?? "Bảng device_user_profiles hiện chưa có dữ liệu hợp lệ để đồng bộ.";
        }

        return $"Đã quét {result.SourceRowCount} hồ sơ từ device_user_profiles. Tạo mới {result.CreatedCount}, cập nhật {result.UpdatedCount}, bỏ qua {result.SkippedCount}.";
    }

    #endregion

    #region Disposal And Nested Types

    public void Dispose()
    {
        CancelActiveLoad();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    private static EmployeeSummaryDto EmptySummary { get; } = new(0, 0, 0, 0, 0);

    private sealed record NhanVienLoadRequest(
        string? SearchText,
        string ActiveSummaryBadgeKey,
        int PageIndex,
        int PageSize);

    private sealed record EmployeeSummaryBadge(string Key, string Label, string ShortLabel, int Count);

    private sealed record EmployeeStatusOption(EmployeeEmploymentStatus Value, string Text);

    private static IReadOnlyList<EmployeeStatusOption> EmployeeStatusOptions { get; } =
    [
        new(EmployeeEmploymentStatus.Probation, "Thử việc"),
        new(EmployeeEmploymentStatus.Official, "Chính thức"),
        new(EmployeeEmploymentStatus.Resigned, "Nghỉ việc")
    ];

    #endregion
}
