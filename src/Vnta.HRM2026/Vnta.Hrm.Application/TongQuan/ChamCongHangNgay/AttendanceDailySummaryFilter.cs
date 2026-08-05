namespace Vnta.Hrm.Application.TongQuan.ChamCongHangNgay;

public sealed record AttendanceDailySummaryFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? SearchText,
    int Take = 1000);
