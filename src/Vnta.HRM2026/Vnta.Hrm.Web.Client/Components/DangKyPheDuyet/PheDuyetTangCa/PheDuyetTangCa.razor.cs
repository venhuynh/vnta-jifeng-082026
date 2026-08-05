using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.DangKyPheDuyet.PheDuyetTangCa;

public partial class PheDuyetTangCa : IDisposable
{
    private const string DefaultLoadingText = "Đang tải danh sách phiếu chờ phê duyệt...";
    private const string LoadErrorDefaultMessage = "Không thể tải dữ liệu vào lúc này. Vui lòng thử lại.";
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly IReadOnlyList<DayTypeOption> DayTypeOptions =
    [
        new(null, "Tất cả"),
        new(AttendanceWorkCalendarDayType.Regular, "Ngày thường"),
        new(AttendanceWorkCalendarDayType.DayOff, "Ngày nghỉ"),
        new(AttendanceWorkCalendarDayType.Holiday, "Ngày lễ")
    ];

    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject]
    private OvertimeRegistrationDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IGrid? Grid { get; set; }
    private IReadOnlyList<OvertimeRegistrationListItemDto> Requests { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private DateTime? WorkDateFilter { get; set; }
    private AttendanceWorkCalendarDayType? DayTypeFilter { get; set; }
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; }
    private bool IsDetailsPopupVisible { get; set; }
    private bool IsDetailsPopupLoading { get; set; }
    private Guid? DetailsRequestId { get; set; }
    private OvertimeRegistrationListItemDto? DetailsRequest { get; set; }
    private string? DetailsPopupErrorMessage { get; set; }
    private bool IsStatusActionPopupVisible { get; set; }
    private OvertimeRegistrationStatus? PendingStatusAction { get; set; }
    private IReadOnlyList<Guid> PendingStatusRequestIds { get; set; } = [];
    private string? StatusActionErrorMessage { get; set; }

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool ShowLoadingPanel => IsLoading || IsProcessing;
    private bool CanChangeFilters => !ShowLoadingPanel && !HasLoadError;
    private bool CanView => CanChangeFilters;
    private bool CanRefresh => CanChangeFilters;
    private bool CanUseColumnChooser => CanChangeFilters && Requests.Count > 0;
    private bool CanReviewSelected => CanChangeFilters && GetSelectedRequests().Count > 0;
    private int RegisteredEmployeeCount => Requests.Sum(GetRegisteredEmployeeCount);
    private int RegularDayCount => Requests.Count(request => request.DayType == AttendanceWorkCalendarDayType.Regular);
    private int SpecialDayCount => Requests.Count - RegularDayCount;
    private string LoadingText => IsProcessing ? "Đang cập nhật trạng thái phiếu tăng ca..." : DefaultLoadingText;
    private string DetailsPopupTitle => DetailsRequest is null
        ? "Chi tiết phiếu tăng ca"
        : $"Chi tiết phiếu tăng ca · {FormatDate(DetailsRequest.WorkDate)} · {DetailsRequest.WorkshopName}";
    private bool HasActiveFilters => WorkDateFilter.HasValue || DayTypeFilter.HasValue || !string.IsNullOrWhiteSpace(SearchText);
    private string FilterSummary => WorkDateFilter.HasValue ? $"Ngày {WorkDateFilter.Value:dd/MM/yyyy}" : "Tất cả ngày";
    private string EmptyStateTitle => HasActiveFilters
        ? "Không tìm thấy phiếu tăng ca chờ phê duyệt phù hợp"
        : "Không có phiếu tăng ca chờ phê duyệt";
    private string EmptyStateMessage => HasActiveFilters
        ? "Hãy thay đổi hoặc xóa bộ lọc để xem thêm dữ liệu."
        : "Các phiếu đăng ký tăng ca mới sẽ xuất hiện ở đây sau khi được gửi phê duyệt.";
    private string EmptyStateActionText => HasActiveFilters ? "Xóa bộ lọc" : "Làm mới";

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
        await base.OnInitializedAsync();
    }

    private async Task ReloadAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsLoading = true;
        LoadErrorMessage = null;
        try
        {
            Requests = await SearchPendingRequestsAsync(disposalTokenSource.Token);
            await ClearSelectionAsync();
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            Requests = [];
            LoadErrorMessage = LoadErrorDefaultMessage;
            ToastService.ShowError("Không thể tải danh sách phiếu chờ phê duyệt.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task RetryAsync() => ReloadAsync();

    private async Task RefreshAsync()
    {
        if (!CanRefresh)
        {
            return;
        }

        await ReloadAsync();
        if (!HasLoadError && !disposalTokenSource.IsCancellationRequested)
        {
            ToastService.ShowInfo("Đã làm mới danh sách phiếu tăng ca chờ phê duyệt.");
        }
    }

    private async Task OnSearchTextChangedAsync(string? value)
    {
        var normalizedSearchText = NormalizeOptional(value);
        if (string.Equals(SearchText, normalizedSearchText, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedSearchText;
        await ReloadAsync();
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task ClearFiltersAsync()
    {
        WorkDateFilter = null;
        DayTypeFilter = null;
        SearchText = null;
        await ReloadAsync();
    }

    private Task ShowColumnChooserAsync()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private bool CanOperateOnRow(OvertimeRegistrationListItemDto request) => CanChangeFilters && request.Status == OvertimeRegistrationStatus.PendingApproval;

    private bool CanOpenDetails(OvertimeRegistrationListItemDto request) => CanOperateOnRow(request);

    private bool CanReviewRow(OvertimeRegistrationListItemDto request) => CanOperateOnRow(request);

    private Task OpenApproveSelectedAsync() => OpenStatusActionPopupAsync(
        OvertimeRegistrationStatus.Approved,
        GetSelectedRequests().Select(request => request.Id).ToArray());

    private Task OpenReturnSelectedAsync() => OpenStatusActionPopupAsync(
        OvertimeRegistrationStatus.Returned,
        GetSelectedRequests().Select(request => request.Id).ToArray());

    private Task OpenRejectSelectedAsync() => OpenStatusActionPopupAsync(
        OvertimeRegistrationStatus.Rejected,
        GetSelectedRequests().Select(request => request.Id).ToArray());

    private Task OpenStatusActionPopupAsync(OvertimeRegistrationStatus targetStatus, IReadOnlyList<Guid> requestIds)
    {
        var normalizedRequestIds = requestIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (!CanChangeFilters || normalizedRequestIds.Length == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một phiếu chờ phê duyệt.");
            return Task.CompletedTask;
        }

        PendingStatusAction = targetStatus;
        PendingStatusRequestIds = normalizedRequestIds;
        StatusActionErrorMessage = null;
        IsStatusActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private async Task ConfirmStatusActionAsync()
    {
        if (IsProcessing || PendingStatusAction is null || PendingStatusRequestIds.Count == 0)
        {
            return;
        }

        var targetStatus = PendingStatusAction.Value;
        var requestIds = PendingStatusRequestIds;
        IsProcessing = true;
        StatusActionErrorMessage = null;
        try
        {
            await DataProvider.ChangeStatusAsync(
                new ChangeOvertimeRegistrationStatusRequest
                {
                    Ids = requestIds,
                    TargetStatus = targetStatus
                },
                disposalTokenSource.Token);

            CloseStatusActionPopupCore();
            await ReloadAsync();
            ToastService.ShowSuccess(GetStatusActionSuccessMessage(targetStatus));
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            StatusActionErrorMessage = "Không thể cập nhật trạng thái phiếu tăng ca. Vui lòng kiểm tra dữ liệu và thử lại.";
            ToastService.ShowError("Không thể cập nhật trạng thái phiếu tăng ca.");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private Task OnStatusActionPopupVisibleChangedAsync(bool visible)
    {
        if (!visible && !IsProcessing)
        {
            CloseStatusActionPopupCore();
        }

        return Task.CompletedTask;
    }

    private void CloseStatusActionPopupCore()
    {
        IsStatusActionPopupVisible = false;
        PendingStatusAction = null;
        PendingStatusRequestIds = [];
        StatusActionErrorMessage = null;
    }

    private Task OpenDetailsPopupAsync(OvertimeRegistrationListItemDto request)
    {
        if (!CanOpenDetails(request))
        {
            return Task.CompletedTask;
        }

        DetailsRequestId = request.Id;
        DetailsRequest = request;
        DetailsPopupErrorMessage = null;
        IsDetailsPopupVisible = true;
        return Task.CompletedTask;
    }

    private async Task RefreshDetailsPopupAsync()
    {
        if (DetailsRequestId is not { } requestId || IsDetailsPopupLoading || IsProcessing)
        {
            return;
        }

        IsDetailsPopupLoading = true;
        DetailsPopupErrorMessage = null;
        try
        {
            var refreshedRequests = await DataProvider.SearchAsync(
                new OvertimeRegistrationFilter(null, null, OvertimeRegistrationStatus.PendingApproval, null, 2000),
                disposalTokenSource.Token);
            DetailsRequest = refreshedRequests.SingleOrDefault(request => request.Id == requestId);
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            DetailsPopupErrorMessage = "Không thể tải lại chi tiết phiếu tăng ca. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải lại chi tiết phiếu tăng ca.");
        }
        finally
        {
            IsDetailsPopupLoading = false;
        }
    }

    private Task OnDetailsPopupVisibleChangedAsync(bool visible)
    {
        if (!visible && !IsDetailsPopupLoading && !IsProcessing)
        {
            CloseDetailsPopupCore();
        }

        return Task.CompletedTask;
    }

    private void CloseDetailsPopupCore()
    {
        IsDetailsPopupVisible = false;
        IsDetailsPopupLoading = false;
        DetailsRequestId = null;
        DetailsRequest = null;
        DetailsPopupErrorMessage = null;
    }

    private IReadOnlyList<OvertimeRegistrationListItemDto> GetSelectedRequests() => SelectedDataItems
        .OfType<OvertimeRegistrationListItemDto>()
        .Where(request => request.Status == OvertimeRegistrationStatus.PendingApproval)
        .Where(request => Requests.Any(row => row.Id == request.Id))
        .DistinctBy(request => request.Id)
        .ToArray();

    private async Task<IReadOnlyList<OvertimeRegistrationListItemDto>> SearchPendingRequestsAsync(CancellationToken cancellationToken) =>
        await DataProvider.SearchAsync(
            new OvertimeRegistrationFilter(
                WorkDateFilter.HasValue ? DateOnly.FromDateTime(WorkDateFilter.Value) : null,
                DayTypeFilter,
                OvertimeRegistrationStatus.PendingApproval,
                NormalizeOptional(SearchText)),
            cancellationToken);

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

    private static int GetRegisteredEmployeeCount(OvertimeRegistrationListItemDto request) =>
        request.EmployeeAssignments.Count(employee => employee.AssignmentType != OvertimeEmployeeAssignmentType.None);

    private static string GetTeamSummary(OvertimeRegistrationListItemDto request) => string.Join(", ",
        request.EmployeeAssignments
            .Select(employee => employee.TeamName)
            .Distinct(StringComparer.OrdinalIgnoreCase));

    private static string GetRegistrationMode(OvertimeRegistrationListItemDto request) =>
        request.DayType == AttendanceWorkCalendarDayType.Regular ? "19:00 / 21:00" : "Theo danh sách";

    private static string GetDayTypeText(AttendanceWorkCalendarDayType dayType) =>
        AttendanceWorkCalendarDayTypes.GetDisplayName(dayType);

    private static string GetDayTypeBadgeCssClass(AttendanceWorkCalendarDayType dayType) =>
        dayType == AttendanceWorkCalendarDayType.Regular
            ? "overtime-approval-day-badge overtime-approval-day-badge-regular"
            : "overtime-approval-day-badge overtime-approval-day-badge-special";

    private static string FormatDate(DateOnly value) =>
        value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", DisplayCulture);

    private static string FormatDateTime(DateTime? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm", DisplayCulture)
        : "—";

    private static string GetStatusActionSuccessMessage(OvertimeRegistrationStatus targetStatus) => targetStatus switch
    {
        OvertimeRegistrationStatus.Approved => "Đã phê duyệt phiếu đăng ký tăng ca. Dữ liệu tăng ca đã sẵn sàng cho chấm công.",
        OvertimeRegistrationStatus.Returned => "Đã trả lại phiếu đăng ký tăng ca để người đăng ký chỉnh sửa.",
        OvertimeRegistrationStatus.Rejected => "Đã từ chối phiếu đăng ký tăng ca.",
        _ => "Đã cập nhật trạng thái phiếu đăng ký tăng ca."
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }

    private sealed record DayTypeOption(AttendanceWorkCalendarDayType? Value, string Text);
}
