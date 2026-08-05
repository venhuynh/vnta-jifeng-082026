namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

public sealed record AttendanceAllowanceResultFilter(
    PayrollAllowanceKind AllowanceKind,
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    // Keep Take in its original position so legacy consumers that construct this
    // record positionally continue to request the same result limit. A missing
    // value is normalized by each compatible read path (legacy list = 1000,
    // paged screen = 50).
    int Take = 0,
    int Skip = 0,
    AttendanceAllowanceLockState LockState = AttendanceAllowanceLockState.All,
    string? AttendanceClass = null);
