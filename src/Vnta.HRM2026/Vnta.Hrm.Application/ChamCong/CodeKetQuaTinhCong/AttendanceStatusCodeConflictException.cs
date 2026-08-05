namespace Vnta.Hrm.Application.ChamCong.CodeKetQuaTinhCong;

public sealed class AttendanceStatusCodeConflictException(string message)
    : InvalidOperationException(message);
