using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.TinhLuong.LuongCanBan;

public partial class LuongCanBanInfoPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    private Guid? activeSalaryRecordId;
    private string? SearchText { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public BasicSalaryRecord? SalaryRecord { get; set; }

    [Parameter]
    public IReadOnlyList<BasicSalaryInfoRow> Rows { get; set; } = [];

    [Parameter]
    public DateTime? ResponseTime { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public string EmptyMessage { get; set; } =
        "Bản ghi lương căn bản này chưa có dữ liệu chi tiết để hiển thị.";

    [Parameter]
    public EventCallback RetryRequested { get; set; }

    private string DisplaySalaryRecordName => GetDisplayValue(SalaryRecord?.EmployeeDisplayText, "Lương căn bản");

    private string DisplayEmployeeCode => GetDisplayValue(SalaryRecord?.EmployeeCode, "--");

    private string DisplayPayrollPeriod => GetDisplayValue(SalaryRecord?.PeriodDisplayText, "--");

    protected override void OnParametersSet()
    {
        if (activeSalaryRecordId == SalaryRecord?.Id)
        {
            return;
        }

        activeSalaryRecordId = SalaryRecord?.Id;
        SearchText = null;
    }

    private Task OnVisibleChanged(bool visible)
    {
        return VisibleChanged.InvokeAsync(visible);
    }

    private Task CloseAsync()
    {
        return VisibleChanged.InvokeAsync(false);
    }

    private Task RetryAsync()
    {
        return RetryRequested.InvokeAsync();
    }

    private static string GetDisplayValue(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string FormatResponseTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        var normalized = value.Value.Kind == DateTimeKind.Unspecified
            ? value.Value
            : value.Value.ToLocalTime();

        return normalized.ToString("dd/MM/yyyy HH:mm:ss", DisplayCulture);
    }
}
