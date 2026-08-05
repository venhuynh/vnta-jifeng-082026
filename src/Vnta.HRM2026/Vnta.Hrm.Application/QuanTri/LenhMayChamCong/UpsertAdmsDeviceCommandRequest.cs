namespace Vnta.Hrm.Application.QuanTri.LenhMayChamCong;

public sealed record UpsertAdmsDeviceCommandRequest(
    string DeviceSn,
    string Content,
    DateTime? CommitTime,
    string? Description);
