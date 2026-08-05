namespace Vnta.Hrm.Application.CaKip.CaiDatCa;

public sealed record AttendanceShiftListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? ShortName,
    string? Description,
    string DepartmentGroup,
    string StartTime,
    string EndTime,
    bool IsOvernight,
    string? BreakStartTime,
    string? BreakEndTime,
    int Status,
    string? ColorHex,
    string? WorkingDays,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
