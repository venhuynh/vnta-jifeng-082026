namespace Vnta.Hrm.Web.Client.Components.CaKip.BangXepCa;

public sealed class ShiftEditState
{
    public Guid EmployeeId { get; init; }

    public string EmployeeDisplay { get; init; } = "--";

    public DateOnly WorkDate { get; init; }

    public string WorkDateText { get; init; } = string.Empty;

    public string CurrentShiftText { get; init; } = "--";
}

public sealed record ShiftEditOption(
    Guid Id,
    string DisplayText);
