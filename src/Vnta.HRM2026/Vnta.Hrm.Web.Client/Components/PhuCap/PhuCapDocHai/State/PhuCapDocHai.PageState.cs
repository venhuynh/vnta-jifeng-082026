using System.Globalization;
using DevExpress.Blazor;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Export;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Sections;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHai</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai
{
    #region Derived State

    /// <summary>Giá trị <c>AvailableMonthOptions</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private IReadOnlyList<PhuCapDocHaiMonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;

    /// <summary>Tập bản ghi sau khi áp dụng badge cục bộ trên dữ liệu đã tải.</summary>
    private IReadOnlyList<HazardAllowanceListItemDto> VisibleRecords =>
        LoadedRecords;

    /// <summary>Các dòng của trang hiện tại sau khi áp dụng badge.</summary>
    private IReadOnlyList<HazardAllowanceListItemDto> PagedRecords =>
        LoadedRecords;

    /// <summary>Giá trị <c>SummaryBadges</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private IReadOnlyList<PhuCapDocHaiSummaryBadge> SummaryBadges => BuildSummaryBadges(Summary);

    // Retains the old pager markup during the staged component extraction without rendering it.
    /// <summary>Giá trị <c>HasPendingPeriodChange</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    /// <summary>Giá trị <c>ShowLoadingPanel</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool ShowLoadingPanel =>
        IsLoading
        || IsChangingPageSize
        || IsRefreshingRow
        || IsSavingEdit
        || IsMonthlyWorkPopupLoading
        || IsExporting;
    /// <summary>Giá trị <c>CanInteract</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    /// <summary>Giá trị <c>CanView</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanView => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanChangeFilters</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanChangeFilters => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanOperateOnCurrentDataset</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    /// <summary>Giá trị <c>CanRecalculate</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanRecalculate => CanOperateOnCurrentDataset;
    /// <summary>Cho biết có thể mở thao tác khóa theo kỳ hoặc dòng đã chọn.</summary>
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    /// <summary>Cho biết có thể mở thao tác mở khóa theo kỳ hoặc dòng đã chọn.</summary>
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    /// <summary>Cho biết popup có thể chọn phạm vi các dòng trong trang hiện tại.</summary>
    private bool CanChooseSelectedRowsScope => GetSelectedVisibleRecordCount() > 0;
    /// <summary>Cho biết có thể xác nhận thao tác khóa/mở khóa theo phạm vi hiện chọn.</summary>
    private bool CanConfirmLockAction =>
        CanOperateOnCurrentDataset
        && (string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal)
            || CanChooseSelectedRowsScope);
    /// <summary>Nhãn kỳ được chốt trong popup khóa/mở khóa.</summary>
    private string PendingLockActionPeriodLabel => FormatPayrollPeriod(PendingLockActionMonth, PendingLockActionYear);
    /// <summary>Tiêu đề popup khóa/mở khóa theo trạng thái đích.</summary>
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa phụ cấp độc hại"
        : "Mở khóa phụ cấp độc hại";
    /// <summary>Nội dung yêu cầu xác nhận theo trạng thái đích.</summary>
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi dữ liệu phụ cấp độc hại cần khóa. Dòng đã khóa không thể điều chỉnh hoặc làm mới."
        : "Chọn phạm vi dữ liệu phụ cấp độc hại cần mở khóa để cho phép điều chỉnh hoặc làm mới.";
    /// <summary>Ngữ cảnh lựa chọn phạm vi khóa/mở khóa.</summary>
    private string LockActionScopeContextText => $"Thao tác áp dụng trên snapshot kỳ {PendingLockActionPeriodLabel}.";
    /// <summary>Mô tả phạm vi các dòng đã chọn.</summary>
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {GetSelectedVisibleRecordCount():N0} dòng đang chọn trên trang hiện tại."
        : "Chưa có dòng nào được chọn trên trang hiện tại.";
    /// <summary>Mô tả phạm vi toàn bộ kỳ lương.</summary>
    private string WholePeriodScopeDescription => "Áp dụng cho tất cả dòng phụ cấp độc hại của kỳ, không phụ thuộc bộ lọc hoặc trang hiện tại.";
    /// <summary>Giá trị <c>CanExport</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanExport => CanOperateOnCurrentDataset && VisibleRecords.Count > 0;
    /// <summary>Giá trị <c>CanExportSelected</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanExportSelected => CanExport && GetSelectedVisibleRecordCount() > 0;
    /// <summary>Giá trị <c>TotalPageCount</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private int TotalPageCount => TotalRecordCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
    private int TotalRecordCount => TotalCount;
    /// <summary>Danh sách page size khả dụng theo tổng số dòng hiện tại.</summary>
    private IReadOnlyList<PageSizeOption> AvailablePageSizeOptions => TotalRecordCount > AllPageSize
        ? PageSizeOptions.Where(option => option.Value != AllPageSize).ToArray()
        : PageSizeOptions;
    /// <summary>Cho biết pager đang hiển thị toàn bộ dữ liệu.</summary>
    private bool IsShowingAllRows => PageSize == AllPageSize;
    /// <summary>Mô tả đơn vị của page size hiện tại.</summary>
    private string PageSizeDescription => IsShowingAllRows ? "tất cả dòng" : "dòng/trang";
    /// <summary>Chỉ số dòng đầu tiên của trang hiện tại.</summary>
    private int CurrentPageStartRecord => TotalRecordCount == 0
        ? 0
        : PageIndex * PageSize + 1;
    /// <summary>Chỉ số dòng cuối cùng của trang hiện tại.</summary>
    private int CurrentPageEndRecord => TotalRecordCount == 0
        ? 0
        : Math.Min(TotalRecordCount, PageIndex * PageSize + PagedRecords.Count);
    /// <summary>Cho biết có thể điều hướng giữa các trang của bộ dữ liệu đang áp dụng.</summary>
    private bool CanBrowsePages => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    /// <summary>Nội dung tóm tắt của pager tùy biến.</summary>
    private string PagerSummaryText => !HasRequestedData || HasLoadError || TotalRecordCount == 0
        ? "Chưa có trang dữ liệu"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";
    /// <summary>Cho biết có thể cập nhật trạng thái hưởng của toàn bộ các dòng đang chọn.</summary>
    private bool CanSetSelectedEntitlement =>
        CanOperateOnCurrentDataset
        && GetSelectedVisibleRecords() is { Count: > 0 } selectedRecords
        && selectedRecords.All(record => !record.IsLocked);
    /// <summary>Giá trị <c>CanSaveEdit</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool CanSaveEdit =>
        IsEditPopupVisible
        && !IsSavingEdit
        && !HasLoadError
        && !HasPendingPeriodChange
        && EditModel.PayrollAllowanceSummaryRecordId != Guid.Empty;
    /// <summary>Kiểm tra điều kiện cho luồng <c>CanRefreshRow</c>.</summary>
    private bool CanRefreshRow(HazardAllowanceListItemDto record) => CanOperateOnCurrentDataset && !record.IsLocked;
    /// <summary>Kiểm tra điều kiện cho luồng <c>CanToggleLock</c>.</summary>
    private bool CanToggleLock(HazardAllowanceListItemDto record) => CanOperateOnCurrentDataset;
    /// <summary>Kiểm tra điều kiện cho luồng <c>CanViewMonthlyWork</c>.</summary>
    private bool CanViewMonthlyWork(HazardAllowanceListItemDto record) =>
        CanOperateOnCurrentDataset && record.EmployeeId != Guid.Empty;
    /// <summary>Giá trị <c>HasActiveSummaryBadge</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool HasActiveSummaryBadge => !string.Equals(ActiveSummaryBadgeKey, SummaryAllKey, StringComparison.Ordinal);
    /// <summary>Giá trị <c>HasActiveSearch</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchText);
    /// <summary>Cho biết empty state hiện tại có bộ lọc cần đặt lại.</summary>
    private bool CanResetFilters => HasActiveSearch || HasActiveSummaryBadge;
    /// <summary>Giá trị <c>CurrentPeriodLabel</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private string CurrentPeriodLabel => FormatPayrollPeriod(ToolbarMonth, ToolbarYear);
    /// <summary>Giá trị <c>AppliedPeriodLabel</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private string AppliedPeriodLabel => FormatPayrollPeriod(AppliedMonth, AppliedYear);
    /// <summary>Giá trị <c>DisplayedRulesPayrollPeriod</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private string DisplayedRulesPayrollPeriod => HasRequestedData ? AppliedPeriodLabel : CurrentPeriodLabel;
    /// <summary>Giá trị <c>EmptyStateTitle</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private string EmptyStateTitle => HasLoadError
        ? "Không thể tải dữ liệu phụ cấp độc hại"
        : !HasRequestedData
        ? "Chưa tải dữ liệu phụ cấp độc hại"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
            : CanResetFilters
                ? "Không tìm thấy kết quả phù hợp"
                : "Chưa có dữ liệu phụ cấp độc hại";

    /// <summary>Giá trị <c>EmptyStateMessage</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private string EmptyStateMessage => HasLoadError
        ? "Hãy bấm Tải lại hoặc Xem để thử tải dữ liệu lại. Chi tiết lỗi đã được gửi qua thông báo hệ thống."
        : !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để tải dữ liệu khi bạn sẵn sàng."
        : HasPendingPeriodChange
            ? $"Bạn đã đổi kỳ lương sang {CurrentPeriodLabel}. Nhấn Xem để tải dữ liệu phụ cấp độc hại của kỳ này."
            : CanResetFilters
                ? "Hãy điều chỉnh từ khóa tìm kiếm hoặc trạng thái để xem thêm dữ liệu."
                : $"Nhấn Tính lại để tính phụ cấp độc hại của kỳ {AppliedPeriodLabel} từ bảng công và cập nhật bảng tổng hợp.";

    /// <summary>Giá trị <c>EmptyStateActionText</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private string EmptyStateActionText => HasLoadError
        ? "Tải lại"
        : !HasRequestedData || HasPendingPeriodChange
        ? "Xem dữ liệu"
        : CanResetFilters
            ? "Đặt lại bộ lọc"
            : "Tải lại";

    #endregion
    #region Shared Helpers

    /// <summary>Thực hiện xử lý cho luồng <c>BeginBusyState</c>.</summary>
    private void BeginBusyState(string loadingText)
    {
        HasLoadError = false;
        LoadingText = loadingText;
        IsLoading = true;
    }

    /// <summary>Thực hiện xử lý cho luồng <c>EndBusyState</c>.</summary>
    private void EndBusyState()
    {
        IsLoading = false;
        LoadingText = DefaultLoadingText;
    }

    /// <summary>Tạo cho luồng <c>BuildBaseFilter</c>.</summary>
    private HazardAllowanceFilter BuildBaseFilter() =>
        new(
            AppliedMonth,
            AppliedYear,
            HazardAllowanceLockState.All,
            SearchText,
            SummaryBucket: HazardAllowanceSummaryBucket.All);

    private HazardAllowanceFilter BuildPageFilter() =>
        BuildBaseFilter() with
        {
            SummaryBucket = MapSummaryBucket(ActiveSummaryBadgeKey),
            Take = PageSize,
            Skip = PageIndex * PageSize,
            IncludeTotalCount = true
        };

    /// <summary>Tạo filter xuất tương ứng badge đang chọn; export không bị giới hạn bởi trang UI.</summary>
    private HazardAllowanceFilter BuildExportFilter() =>
        BuildBaseFilter() with
        {
            SummaryBucket = MapSummaryBucket(ActiveSummaryBadgeKey)
        };

    /// <summary>Đưa chỉ số trang về phạm vi hợp lệ của tổng số dòng hiện tại.</summary>
    private void ClampPageIndex()
    {
        PageIndex = Math.Clamp(PageIndex, 0, Math.Max(0, TotalPageCount - 1));
    }

    /// <summary>Định dạng cho luồng <c>FormatPayrollPeriod</c>.</summary>
    private static string FormatPayrollPeriod(int payrollMonth, int payrollYear) => $"{payrollMonth:00}/{payrollYear}";

    /// <summary>Làm tròn tiền Việt Nam đồng đến đơn vị đồng theo quy tắc nửa lên.</summary>
    private static decimal RoundVnd(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero);

    /// <summary>Định dạng cho luồng <c>FormatVisibleAllowanceTotal</c>.</summary>
    private static string FormatVisibleAllowanceTotal(decimal value)
    {
        var rounded = RoundVnd(value);
        return rounded == 0m ? string.Empty : $"{rounded.ToString("#,##0", DisplayCulture)} đ";
    }

    /// <summary>Đặt lại cho luồng <c>ResetVisibleAllowanceTotal</c>.</summary>
    private void ResetVisibleAllowanceTotal()
    {
        VisibleAllowanceTotal = PagedRecords.Sum(record => record.HazardAllowanceAmount);
    }

    /// <summary>Thực hiện xử lý cho luồng <c>UpdateVisibleAllowanceTotalFromGrid</c>.</summary>
    private void UpdateVisibleAllowanceTotalFromGrid()
    {
        var summaryItem = Grid?.GetTotalSummaryItems()
            .FirstOrDefault(item => string.Equals(item.Name, HazardAllowanceAmountTotalSummaryName, StringComparison.Ordinal));
        var summaryValue = summaryItem is null ? null : Grid!.GetTotalSummaryValue(summaryItem);
        VisibleAllowanceTotal = summaryValue switch
        {
            decimal value => value,
            null => 0m,
            IConvertible value => Convert.ToDecimal(value, DisplayCulture),
            _ => 0m
        };
    }

    /// <summary>Lấy cho luồng <c>GetEmployeeDisplay</c>.</summary>
    private static string GetEmployeeDisplay(HazardAllowanceListItemDto record) =>
        string.Join(" - ", new[] { record.EmployeeCode, record.EmployeeName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    /// <summary>Thực hiện xử lý cho luồng <c>ValidateEditModel</c>.</summary>
    private static string? ValidateEditModel(PhuCapDocHaiEditModel model)
    {
        if(model.QualifiedWorkdayCount < 0m
           || model.LateEarlyDeductionDays < 0m
           || model.HazardAllowancePerDay < 0m
           || model.HazardAllowanceAmount < 0m)
        {
            return "Các giá trị số không được nhỏ hơn 0.";
        }

        if(model.LateEarlyDeductionDays > model.QualifiedWorkdayCount)
        {
            return "Công khấu trừ và công tính phụ cấp không được lớn hơn công hợp lệ.";
        }

        if(!model.IsEligibleDepartment && string.IsNullOrWhiteSpace(model.ExclusionReason))
        {
            return "Hãy nhập lý do loại trừ khi nhân viên không đủ điều kiện hưởng phụ cấp độc hại.";
        }

        if(!model.IsEligibleDepartment && model.HazardAllowanceAmount != 0m)
        {
            return "Phụ cấp độc hại phải bằng 0 khi nhân viên không đủ điều kiện hưởng.";
        }

        return model.ExclusionReason?.Trim().Length > 1000
            ? "Lý do loại trừ không được vượt quá 1.000 ký tự."
            : null;
    }

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp độc hại.</summary>
    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        if(normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            return (MinimumSupportedMonth, MinimumSupportedYear);
        }

        return (normalizedMonth, normalizedYear);
    }

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp độc hại.</summary>
    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ShowRecalculateSuccessToast</c>.</summary>
    private void ShowRecalculateSuccessToast(RefreshHazardAllowanceResult result)
    {
        var payrollPeriod = FormatPayrollPeriod(result.PayrollMonth, result.PayrollYear);
        ToastService.ShowSuccess(
            $"Đã tính lại phụ cấp độc hại kỳ {payrollPeriod}: thêm mới {result.CreatedCount:N0}, cập nhật {result.UpdatedCount:N0}, giữ nguyên do đã khóa {result.SkippedLockedCount:N0}, ngoại lệ không hưởng {result.IneligibleDepartmentCount:N0}, không có công hợp lệ {result.ZeroWorkdayCount:N0}.");
    }

    #endregion

    #region Summary Helpers

    /// <summary>Tạo cho luồng <c>BuildSummaryBadges</c>.</summary>
    private static IReadOnlyList<PhuCapDocHaiSummaryBadge> BuildSummaryBadges(HazardAllowanceSummaryDto summary) =>
    [
        new(SummaryAllKey, "Tất cả", summary.TotalCount),
        new(SummaryEligibleKey, "Hưởng PC", summary.EligibleCount),
        new(SummaryExceptionKey, "Ngoại lệ", summary.ExceptionCount),
        new(SummaryLockedKey, "Đã khóa", summary.LockedCount),
        new(SummaryOpenKey, "Đang mở", summary.OpenCount)
    ];

    /// <summary>Thực hiện xử lý cho luồng <c>MapSummaryBucket</c>.</summary>
    private static HazardAllowanceSummaryBucket MapSummaryBucket(string summaryBadgeKey) =>
        summaryBadgeKey switch
        {
            SummaryEligibleKey => HazardAllowanceSummaryBucket.Eligible,
            SummaryExceptionKey => HazardAllowanceSummaryBucket.Exception,
            SummaryLockedKey => HazardAllowanceSummaryBucket.Locked,
            SummaryOpenKey => HazardAllowanceSummaryBucket.Open,
            _ => HazardAllowanceSummaryBucket.All
        };

    #endregion
}
