namespace Vnta.Hrm.Application.DangTrienKhai.BangCongNgay;

public sealed record AttendanceWorkdaySummaryFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? SearchText,
    int Take = 1000);
