namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

/// <summary>
/// Kết quả chuẩn bị snapshot phí công đoàn từ roster Khấu trừ tổng hợp của một kỳ lương.
/// </summary>
public sealed record PreparePayrollUnionFeeDeductionPeriodResult(
    int PayrollYear,
    int PayrollMonth,
    int SummaryCount,
    int CreatedCount,
    int ExistingCount,
    int LockedSummaryCount);
