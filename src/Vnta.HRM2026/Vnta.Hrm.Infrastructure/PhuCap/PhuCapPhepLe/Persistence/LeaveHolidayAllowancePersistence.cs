using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;

/// <summary>Shared EF persistence primitives; command services retain their use-case decisions.</summary>
internal sealed class LeaveHolidayAllowancePersistence(ApplicationDbContext dbContext)
{
    public const int MinimumSupportedYear = 1900;
    public const int MaximumSupportedYear = 2100;
    public const int MaxNoteLength = 1000;
    public const string SystemActor = "system";

    public async Task EnsurePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken)
    {
        var summaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollYear == payrollYear && row.PayrollMonth == payrollMonth)
            .ToListAsync(cancellationToken);
        await EnsureDetailsAsync(summaries, GetDatabaseNow(), SystemActor, cancellationToken);
    }

    public async Task<Dictionary<Guid, PayrollAllowanceSummaryLeaveHolidayRecordRow>> EnsureDetailsAsync(
        IReadOnlyCollection<PayrollAllowanceSummaryRecordRow> summaries,
        DateTime now,
        string actor,
        CancellationToken cancellationToken)
    {
        if (summaries.Count == 0) return [];

        var summaryIds = summaries.Select(row => row.Id).ToArray();
        var details = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollAllowanceSummaryRecordId, cancellationToken);

        foreach (var summary in summaries.Where(summary => !details.ContainsKey(summary.Id)))
        {
            var detail = CreateDetail(summary, now, actor);
            dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
            details.Add(summary.Id, detail);
        }

        return details;
    }

    public async Task<PayrollAllowanceSummaryLeaveHolidayRecordRow> EnsureDetailAsync(
        PayrollAllowanceSummaryRecordRow summary,
        DateTime now,
        string actor,
        CancellationToken cancellationToken)
    {
        var detail = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.SingleOrDefaultAsync(
            row => row.PayrollAllowanceSummaryRecordId == summary.Id,
            cancellationToken);
        if (detail is not null) return detail;

        detail = CreateDetail(summary, now, actor);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
        return detail;
    }

    public static void ApplyValues(
        PayrollAllowanceSummaryRecordRow summary,
        PayrollAllowanceSummaryLeaveHolidayRecordRow detail,
        decimal dailyWageAmount,
        decimal leaveDayCount,
        decimal holidayDayCount,
        string? note,
        DateTime now,
        string actor)
    {
        var calculation = LeaveHolidayAllowanceCalculationPolicy.Calculate(
            new LeaveHolidayAllowanceCalculationInput(dailyWageAmount, leaveDayCount, holidayDayCount));
        detail.DailyWageAmount = calculation.DailyWageAmount;
        detail.LeaveDayCount = calculation.LeaveDayCount;
        detail.HolidayDayCount = calculation.HolidayDayCount;
        detail.LeaveHolidayAllowanceAmount = calculation.AllowanceAmount;
        detail.Note = NormalizeOptional(note);
        detail.UpdatedAtUtc = now;
        detail.UpdatedBy = actor;
        summary.LeaveHolidayAllowanceAmount = detail.LeaveHolidayAllowanceAmount;
        summary.UpdatedAtUtc = now;
        summary.UpdatedBy = actor;
    }

    public static bool HasManualInput(PayrollAllowanceSummaryLeaveHolidayRecordRow detail) =>
        detail.DailyWageAmount != 0m || detail.LeaveDayCount != 0m || detail.HolidayDayCount != 0m || !string.IsNullOrWhiteSpace(detail.Note);

    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static DateTime GetDatabaseNow() => ToDatabaseTimestamp(DateTime.UtcNow.AddHours(7));

    public static DateTime ToDatabaseTimestamp(DateTime value)
    {
        const long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1_000;
        return new DateTime(value.Ticks - value.Ticks % ticksPerMicrosecond, DateTimeKind.Unspecified);
    }

    public static void ValidatePeriod(int year, int month)
    {
        if (year < MinimumSupportedYear || year > MaximumSupportedYear)
            throw new InvalidOperationException($"Năm dữ liệu phải nằm trong khoảng {MinimumSupportedYear} đến {MaximumSupportedYear}.");
        if (month is < 1 or > 12)
            throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
    }

    private static PayrollAllowanceSummaryLeaveHolidayRecordRow CreateDetail(PayrollAllowanceSummaryRecordRow summary, DateTime now, string actor) => new()
    {
        PayrollAllowanceSummaryRecordId = summary.Id,
        DailyWageAmount = 0m,
        LeaveDayCount = 0m,
        HolidayDayCount = 0m,
        LeaveHolidayAllowanceAmount = LeaveHolidayAllowanceCalculationPolicy.RoundToStoragePrecision(summary.LeaveHolidayAllowanceAmount),
        CreatedAtUtc = now,
        CreatedBy = actor
    };
}
