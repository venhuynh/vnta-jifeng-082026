namespace Vnta.Hrm.Application.TongQuan.ChamCongHangNgay;

public sealed record RebuildAttendanceDailySummaryRequest(
    DateOnly FromDate,
    DateOnly ToDate);
