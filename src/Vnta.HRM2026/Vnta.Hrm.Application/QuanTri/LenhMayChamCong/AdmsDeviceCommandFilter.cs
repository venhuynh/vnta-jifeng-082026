namespace Vnta.Hrm.Application.QuanTri.LenhMayChamCong;

public sealed record AdmsDeviceCommandFilter(
    string? DeviceSn,
    string? Status,
    string? SearchTerm,
    DateTime? CommitFrom,
    DateTime? CommitTo);
