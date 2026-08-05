using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public sealed partial class OtherAllowanceCoordinator
{
    public bool HasLoadError => !string.IsNullOrWhiteSpace(ErrorMessage);
    private bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
    private bool HasNoSearchResults => HasRequestedData && HasSearchText && ServerTotalRecordCount == 0;
    private string EmptyStateTitle => HasNoSearchResults ? "Không tìm thấy kết quả" : "Chưa có phụ cấp khác";
    private string EmptyStateMessage => HasNoSearchResults
        ? "Không tìm thấy dòng phụ cấp khác phù hợp với điều kiện tìm kiếm đang chọn."
        : "Không có dòng phụ cấp khác cho kỳ lương đang chọn.";
    private string EmptyStateActionText => HasNoSearchResults ? "Xóa điều kiện tìm kiếm" : "Tải lại";
    private bool HasPendingPeriodChange => HasRequestedData && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    private IReadOnlyList<int> AvailableMonthOptions => ToolbarYear == MinimumSupportedYear
        ? MonthOptions.Where(month => month >= MinimumSupportedMonth).ToArray()
        : MonthOptions;
    private int PageSize => pageSize;
    private int CurrentPageIndex => currentPageIndex;
    private int TotalPageCount => ServerTotalRecordCount <= 0 ? 1 : (int)Math.Ceiling(ServerTotalRecordCount / (double)PageSize);
    private int CurrentPageStartRecord => ServerTotalRecordCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => ServerTotalRecordCount == 0 ? 0 : Math.Min(ServerTotalRecordCount, CurrentPageIndex * PageSize + Rows.Count);
    private string PagerSummaryText => !HasRequestedData || HasLoadError || ServerTotalRecordCount == 0
        ? "Chưa có trang dữ liệu"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {ServerTotalRecordCount:N0} dòng";
    private bool CanBrowsePages => CanOperateOnCurrentDataset && ServerTotalRecordCount > 0;
    private bool CanChangeFilters => !IsLoading && !IsSyncingFromPreviousMonth && !IsChangingLockState && !IsSavingEdit && !IsMonthlyWorkPopupLoading;
    private bool CanView => CanChangeFilters;
    private bool CanInteract => CanChangeFilters;
    private bool CanOperateOnCurrentDataset => !IsLoading && !IsSyncingFromPreviousMonth && !IsChangingLockState && !HasLoadError && HasRequestedData && !HasPendingPeriodChange;
    private bool CanCreate => CanOperateOnCurrentDataset && !IsSavingEdit;
    public IReadOnlyList<object> SelectedItems => selectedItems;
    public bool CanOperate => CanOperateOnCurrentDataset;
    public bool CanSyncFromPreviousMonth
    {
        get
        {
            var previousPeriod = GetPreviousPeriod(AppliedMonth, AppliedYear);
            return CanOperateOnCurrentDataset && IsValidPayrollPeriod(previousPeriod.Month, previousPeriod.Year);
        }
    }
    public bool CanLockSelectedRows => CanOperateOnCurrentDataset;
    public bool CanUnlockSelectedRows => CanOperateOnCurrentDataset;
    private int SelectedRowCount => GetSelectedRows().Count;
    private bool CanChooseSelectedRowsScope => SelectedRowCount > 0;
    private bool CanConfirmLockAction => CanOperateOnCurrentDataset
        && (IsWholePeriodLockStateScope(SelectedLockActionScope) || CanChooseSelectedRowsScope);
    private string LockActionPopupTitle => PendingLockActionState ? "Khóa phụ cấp khác" : "Mở khóa phụ cấp khác";
    private string PendingLockActionPeriodLabel => $"{PendingLockActionMonth:00}/{PendingLockActionYear}";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu phụ cấp khác."
        : "Chọn phạm vi cần mở khóa dữ liệu phụ cấp khác.";
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Lựa chọn toàn kỳ sẽ bỏ qua bộ lọc tìm kiếm đang hiển thị. " +
        "Các dòng có tổng hợp payroll_allowance_summary_records đã khóa sẽ được bỏ qua.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRowCount:N0} dòng đang được chọn trong trang hiện tại; các dòng có summary đã khóa sẽ được bỏ qua."
        : "Chưa có dòng nào được chọn trong trang hiện tại.";
    private string WholePeriodScopeDescription =>
        $"Áp dụng cho toàn bộ phụ cấp khác của kỳ {PendingLockActionPeriodLabel}; các summary đã khóa sẽ được bỏ qua.";
    private bool CanEditFields => CanOperateOnCurrentDataset && !IsSavingEdit && !EditModel.IsLocked && !HasPendingPeriodChange;
    private bool CanSaveEdit => CanEditFields && (IsCreateMode ? EditModel.PayrollAllowanceSummaryRecordId != Guid.Empty : EditModel.Id != Guid.Empty);
    private string CurrentPeriodLabel => $"{ToolbarMonth:00}/{ToolbarYear}";

    private static string GetEmployeeDisplay(OtherAllowanceListItemDto row) =>
        string.IsNullOrWhiteSpace(row.EmployeeName)
            ? row.EmployeeCode?.Trim() ?? "--"
            : string.IsNullOrWhiteSpace(row.EmployeeCode) ? row.EmployeeName.Trim() : $"{row.EmployeeCode.Trim()} - {row.EmployeeName.Trim()}";

    private IReadOnlyList<OtherAllowanceListItemDto> GetSelectedRows()
    {
        var visibleRowIds = Rows.Select(row => row.Id).ToHashSet();
        return selectedItems
            .OfType<OtherAllowanceListItemDto>()
            .Where(row => row.Id != Guid.Empty && visibleRowIds.Contains(row.Id))
            .DistinctBy(row => row.Id)
            .ToArray();
    }

    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var vietnamNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(vietnamNow.Month, vietnamNow.Year);
    }

    private static (int Month, int Year) GetPreviousPeriod(int month, int year) =>
        month == 1 ? (12, year - 1) : (month - 1, year);

    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        return normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth
            ? (MinimumSupportedMonth, MinimumSupportedYear)
            : (normalizedMonth, normalizedYear);
    }

    private static bool IsValidPayrollPeriod(int payrollMonth, int payrollYear) => payrollYear is >= MinimumSupportedYear and <= MaximumSupportedYear
        && payrollMonth is >= 1 and <= 12
        && (payrollYear != MinimumSupportedYear || payrollMonth >= MinimumSupportedMonth);

    private static bool IsWholePeriodLockStateScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    private static PhuCapKhacEditModel CloneEditModel(PhuCapKhacEditModel source) => new()
    {
        Id = source.Id,
        PayrollAllowanceSummaryRecordId = source.PayrollAllowanceSummaryRecordId,
        EmployeeDisplay = source.EmployeeDisplay,
        PayrollMonth = source.PayrollMonth,
        PayrollYear = source.PayrollYear,
        PayrollPeriodDisplay = source.PayrollPeriodDisplay,
        AllowanceName = source.AllowanceName?.Trim() ?? string.Empty,
        IsFixedAmount = source.IsFixedAmount,
        AllowanceAmount = OtherAllowanceAmountPreview.Calculate(source.IsFixedAmount, source.AllowanceAmount),
        Note = source.Note,
        IsLocked = source.IsLocked,
        OriginalUpdatedAtUtc = source.OriginalUpdatedAtUtc
    };

    private static string? NormalizeSearchText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void ClampCurrentPageIndex() => currentPageIndex = Math.Clamp(currentPageIndex, 0, Math.Max(0, TotalPageCount - 1));
}
