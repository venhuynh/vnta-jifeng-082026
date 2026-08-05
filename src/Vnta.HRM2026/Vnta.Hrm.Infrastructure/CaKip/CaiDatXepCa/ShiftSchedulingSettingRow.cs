namespace Vnta.Hrm.Infrastructure.CaKip.CaiDatXepCa;

public sealed class ShiftSchedulingSettingRow
{
    public Guid Id { get; set; }

    public Guid? ShiftId { get; set; }

    public int ClassificationType { get; set; }

    public string? Value { get; set; }

    public int AssignmentScopeMode { get; set; }

    public DateOnly? EffectiveFromDate { get; set; }

    public DateOnly? EffectiveToDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
