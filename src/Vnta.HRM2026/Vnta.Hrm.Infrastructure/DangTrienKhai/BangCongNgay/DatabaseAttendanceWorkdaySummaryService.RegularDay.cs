using Microsoft.EntityFrameworkCore.Storage;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService
{
    private const int RegularDayMinimumOvertimeMinutes = 30;
    private static readonly TimeOnly ProductionOvertimeBlock1900 = new(19, 0);
    private static readonly TimeOnly ProductionOvertimeBlock2100 = new(21, 0);

    private async Task<RebuildAttendanceWorkdaySummaryResult> RebuildRegularDayAsync(
        DateOnly workDate,
        CancellationToken cancellationToken)
    {
        var context = await BuildRebuildContextAsync(
            workDate,
            AttendanceWorkCalendarDayType.Regular,
            includeEligibleEmployees: true,
            cancellationToken);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var expectedKeys = new HashSet<(Guid EmployeeId, DateOnly WorkDate)>();

        foreach(var punchGroup in context.PunchGroups)
        {
            var key = (punchGroup.EmployeeId, workDate);
            expectedKeys.Add(key);

            var checkInAt = FormatPunchTime(punchGroup.FirstPunchAt);
            var checkOutAt = punchGroup.PunchCount > 1
                ? FormatPunchTime(punchGroup.LastPunchAt)
                : null;
            var evaluation = EvaluateRegularDayAttendance(context, punchGroup);

            UpsertSummaryRow(
                context,
                punchGroup.EmployeeId,
                checkInAt,
                checkOutAt,
                evaluation.StatusCodeId,
                evaluation.Note);

            if(context.RowsByKey.TryGetValue(key, out var row)
                && !row.IsLocked)
            {
                row.LateMinutes = evaluation.LateMinutes;
                row.EarlyLeaveMinutes = evaluation.EarlyLeaveMinutes;
                ApplyRegularDayOvertime(context, row, punchGroup);
            }
        }

        var unauthorizedAbsenceStatusCodeId = ResolveRequiredStatusCodeId(
            context.StatusCodeIds,
            UnauthorizedAbsenceStatusCode);

        foreach(var employeeId in context.EligibleEmployeeIds)
        {
            var key = (employeeId, workDate);
            expectedKeys.Add(key);

            if(context.RowsByKey.ContainsKey(key))
            {
                continue;
            }

            UpsertSummaryRow(
                context,
                employeeId,
                null,
                null,
                unauthorizedAbsenceStatusCodeId,
                UnauthorizedAbsenceNote);
        }

        DeleteUnlockedRowsOutsideKeys(context, expectedKeys);

        return await SaveAndBuildResultAsync(context, transaction, cancellationToken);
    }

    private RegularDayAttendanceEvaluation EvaluateRegularDayAttendance(
        WorkdaySummaryRebuildContext context,
        WorkdayPunchAggregate punchGroup)
    {
        if(punchGroup.PunchCount <= 1)
        {
            return new RegularDayAttendanceEvaluation(
                ResolveRequiredStatusCodeId(context.StatusCodeIds, MissingLogStatusCode),
                PartialPunchNote,
                0,
                0);
        }

        var shiftInfo = ResolveShiftInfo(
            context.EmployeeProfiles,
            context.ShiftsById,
            context.ShiftAssignmentsByKey,
            context.ShiftSchedulingSettings,
            punchGroup.EmployeeId,
            context.WorkDate);

        if(shiftInfo is null
            || !TryBuildWorkdayTimeRange(context.WorkDate, shiftInfo.ScheduledStartAt, shiftInfo.ScheduledEndAt, out var scheduledRange))
        {
            return new RegularDayAttendanceEvaluation(
                ResolveRequiredStatusCodeId(context.StatusCodeIds, AbnormalStatusCode),
                null,
                0,
                0);
        }

        if(!TryBuildActualPunchRange(punchGroup, out var actualRange))
        {
            return new RegularDayAttendanceEvaluation(
                ResolveRequiredStatusCodeId(context.StatusCodeIds, AbnormalStatusCode),
                null,
                0,
                0);
        }

        if(actualRange.CheckIn <= scheduledRange.Start && actualRange.CheckOut >= scheduledRange.End)
        {
            return new RegularDayAttendanceEvaluation(
                ResolveRequiredStatusCodeId(context.StatusCodeIds, FullWorkStatusCode),
                null,
                0,
                0);
        }

        var actualDurationMinutes = CalculatePositiveMinutes(actualRange.CheckIn, actualRange.CheckOut);
        if(actualDurationMinutes < AbnormalThresholdMinutes)
        {
            return new RegularDayAttendanceEvaluation(
                ResolveRequiredStatusCodeId(context.StatusCodeIds, AbnormalStatusCode),
                null,
                0,
                0);
        }

        var hasBreakRange = TryBuildWorkdayTimeRange(
            context.WorkDate,
            shiftInfo.BreakStartAt,
            shiftInfo.BreakEndAt,
            out var breakRange);

        var lateMinutes = CalculateLateMinutes(
            actualRange.CheckIn,
            scheduledRange,
            hasBreakRange ? breakRange : null);
        var earlyLeaveMinutes = CalculateEarlyLeaveMinutes(
            actualRange.CheckOut,
            scheduledRange,
            hasBreakRange ? breakRange : null);

        if(lateMinutes > 0 || earlyLeaveMinutes > 0)
        {
            return new RegularDayAttendanceEvaluation(
                ResolveRequiredStatusCodeId(context.StatusCodeIds, LateEarlyStatusCode),
                null,
                lateMinutes,
                earlyLeaveMinutes);
        }

        return new RegularDayAttendanceEvaluation(
            ResolveRequiredStatusCodeId(context.StatusCodeIds, AbnormalStatusCode),
            null,
            0,
            0);
    }

    private void ApplyRegularDayOvertime(
        WorkdaySummaryRebuildContext context,
        AttendanceWorkdaySummaryRow row,
        WorkdayPunchAggregate punchGroup)
    {
        if(!TryBuildActualPunchRange(punchGroup, out var actualRange)
            || !TryBuildWorkdayTimeRange(context.WorkDate, row.ScheduledStartAt, row.ScheduledEndAt, out var scheduledRange))
        {
            return;
        }

        var actualOvertimeMinutes = CalculatePositiveMinutes(scheduledRange.End, actualRange.CheckOut);
        if(actualOvertimeMinutes < RegularDayMinimumOvertimeMinutes)
        {
            return;
        }

        EnsureDailyOvertimeRegistrationCheckIsSatisfied();

        var overtimeMinutes15 = actualOvertimeMinutes;
        if(row.ShiftId.HasValue
            && context.ShiftsById.TryGetValue(row.ShiftId.Value, out var shift)
            && IsProductionShift(shift))
        {
            overtimeMinutes15 = CalculateProductionRegularDayOvertimeMinutes(
                scheduledRange.End,
                actualRange.CheckOut,
                actualOvertimeMinutes);
        }

        if(overtimeMinutes15 <= 0)
        {
            return;
        }

        row.OvertimeMinutes = overtimeMinutes15;
        row.OvertimeMinutes15 = overtimeMinutes15;
        row.IsRegisterForOT = true;
    }

    private static int CalculateLateMinutes(
        DateTime actualCheckIn,
        WorkdayTimeRange scheduledRange,
        WorkdayTimeRange? breakRange)
    {
        if(actualCheckIn <= scheduledRange.Start)
        {
            return 0;
        }

        var lateMinutes = (int)Math.Floor((actualCheckIn - scheduledRange.Start).TotalMinutes);
        return Math.Max(0, lateMinutes - CalculateOverlapMinutes(scheduledRange.Start, actualCheckIn, breakRange));
    }

    private static int CalculateEarlyLeaveMinutes(
        DateTime actualCheckOut,
        WorkdayTimeRange scheduledRange,
        WorkdayTimeRange? breakRange)
    {
        if(actualCheckOut >= scheduledRange.End)
        {
            return 0;
        }

        var earlyLeaveMinutes = (int)Math.Floor((scheduledRange.End - actualCheckOut).TotalMinutes);
        return Math.Max(0, earlyLeaveMinutes - CalculateOverlapMinutes(actualCheckOut, scheduledRange.End, breakRange));
    }

    private static int CalculateOverlapMinutes(
        DateTime rangeStart,
        DateTime rangeEnd,
        WorkdayTimeRange? breakRange)
    {
        if(breakRange is null || rangeEnd <= rangeStart)
        {
            return 0;
        }

        var effectiveBreakRange = breakRange.Value;
        var overlapStart = rangeStart > effectiveBreakRange.Start ? rangeStart : effectiveBreakRange.Start;
        var overlapEnd = rangeEnd < effectiveBreakRange.End ? rangeEnd : effectiveBreakRange.End;

        return overlapEnd <= overlapStart
            ? 0
            : (int)Math.Floor((overlapEnd - overlapStart).TotalMinutes);
    }

    private static int CalculateProductionRegularDayOvertimeMinutes(
        DateTime scheduledEnd,
        DateTime actualCheckOut,
        int actualOvertimeMinutes)
    {
        var block1900 = BuildSameDateTime(scheduledEnd, ProductionOvertimeBlock1900);
        var block2100 = BuildSameDateTime(scheduledEnd, ProductionOvertimeBlock2100);

        if(actualCheckOut >= block2100)
        {
            return Math.Min(actualOvertimeMinutes, CalculatePositiveMinutes(scheduledEnd, block2100));
        }

        if(actualCheckOut >= block1900)
        {
            return Math.Min(actualOvertimeMinutes, CalculatePositiveMinutes(scheduledEnd, block1900));
        }

        return actualOvertimeMinutes;
    }

    private static DateTime BuildSameDateTime(DateTime source, TimeOnly time) =>
        new(source.Year, source.Month, source.Day, time.Hour, time.Minute, time.Second);

    private static bool IsProductionShift(AttendanceShiftRow shift)
    {
        var normalizedCode = NormalizeShiftCode(shift.ShortName)
            ?? NormalizeShiftCode(shift.Code)
            ?? string.Empty;

        if(normalizedCode.StartsWith("SX", StringComparison.Ordinal))
        {
            return true;
        }

        var normalizedText = NormalizeShiftText(shift.ShortName, shift.Name);
        return normalizedText.Contains("san xuat", StringComparison.Ordinal)
            || normalizedText.Contains("production", StringComparison.Ordinal);
    }

    private static string? NormalizeShiftCode(string? value)
    {
        var normalized = NormalizeShiftText(value);
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static string NormalizeShiftText(params string?[] values)
    {
        var combined = string.Join(
            " ",
            values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!.Trim()));

        if(string.IsNullOrWhiteSpace(combined))
        {
            return string.Empty;
        }

        var normalized = combined.Normalize(System.Text.NormalizationForm.FormD);
        var buffer = new char[normalized.Length];
        var index = 0;

        foreach(var character in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);
            if(category == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            buffer[index++] = character switch
            {
                '\u0111' => 'd',
                '\u0110' => 'D',
                _ => character
            };
        }

        return new string(buffer, 0, index)
            .Normalize(System.Text.NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    private static bool TryBuildActualPunchRange(
        WorkdayPunchAggregate punchGroup,
        out ActualPunchRange range)
    {
        range = default;

        if(!punchGroup.FirstPunchAt.HasValue || !punchGroup.LastPunchAt.HasValue)
        {
            return false;
        }

        var checkIn = punchGroup.FirstPunchAt.Value;
        var checkOut = punchGroup.LastPunchAt.Value;
        if(checkOut < checkIn)
        {
            (checkIn, checkOut) = (checkOut, checkIn);
        }

        range = new ActualPunchRange(checkIn, checkOut);
        return true;
    }

    private static int CalculatePositiveMinutes(DateTime start, DateTime end) =>
        end <= start
            ? 0
            : (int)Math.Floor((end - start).TotalMinutes);

    private static bool TryBuildWorkdayTimeRange(
        DateOnly workDate,
        string? startTime,
        string? endTime,
        out WorkdayTimeRange range)
    {
        range = default;

        if(!TryParseTimeOnly(startTime, out var parsedStart)
            || !TryParseTimeOnly(endTime, out var parsedEnd))
        {
            return false;
        }

        var startDateTime = workDate.ToDateTime(parsedStart);
        var endDateTime = workDate.ToDateTime(parsedEnd);
        if(endDateTime <= startDateTime)
        {
            endDateTime = endDateTime.AddDays(1);
        }

        range = new WorkdayTimeRange(startDateTime, endDateTime);
        return true;
    }

    private static bool TryParseTimeOnly(string? value, out TimeOnly time)
    {
        time = default;
        var formats = new[] { "HH:mm:ss", "HH:mm" };
        return !string.IsNullOrWhiteSpace(value)
            && TimeOnly.TryParseExact(
                value.Trim(),
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out time);
    }

    private sealed record RegularDayAttendanceEvaluation(
        Guid StatusCodeId,
        string? Note,
        int LateMinutes,
        int EarlyLeaveMinutes);

    private readonly record struct ActualPunchRange(
        DateTime CheckIn,
        DateTime CheckOut);

    private readonly record struct WorkdayTimeRange(
        DateTime Start,
        DateTime End);
}
