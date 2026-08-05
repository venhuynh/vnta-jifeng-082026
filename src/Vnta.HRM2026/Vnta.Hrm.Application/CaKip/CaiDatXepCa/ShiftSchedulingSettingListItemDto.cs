namespace Vnta.Hrm.Application.CaKip.CaiDatXepCa;

public sealed record ShiftSchedulingSettingListItemDto(
    Guid Id,
    Guid? ShiftId,
    string? ShiftCode,
    string? ShiftName,
    string? ShiftStartTime,
    string? ShiftEndTime,
    int ClassificationType,
    string Value,
    int AssignmentScopeMode,
    DateOnly? EffectiveFromDate,
    DateOnly? EffectiveToDate,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
