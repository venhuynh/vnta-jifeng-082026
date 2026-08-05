namespace Vnta.Hrm.Application.QuanTri.LenhMayChamCong;

public sealed record AdmsDeviceCommandSummaryDto(
    int Id,
    string DeviceSn,
    string Content,
    DateTime? CommitTime,
    DateTime? TransTime,
    DateTime? ResponseTime,
    string Status,
    string StatusText,
    string ReturnValue,
    string Description,
    bool IsTimedOut);
