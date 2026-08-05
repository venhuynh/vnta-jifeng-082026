namespace Vnta.Hrm.Application.TongQuan.ChamCongHangNgay;

public sealed record RebuildAttendanceDailySummaryResult(
    DateOnly FromDate,
    DateOnly ToDate,
    int RebuiltSummaryCount,
    int TotalPunchCount);
