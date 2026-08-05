namespace Vnta.Hrm.Infrastructure.TinhLuong.BangCongTongHop;

/// <summary>
/// Quy tắc thuần để tính các giá trị công tháng từ dữ liệu chấm công đã tổng hợp.
/// </summary>
internal static class PayrollMonthlyWorkInputCalculator
{
    private const decimal MinutesPerWorkday = 480m;

    public static decimal CalculatePayrollWorkDays(
        decimal administrativeWorkDays,
        int lateEarlyLeaveMinutes)
    {
        var deductionDays = lateEarlyLeaveMinutes / MinutesPerWorkday;
        return Math.Round(
            administrativeWorkDays - deductionDays,
            decimals: 4,
            MidpointRounding.AwayFromZero);
    }
}
