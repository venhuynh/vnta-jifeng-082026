using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHaiEditPopup</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHaiEditPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public PhuCapDocHaiEditModel Model { get; set; } = new();
    [Parameter] public EventCallback SaveRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    /// <summary>Lưu cho luồng <c>SaveAsync</c>.</summary>
    private Task SaveAsync() => SaveRequested.InvokeAsync();

    private Task OnQualifiedWorkdayCountChangedAsync(decimal value) => SetDecimalAsync(value, value =>
    {
        Model.QualifiedWorkdayCount = value;
        RecalculatePayableWorkdays();
    });

    private Task OnLateEarlyDeductionDaysChangedAsync(decimal value) => SetDecimalAsync(value, value =>
    {
        Model.LateEarlyDeductionDays = value;
        RecalculatePayableWorkdays();
    });
    private Task OnHazardAllowancePerDayChangedAsync(decimal value) => SetVndAsync(value, value => Model.HazardAllowancePerDay = value);
    private Task OnHazardAllowanceAmountChangedAsync(decimal value) => SetVndAsync(value, value => Model.HazardAllowanceAmount = value);

    private Task OnIsEligibleDepartmentChangedAsync(bool value)
    {
        Model.IsEligibleDepartment = value;
        if(value)
        {
            Model.ExclusionReason = null;
        }

        return Task.CompletedTask;
    }

    private Task OnExclusionReasonChangedAsync(string? value)
    {
        Model.ExclusionReason = value;
        return Task.CompletedTask;
    }

    private static Task SetDecimalAsync(decimal value, Action<decimal> setValue)
    {
        setValue(Math.Max(0m, value));
        return Task.CompletedTask;
    }

    private static Task SetVndAsync(decimal value, Action<decimal> setValue)
    {
        setValue(decimal.Round(Math.Max(0m, value), 0, MidpointRounding.AwayFromZero));
        return Task.CompletedTask;
    }

    private void RecalculatePayableWorkdays() =>
        Model.PayableWorkdayCount = decimal.Round(
            Math.Max(0m, Model.QualifiedWorkdayCount - Model.LateEarlyDeductionDays),
            4,
            MidpointRounding.AwayFromZero);
}
