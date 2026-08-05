namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

/// <summary>Phân loại phụ cấp chuyên cần được lưu trong snapshot kỳ lương.</summary>
public enum AttendanceAllowanceClass
{
    Unresolved = 0,
    A = 1,
    B = 2,
    C = 3
}

/// <summary>Trạng thái nghỉ không phép (KP) đã được xác định từ bảng công.</summary>
public enum AttendanceAllowanceKpViolationState
{
    NotPresent = 0,
    Present = 1
}

/// <summary>Rule cuối cùng được áp dụng cho snapshot phụ cấp chuyên cần.</summary>
public enum AttendanceAllowanceAppliedRule
{
    Unresolved = 0,
    AttendanceClassA = 1,
    AttendanceClassB = 2,
    AttendanceClassC = 3,
    KpOverride = 4
}

/// <summary>Input thuần của phép tính phụ cấp chuyên cần.</summary>
public sealed record AttendanceAllowanceCalculationInput(
    decimal StandardWorkdayCount,
    decimal AttendanceWorkdayCount,
    decimal? MissingWorkdayCount,
    AttendanceAllowanceKpViolationState KpViolationState);

/// <summary>Kết quả thuần của phép tính phụ cấp chuyên cần.</summary>
public sealed record AttendanceAllowanceCalculationResult(
    decimal AttendanceRate,
    decimal ActualAllowanceAmount,
    AttendanceAllowanceClass AttendanceClass,
    decimal? MissingWorkdayCount,
    AttendanceAllowanceAppliedRule AppliedRule,
    AttendanceAllowanceKpViolationState KpViolationState);

/// <summary>
/// Chính sách tính mức và số tiền phụ cấp chuyên cần. Không đọc database, HTTP hay UI.
/// </summary>
public sealed class AttendanceAllowanceCalculationPolicy
{
    public const decimal AttendanceClassAMissingWorkdayThreshold = 1.0625m;
    public const decimal AttendanceClassBMissingWorkdayThreshold = 3.0625m;
    public const decimal AttendanceClassAAmount = 600_000m;
    public const decimal AttendanceClassBAmount = 300_000m;

    public AttendanceAllowanceCalculationResult Calculate(AttendanceAllowanceCalculationInput input)
    {
        var attendanceRate = CalculateAttendanceRate(input.AttendanceWorkdayCount, input.StandardWorkdayCount);
        var missingWorkdayCount = ResolveMissingWorkdayCount(input);
        var attendanceClass = ResolveAttendanceClass(missingWorkdayCount, input.KpViolationState);

        return new AttendanceAllowanceCalculationResult(
            attendanceRate,
            ResolveAllowanceAmount(attendanceClass),
            attendanceClass,
            missingWorkdayCount,
            ResolveAppliedRule(attendanceClass, input.KpViolationState),
            input.KpViolationState);
    }

    private static decimal CalculateAttendanceRate(decimal attendanceWorkdayCount, decimal standardWorkdayCount)
    {
        if(standardWorkdayCount <= 0m || attendanceWorkdayCount < 0m)
        {
            return 0m;
        }

        return Math.Round(
            Math.Clamp(attendanceWorkdayCount / standardWorkdayCount, 0m, 1m),
            4,
            MidpointRounding.AwayFromZero);
    }

    private static decimal? ResolveMissingWorkdayCount(AttendanceAllowanceCalculationInput input)
    {
        if(input.StandardWorkdayCount <= 0m)
        {
            return null;
        }

        // Characterized current server semantics: derived missing workdays cannot
        // become negative when attendance workdays exceed the salary standard.
        var rawMissingWorkdayCount = input.MissingWorkdayCount
            ?? input.StandardWorkdayCount - input.AttendanceWorkdayCount;
        return Math.Round(Math.Max(rawMissingWorkdayCount, 0m), 4, MidpointRounding.AwayFromZero);
    }

    private static AttendanceAllowanceClass ResolveAttendanceClass(
        decimal? missingWorkdayCount,
        AttendanceAllowanceKpViolationState kpViolationState)
    {
        if(kpViolationState == AttendanceAllowanceKpViolationState.Present)
        {
            return AttendanceAllowanceClass.C;
        }

        if(missingWorkdayCount is null)
        {
            return AttendanceAllowanceClass.Unresolved;
        }

        if(missingWorkdayCount <= AttendanceClassAMissingWorkdayThreshold)
        {
            return AttendanceAllowanceClass.A;
        }

        return missingWorkdayCount <= AttendanceClassBMissingWorkdayThreshold
            ? AttendanceAllowanceClass.B
            : AttendanceAllowanceClass.C;
    }

    private static decimal ResolveAllowanceAmount(AttendanceAllowanceClass attendanceClass) => attendanceClass switch
    {
        AttendanceAllowanceClass.A => AttendanceClassAAmount,
        AttendanceAllowanceClass.B => AttendanceClassBAmount,
        _ => 0m
    };

    private static AttendanceAllowanceAppliedRule ResolveAppliedRule(
        AttendanceAllowanceClass attendanceClass,
        AttendanceAllowanceKpViolationState kpViolationState)
    {
        if(kpViolationState == AttendanceAllowanceKpViolationState.Present)
        {
            return AttendanceAllowanceAppliedRule.KpOverride;
        }

        return attendanceClass switch
        {
            AttendanceAllowanceClass.A => AttendanceAllowanceAppliedRule.AttendanceClassA,
            AttendanceAllowanceClass.B => AttendanceAllowanceAppliedRule.AttendanceClassB,
            AttendanceAllowanceClass.C => AttendanceAllowanceAppliedRule.AttendanceClassC,
            _ => AttendanceAllowanceAppliedRule.Unresolved
        };
    }
}

public static class AttendanceAllowancePolicyStorageValues
{
    public static string? ToStorageValue(this AttendanceAllowanceClass attendanceClass) => attendanceClass switch
    {
        AttendanceAllowanceClass.A => "A",
        AttendanceAllowanceClass.B => "B",
        AttendanceAllowanceClass.C => "C",
        _ => null
    };

    public static string ToStorageValue(this AttendanceAllowanceAppliedRule appliedRule) => appliedRule switch
    {
        AttendanceAllowanceAppliedRule.AttendanceClassA => "attendance-cc-a",
        AttendanceAllowanceAppliedRule.AttendanceClassB => "attendance-cc-b",
        AttendanceAllowanceAppliedRule.AttendanceClassC => "attendance-cc-c",
        AttendanceAllowanceAppliedRule.KpOverride => "attendance-kp-cc-c",
        _ => "attendance-unresolved"
    };
}
