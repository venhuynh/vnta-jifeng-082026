namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed record OvertimeRegistrationActorContext(
    string Actor,
    Guid? EmployeeId,
    bool CanManageWorkshopRegistrations);
