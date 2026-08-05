using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

namespace Vnta.Hrm.Web.Client.Components.DangKyPheDuyet.PheDuyetTangCa;

public partial class PheDuyetTangCaDetailsPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string Title { get; set; } = "Chi tiết phiếu tăng ca";
    [Parameter] public OvertimeRegistrationListItemDto? Request { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback RefreshRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    private Task RefreshAsync() => RefreshRequested.InvokeAsync();

    private static int GetRegisteredEmployeeCount(OvertimeRegistrationListItemDto request) =>
        request.EmployeeAssignments.Count(employee => employee.AssignmentType != OvertimeEmployeeAssignmentType.None);

    private static string GetDayTypeText(AttendanceWorkCalendarDayType dayType) =>
        AttendanceWorkCalendarDayTypes.GetDisplayName(dayType);

    private static string GetAssignmentDisplay(OvertimeEmployeeAssignmentType assignmentType) => assignmentType switch
    {
        OvertimeEmployeeAssignmentType.Until1900 => "Đến 19:00",
        OvertimeEmployeeAssignmentType.Until2100 => "Đến 21:00",
        OvertimeEmployeeAssignmentType.SpecialDayRegistered => "Có tham gia",
        _ => "Không đăng ký"
    };

    private static string FormatDate(DateOnly value) =>
        value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", DisplayCulture);

    private static string FormatDateTime(DateTime? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm", DisplayCulture)
        : "—";
}
