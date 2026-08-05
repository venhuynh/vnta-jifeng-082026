namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem.Policies;

/// <summary>Trạng thái mã công không phép dùng trong chính sách phụ cấp trách nhiệm.</summary>
public enum ResponsibilityAllowanceUnexcusedAbsenceState
{
    NotPresent = 0,
    Present = 1
}

/// <summary>Kết quả xếp loại ABC của kỳ lương.</summary>
public enum ResponsibilityAllowanceAbcRating
{
    NotAvailable = 0,
    A = 1,
    B = 2,
    C = 3,
    D = 4
}

/// <summary>Trạng thái áp dụng thưởng hiệu suất (THS).</summary>
public enum ResponsibilityAllowancePerformanceBonusApplication
{
    Applied = 0,
    Excluded = 1
}

/// <summary>Nhánh công thức tiền thực tế đã áp dụng.</summary>
public enum ResponsibilityAllowanceCalculationBranch
{
    NoStandardAmount = 0,
    PerformanceBonusExcludedFullAmount = 1,
    PerformanceBonusExcludedProrated = 2,
    PerformanceBonusUnavailable = 3,
    RatingDProrated = 4,
    RatingMultiplier = 5
}

/// <summary>Trạng thái một dòng công có được tính là công hành chính hay không.</summary>
public enum ResponsibilityAllowanceWorkdayEligibility
{
    NotEligible = 0,
    Eligible = 1
}

/// <summary>Nguồn assignment mức phụ cấp.</summary>
public enum ResponsibilityAllowanceAssignmentSource
{
    EmployeeAssignment = 0,
    PositionDefault = 1
}

/// <summary>Nguồn mức phụ cấp được policy chọn.</summary>
public enum ResponsibilityAllowanceSelectedSource
{
    None = 0,
    EmployeeAssignment = 1,
    PositionDefault = 2
}

/// <summary>Trạng thái bản ghi cấu hình mức phụ cấp.</summary>
public enum ResponsibilityAllowanceConfigurationState
{
    Inactive = 0,
    Active = 1
}

/// <summary>Snapshot mức phụ cấp đã được adapter dữ liệu ánh xạ.</summary>
public sealed record ResponsibilityAllowanceGradeSnapshot(
    Guid Id,
    string Code,
    string Name,
    decimal StandardAmount,
    ResponsibilityAllowanceConfigurationState State);

/// <summary>Assignment mức phụ cấp của một nhân viên trong kỳ.</summary>
public sealed record ResponsibilityAllowanceAssignmentSnapshot(
    Guid? GradeId,
    ResponsibilityAllowanceAssignmentSource Source);

/// <summary>Mapping chức vụ → mức phụ cấp trong kỳ.</summary>
public sealed record ResponsibilityAllowancePositionMappingSnapshot(
    Guid GradeId,
    ResponsibilityAllowanceConfigurationState State);

/// <summary>Input chọn nguồn mức phụ cấp theo snapshot của kỳ lương.</summary>
public sealed record ResponsibilityAllowanceSourceSelectionInput(
    ResponsibilityAllowanceAssignmentSnapshot? Assignment,
    ResponsibilityAllowancePositionMappingSnapshot? PositionMapping,
    IReadOnlyDictionary<Guid, ResponsibilityAllowanceGradeSnapshot> Grades);

/// <summary>Kết quả chọn nguồn; assignment tồn tại nhưng không hợp lệ vẫn chặn fallback.</summary>
public sealed record ResponsibilityAllowanceSourceSelectionResult(
    ResponsibilityAllowanceSelectedSource Source,
    ResponsibilityAllowanceGradeSnapshot? Grade)
{
    public decimal StandardAmount => Grade?.StandardAmount ?? 0m;
}

/// <summary>Chính sách xác định đối tượng và mức phụ cấp áp dụng trong kỳ.</summary>
public sealed class ResponsibilityAllowanceSourceSelectionPolicy
{
    public ResponsibilityAllowanceSourceSelectionResult Select(ResponsibilityAllowanceSourceSelectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Grades);

        if (input.Assignment is not null)
        {
            var source = input.Assignment.Source == ResponsibilityAllowanceAssignmentSource.PositionDefault
                ? ResponsibilityAllowanceSelectedSource.PositionDefault
                : ResponsibilityAllowanceSelectedSource.EmployeeAssignment;
            if (input.Assignment.GradeId is Guid assignmentGradeId
                && input.Grades.TryGetValue(assignmentGradeId, out var assignmentGrade)
                && assignmentGrade.State == ResponsibilityAllowanceConfigurationState.Active)
            {
                return new(source, assignmentGrade);
            }

            return new(source, null);
        }

        if (input.PositionMapping is not null
            && input.PositionMapping.State == ResponsibilityAllowanceConfigurationState.Active
            && input.Grades.TryGetValue(input.PositionMapping.GradeId, out var positionGrade)
            && positionGrade.State == ResponsibilityAllowanceConfigurationState.Active)
        {
            return new(ResponsibilityAllowanceSelectedSource.PositionDefault, positionGrade);
        }

        return new(ResponsibilityAllowanceSelectedSource.None, null);
    }
}

/// <summary>Mã kết quả chấm công có ý nghĩa trong rule phụ cấp trách nhiệm.</summary>
public readonly record struct ResponsibilityAllowanceAttendanceCode
{
    public ResponsibilityAllowanceAttendanceCode(string? value) => Value = value?.Trim() ?? string.Empty;

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public bool IsUnexcusedAbsence =>
        string.Equals(Value, ResponsibilityAllowanceAttendanceResultCodes.UnexcusedAbsence, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Các mã kết quả được workflow dùng để ánh xạ dữ liệu ngoài vào policy.</summary>
public static class ResponsibilityAllowanceAttendanceResultCodes
{
    public const string UnexcusedAbsence = "KP";
}

/// <summary>Input một dòng công đã được adapter Infrastructure ánh xạ.</summary>
public sealed record ResponsibilityAllowanceWorkdayInput(
    ResponsibilityAllowanceWorkdayEligibility Eligibility,
    decimal LateMinutes,
    decimal EarlyLeaveMinutes,
    ResponsibilityAllowanceAttendanceCode AttendanceCode);

/// <summary>Input tổng hợp công ABC của một nhân viên trong kỳ.</summary>
public sealed record ResponsibilityAllowanceWorkdayMetricsInput(
    IReadOnlyCollection<ResponsibilityAllowanceWorkdayInput> Workdays);

/// <summary>Kết quả tổng hợp công dùng cho xếp loại ABC.</summary>
public sealed record ResponsibilityAllowanceWorkdayMetricsResult(
    decimal AdministrativeWorkdays,
    decimal LateEarlyDeductionDays,
    decimal AbcWorkdays,
    ResponsibilityAllowanceUnexcusedAbsenceState UnexcusedAbsenceState,
    IReadOnlyList<ResponsibilityAllowanceAttendanceCode> EligibleAttendanceCodes);

/// <summary>Input thuần của policy xếp loại ABC.</summary>
public sealed record ResponsibilityAllowanceAbcInput(
    decimal StandardWorkdays,
    decimal ActualWorkdays,
    ResponsibilityAllowanceUnexcusedAbsenceState UnexcusedAbsenceState);

/// <summary>Kết quả xếp loại ABC và số ngày thiếu công.</summary>
public sealed record ResponsibilityAllowanceAbcResult(
    ResponsibilityAllowanceAbcRating Rating,
    decimal MissingWorkdays);

/// <summary>Input thuần của calculator tiền phụ cấp trách nhiệm.</summary>
public sealed record ResponsibilityAllowanceAmountInput(
    decimal StandardAmount,
    decimal StandardWorkdays,
    decimal ActualWorkdays,
    ResponsibilityAllowanceAbcRating Rating,
    decimal MonthlyPerformanceBonusFactor,
    ResponsibilityAllowancePerformanceBonusApplication PerformanceBonusApplication);

/// <summary>Kết quả calculator tiền phụ cấp trách nhiệm.</summary>
public sealed record ResponsibilityAllowanceAmountResult(
    decimal ActualAmount,
    decimal AbcMultiplier,
    ResponsibilityAllowanceCalculationBranch CalculationBranch);

/// <summary>Chính sách tổng hợp công hành chính và công ABC.</summary>
public sealed class ResponsibilityAllowanceWorkdayMetricsCalculator
{
    public ResponsibilityAllowanceWorkdayMetricsResult Calculate(ResponsibilityAllowanceWorkdayMetricsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var workdays = input.Workdays ?? [];
        var administrativeWorkdays = workdays.Count(x => x.Eligibility == ResponsibilityAllowanceWorkdayEligibility.Eligible);
        var lateEarlyMinutes = workdays.Sum(x => x.LateMinutes + x.EarlyLeaveMinutes);
        var lateEarlyDeductionDays = Round(Math.Max(lateEarlyMinutes, 0m) / 480m, 4);
        var abcWorkdays = Round(Math.Max(administrativeWorkdays - lateEarlyDeductionDays, 0m), 4);
        var eligibleCodes = workdays
            .Where(x => x.Eligibility == ResponsibilityAllowanceWorkdayEligibility.Eligible && !x.AttendanceCode.IsEmpty)
            .Select(x => x.AttendanceCode)
            .DistinctBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ResponsibilityAllowanceWorkdayMetricsResult(
            administrativeWorkdays,
            lateEarlyDeductionDays,
            abcWorkdays,
            workdays.Any(x => x.AttendanceCode.IsUnexcusedAbsence)
                ? ResponsibilityAllowanceUnexcusedAbsenceState.Present
                : ResponsibilityAllowanceUnexcusedAbsenceState.NotPresent,
            eligibleCodes);
    }

    private static decimal Round(decimal value, int digits) => decimal.Round(value, digits, MidpointRounding.AwayFromZero);
}

/// <summary>Chính sách xếp loại ABC theo công chuẩn, công thực tế và mã KP.</summary>
public sealed class ResponsibilityAllowanceAbcPolicy
{
    public ResponsibilityAllowanceAbcResult Evaluate(ResponsibilityAllowanceAbcInput input)
    {
        var hasStandard = input.StandardWorkdays > 0m;
        var missingWorkdays = hasStandard
            ? Math.Max(input.StandardWorkdays - input.ActualWorkdays, 0m)
            : 0m;
        var rating = input.UnexcusedAbsenceState == ResponsibilityAllowanceUnexcusedAbsenceState.Present
            ? ResponsibilityAllowanceAbcRating.C
            : !hasStandard
                ? ResponsibilityAllowanceAbcRating.NotAvailable
                : missingWorkdays <= 1m
                    ? ResponsibilityAllowanceAbcRating.A
                    : missingWorkdays <= 3m
                        ? ResponsibilityAllowanceAbcRating.B
                        : missingWorkdays <= 7m
                            ? ResponsibilityAllowanceAbcRating.C
                            : ResponsibilityAllowanceAbcRating.D;

        return new ResponsibilityAllowanceAbcResult(rating, missingWorkdays);
    }

    public static decimal GetMultiplier(ResponsibilityAllowanceAbcRating rating) => rating switch
    {
        ResponsibilityAllowanceAbcRating.A => 1m,
        ResponsibilityAllowanceAbcRating.B => 0.9m,
        ResponsibilityAllowanceAbcRating.C => 0.8m,
        ResponsibilityAllowanceAbcRating.D => 0.7m,
        _ => 0m
    };
}

/// <summary>Calculator tiền thực tế; không phụ thuộc EF, HTTP hoặc UI.</summary>
public sealed class ResponsibilityAllowanceAmountCalculator
{
    public ResponsibilityAllowanceAmountResult Calculate(ResponsibilityAllowanceAmountInput input)
    {
        if (input.StandardAmount <= 0m)
        {
            return new(0m, 0m, ResponsibilityAllowanceCalculationBranch.NoStandardAmount);
        }

        if (input.PerformanceBonusApplication == ResponsibilityAllowancePerformanceBonusApplication.Excluded)
        {
            var missingWorkdays = Math.Max(input.StandardWorkdays - input.ActualWorkdays, 0m);
            if (missingWorkdays <= 1m)
            {
                return new(Round(input.StandardAmount), 1m, ResponsibilityAllowanceCalculationBranch.PerformanceBonusExcludedFullAmount);
            }

            if (input.StandardWorkdays <= 0m)
            {
                return new(0m, 0m, ResponsibilityAllowanceCalculationBranch.PerformanceBonusExcludedProrated);
            }

            return new(
                Round(input.StandardAmount / input.StandardWorkdays * input.ActualWorkdays),
                1m,
                ResponsibilityAllowanceCalculationBranch.PerformanceBonusExcludedProrated);
        }

        if (input.MonthlyPerformanceBonusFactor <= 0m)
        {
            return new(0m, 0m, ResponsibilityAllowanceCalculationBranch.PerformanceBonusUnavailable);
        }

        if (input.Rating == ResponsibilityAllowanceAbcRating.D)
        {
            if (input.StandardWorkdays <= 0m)
            {
                return new(0m, 0.7m, ResponsibilityAllowanceCalculationBranch.RatingDProrated);
            }

            return new(
                Round(0.7m * input.StandardAmount * input.MonthlyPerformanceBonusFactor / input.StandardWorkdays * input.ActualWorkdays),
                0.7m,
                ResponsibilityAllowanceCalculationBranch.RatingDProrated);
        }

        var multiplier = ResponsibilityAllowanceAbcPolicy.GetMultiplier(input.Rating);
        return new(
            Round(input.StandardAmount * multiplier * input.MonthlyPerformanceBonusFactor),
            multiplier,
            ResponsibilityAllowanceCalculationBranch.RatingMultiplier);
    }

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public static class ResponsibilityAllowancePolicyStorageValues
{
    public static string ToStorageValue(this ResponsibilityAllowanceSelectedSource source) => source switch
    {
        ResponsibilityAllowanceSelectedSource.EmployeeAssignment => "employee-assignment",
        ResponsibilityAllowanceSelectedSource.PositionDefault => "position-default",
        _ => "none"
    };

    public static string ToStorageValue(this ResponsibilityAllowanceAbcRating rating) => rating switch
    {
        ResponsibilityAllowanceAbcRating.A => "A",
        ResponsibilityAllowanceAbcRating.B => "B",
        ResponsibilityAllowanceAbcRating.C => "C",
        ResponsibilityAllowanceAbcRating.D => "D",
        _ => "NA"
    };

    public static ResponsibilityAllowanceAbcRating ToAbcRating(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "A" => ResponsibilityAllowanceAbcRating.A,
        "B" => ResponsibilityAllowanceAbcRating.B,
        "C" => ResponsibilityAllowanceAbcRating.C,
        "D" => ResponsibilityAllowanceAbcRating.D,
        _ => ResponsibilityAllowanceAbcRating.NotAvailable
    };
}
