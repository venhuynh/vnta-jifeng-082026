using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Components.DangKyPheDuyet.DangKyTangCa;

public partial class DangKyTangCaEditPopup
{
    private static readonly IReadOnlyList<EditDayTypeOption> EditDayTypeOptions =
    [
        new(AttendanceWorkCalendarDayType.Regular, AttendanceWorkCalendarDayTypes.GetDisplayName(AttendanceWorkCalendarDayType.Regular)),
        new(AttendanceWorkCalendarDayType.DayOff, AttendanceWorkCalendarDayTypes.GetDisplayName(AttendanceWorkCalendarDayType.DayOff)),
        new(AttendanceWorkCalendarDayType.Holiday, AttendanceWorkCalendarDayTypes.GetDisplayName(AttendanceWorkCalendarDayType.Holiday))
    ];

    private static readonly IReadOnlyList<AssignmentOption> RegularAssignmentOptions =
    [
        new(OvertimeEmployeeAssignmentType.None, "Không tăng ca"),
        new(OvertimeEmployeeAssignmentType.Until1900, "Tăng ca đến 19:00"),
        new(OvertimeEmployeeAssignmentType.Until2100, "Tăng ca đến 21:00")
    ];

    private Guid? activeRequestId;
    private bool wasVisible;
    private string? EmployeeSearchText { get; set; }
    private string SelectedTeamCode { get; set; } = EditTeamOption.AllValue;

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public OvertimeRequestEditModel? EditRequest { get; set; }

    [Parameter]
    public bool IsCreatingNewRequest { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string LoadingText { get; set; } = string.Empty;

    [Parameter]
    public string? ValidationMessage { get; set; }

    [Parameter]
    public EventCallback SaveDraftRequested { get; set; }

    [Parameter]
    public EventCallback SaveAndSubmitRequested { get; set; }

    private bool CanEditFields => !IsSaving;
    private bool CanClosePopup => !IsSaving;
    private bool CanSaveDraft => EditRequest is not null && !IsSaving;
    private bool CanSubmit => EditRequest is not null && !IsSaving;
    private bool IsSpecialDay => EditRequest is not null && AttendanceWorkCalendarDayTypes.IsSpecialDay(EditRequest.DayType);
    private string PopupTitle => IsCreatingNewRequest ? "Tạo phiếu đăng ký tăng ca" : "Điều chỉnh phiếu đăng ký tăng ca";
    private string RuleSummaryTitle => IsSpecialDay ? "Quy tắc ngày nghỉ/ngày lễ" : "Quy tắc ngày thường";
    private string RuleSummaryText => IsSpecialDay
        ? "Xưởng trưởng đăng ký trước ngày làm thêm tối thiểu 1 ngày; phiếu chỉ cần xác định danh sách có tham gia tăng ca, không yêu cầu chỉ định mốc giờ."
        : "Xưởng trưởng đăng ký trước 15:00 cùng ngày; mỗi nhân viên được chọn một trong hai mức tăng ca đến 19:00 hoặc 21:00, hoặc bị loại khỏi danh sách.";
    private IReadOnlyList<EditTeamOption> EditTeamOptions => BuildEditTeamOptions();
    private IReadOnlyList<OvertimeEmployeeAssignmentRecord> PopupVisibleEmployees => BuildPopupVisibleEmployees();
    private string PopupEmployeeEmptyTitle => !string.IsNullOrWhiteSpace(EmployeeSearchText)
        ? "Không còn nhân viên phù hợp với bộ lọc"
        : "Chưa có nhân viên trong phạm vi hiển thị";
    private string PopupEmployeeEmptyMessage => !string.IsNullOrWhiteSpace(EmployeeSearchText)
        ? "Hãy thử từ khóa khác hoặc đổi bộ lọc tổ."
        : "Danh sách này sẽ hiển thị nhân sự theo tổ của xưởng khi có dữ liệu.";

    protected override void OnParametersSet()
    {
        var isNewRequest = activeRequestId != EditRequest?.Id;
        if (Visible && (!wasVisible || isNewRequest))
        {
            activeRequestId = EditRequest?.Id;
            ResetLocalFilters();
        }

        wasVisible = Visible;
    }

    private Task OnVisibleChangedAsync(bool visible)
    {
        return VisibleChanged.InvokeAsync(visible);
    }

    private Task CloseAsync()
    {
        return VisibleChanged.InvokeAsync(false);
    }

    private Task SaveDraftAsync()
    {
        return SaveDraftRequested.InvokeAsync();
    }

    private Task SaveAndSubmitAsync()
    {
        return SaveAndSubmitRequested.InvokeAsync();
    }

    private Task OnEmployeeSearchTextChanged(string? value)
    {
        EmployeeSearchText = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return Task.CompletedTask;
    }

    private Task OnTeamChanged(string value)
    {
        SelectedTeamCode = string.IsNullOrWhiteSpace(value)
            ? EditTeamOption.AllValue
            : value;
        return Task.CompletedTask;
    }

    private Task OnEditDayTypeChangedAsync(AttendanceWorkCalendarDayType value)
    {
        if (EditRequest is null)
        {
            return Task.CompletedTask;
        }

        EditRequest.DayType = value;
        DangKyTangCa.NormalizeAssignmentsForDayType(EditRequest.EmployeeAssignments, value);
        return Task.CompletedTask;
    }

    private Task SetEmployeesUntil1900Async() => ApplyRegularDayAssignmentAsync(OvertimeEmployeeAssignmentType.Until1900);

    private Task SetEmployeesUntil2100Async() => ApplyRegularDayAssignmentAsync(OvertimeEmployeeAssignmentType.Until2100);

    private Task ClearEmployeesForRegularDayAsync() => ApplyRegularDayAssignmentAsync(OvertimeEmployeeAssignmentType.None);

    private Task ApplyRegularDayAssignmentAsync(OvertimeEmployeeAssignmentType assignmentType)
    {
        if (EditRequest is null)
        {
            return Task.CompletedTask;
        }

        foreach (var employee in GetPopupActionScopeEmployees())
        {
            employee.AssignmentType = assignmentType;
        }

        return Task.CompletedTask;
    }

    private Task SelectEmployeesForSpecialDayAsync() => ApplySpecialDaySelectionAsync(true);

    private Task ClearEmployeesForSpecialDayAsync() => ApplySpecialDaySelectionAsync(false);

    private Task ApplySpecialDaySelectionAsync(bool isRegistered)
    {
        if (EditRequest is null)
        {
            return Task.CompletedTask;
        }

        foreach (var employee in GetPopupActionScopeEmployees())
        {
            employee.AssignmentType = isRegistered
                ? OvertimeEmployeeAssignmentType.SpecialDayRegistered
                : OvertimeEmployeeAssignmentType.None;
        }

        return Task.CompletedTask;
    }

    private Task OnRegularDayAssignmentChanged(OvertimeEmployeeAssignmentRecord row, OvertimeEmployeeAssignmentType value)
    {
        row.AssignmentType = value;
        return Task.CompletedTask;
    }

    private Task OnSpecialDayRegistrationChanged(OvertimeEmployeeAssignmentRecord row, bool value)
    {
        row.AssignmentType = value
            ? OvertimeEmployeeAssignmentType.SpecialDayRegistered
            : OvertimeEmployeeAssignmentType.None;
        return Task.CompletedTask;
    }

    private IReadOnlyList<OvertimeEmployeeAssignmentRecord> GetPopupActionScopeEmployees()
    {
        if (EditRequest is null)
        {
            return [];
        }

        var query = EditRequest.EmployeeAssignments.AsEnumerable();
        if (!string.Equals(SelectedTeamCode, EditTeamOption.AllValue, StringComparison.Ordinal))
        {
            query = query.Where(employee => string.Equals(employee.TeamCode, SelectedTeamCode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(EmployeeSearchText))
        {
            var keyword = DangKyTangCa.NormalizeText(EmployeeSearchText);
            query = query.Where(employee => MatchesEmployeeSearch(employee, keyword));
        }

        return query.ToArray();
    }

    private IReadOnlyList<OvertimeEmployeeAssignmentRecord> BuildPopupVisibleEmployees()
    {
        if (EditRequest is null)
        {
            return [];
        }

        return GetPopupActionScopeEmployees()
            .OrderBy(employee => employee.TeamName)
            .ThenBy(employee => employee.EmployeeCode)
            .ToArray();
    }

    private IReadOnlyList<EditTeamOption> BuildEditTeamOptions()
    {
        if (EditRequest is null)
        {
            return [new(EditTeamOption.AllValue, "Tất cả tổ")];
        }

        return
        [
            new(EditTeamOption.AllValue, "Tất cả tổ"),
            .. EditRequest.EmployeeAssignments
                .GroupBy(employee => employee.TeamCode)
                .OrderBy(group => group.First().TeamName)
                .Select(group => new EditTeamOption(group.Key, group.First().TeamName))
        ];
    }

    private static bool MatchesEmployeeSearch(OvertimeEmployeeAssignmentRecord employee, string keyword)
    {
        var target = DangKyTangCa.NormalizeText(
            $"{employee.EmployeeCode} {employee.EmployeeName} {employee.TeamName} {employee.PositionName}");
        return target.Contains(keyword, StringComparison.Ordinal);
    }

    private void ResetLocalFilters()
    {
        EmployeeSearchText = null;
        SelectedTeamCode = EditTeamOption.AllValue;
    }

    private sealed record EditDayTypeOption(AttendanceWorkCalendarDayType Value, string Text);

    private sealed record AssignmentOption(OvertimeEmployeeAssignmentType Value, string Text);

    private sealed record EditTeamOption(string Value, string Text)
    {
        public const string AllValue = "__all__";
    }
}
