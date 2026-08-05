namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>Yêu cầu sao chép snapshot tổng hợp từ kỳ liền trước sang kỳ đích.</summary>
public sealed record SyncPayrollAllowanceSummaryFromPreviousMonthRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    string? Actor);
