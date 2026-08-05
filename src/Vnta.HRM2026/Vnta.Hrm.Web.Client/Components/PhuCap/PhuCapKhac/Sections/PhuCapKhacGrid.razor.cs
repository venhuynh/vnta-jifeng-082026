using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Sections;

public partial class PhuCapKhacGrid
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter, EditorRequired] public OtherAllowanceGridState State { get; set; } = default!;

    private IReadOnlyList<OtherAllowanceListItemDto> Rows => State.Rows;
    private bool IsLoading => State.IsLoading;
    private bool IsChangingLockState => State.IsChangingLockState;
    private bool CanOperate => !IsLoading && !IsChangingLockState;
    private string EmptyStateTitle => State.EmptyStateTitle;
    private string EmptyStateMessage => State.EmptyStateMessage;
    private string EmptyStateActionText => State.EmptyStateActionText;
    [Parameter] public EventCallback EmptyActionRequested { get; set; }
    [Parameter] public EventCallback<OtherAllowanceListItemDto> EditRequested { get; set; }
    [Parameter] public EventCallback<OtherAllowanceListItemDto> LockStateRequested { get; set; }
    [Parameter] public EventCallback<OtherAllowanceListItemDto> MonthlyWorkRequested { get; set; }
    [Parameter] public IReadOnlyList<object> SelectedItems { get; set; } = [];
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedItemsChanged { get; set; }
    [Parameter, EditorRequired] public Func<OtherAllowanceListItemDto, bool> CanEdit { get; set; } = _ => false;
    [Parameter, EditorRequired] public Func<OtherAllowanceListItemDto, bool> CanToggleLock { get; set; } = _ => false;
    [Parameter, EditorRequired] public Func<OtherAllowanceListItemDto, bool> CanViewMonthlyWork { get; set; } = _ => false;

    private static string GetAmountTypeCssClass(bool isFixedAmount) => isFixedAmount
        ? "other-allowance-amount-type other-allowance-amount-type-fixed hrm-grid-status"
        : "other-allowance-amount-type other-allowance-amount-type-variable hrm-grid-status";

    private static string GetLockStatusCssClass(bool isLocked) => isLocked
        ? "other-allowance-lock-status other-allowance-lock-status-locked hrm-grid-status"
        : "other-allowance-lock-status other-allowance-lock-status-open hrm-grid-status";

    private static string GetEmployeeDisplay(OtherAllowanceListItemDto row) =>
        string.IsNullOrWhiteSpace(row.EmployeeCode)
            ? row.EmployeeName ?? string.Empty
            : string.IsNullOrWhiteSpace(row.EmployeeName)
                ? row.EmployeeCode
                : $"{row.EmployeeCode} - {row.EmployeeName}";

    private static string FormatVnd(decimal amount) => string.Format(DisplayCulture, "{0:N0} đ", amount);
}
