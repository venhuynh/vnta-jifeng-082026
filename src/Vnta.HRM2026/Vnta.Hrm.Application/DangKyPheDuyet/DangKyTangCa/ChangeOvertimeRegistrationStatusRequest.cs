namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed class ChangeOvertimeRegistrationStatusRequest
{
    public IReadOnlyCollection<Guid> Ids { get; set; } = [];

    public OvertimeRegistrationStatus TargetStatus { get; set; }
}
