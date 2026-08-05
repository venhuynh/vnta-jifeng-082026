using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.MayChamCong;

public partial class AttendanceDeviceInfoPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private Guid? activeDeviceId;
    private string? SearchText { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public AttendanceDeviceRecord? Device { get; set; }

    [Parameter]
    public IReadOnlyList<AttendanceDeviceInfoRow> Rows { get; set; } = [];

    [Parameter]
    public DateTime? ResponseTime { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public string EmptyMessage { get; set; } =
        "Hãy dùng menu Tạo lệnh, chọn Truy vấn thông tin và mở lại chi tiết sau khi thiết bị phản hồi.";

    [Parameter]
    public EventCallback RetryRequested { get; set; }

    private string DisplayDeviceName => GetDisplayValue(Device?.Name, "Máy chấm công");

    private string DisplaySerialNumber => GetDisplayValue(Device?.SerialNumber, "--");

    protected override void OnParametersSet()
    {
        if (activeDeviceId == Device?.Id)
        {
            return;
        }

        activeDeviceId = Device?.Id;
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
