using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

/// <summary>Protects allowance commands from locked and stale payroll data.</summary>
public static class OtherAllowanceEditPolicy
{
    public static void EnsureCanCreate(OtherAllowanceLockState summaryLockState)
    {
        if(summaryLockState == OtherAllowanceLockState.Locked)
            throw new InvalidOperationException("Bản ghi tổng hợp đã khóa, không thể thêm phụ cấp khác.");
    }

    public static void EnsureCanEdit(OtherAllowanceEditabilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(input.SummaryLockState == OtherAllowanceLockState.Locked
            || input.AllowanceLockState == OtherAllowanceLockState.Locked)
            throw new InvalidOperationException("Dòng phụ cấp đã khóa, không thể thay đổi.");

        EnsureExpectedVersion(input.ActualVersionUtc, input.ExpectedVersionUtc);
    }

    public static void EnsureCanChangeLockState(OtherAllowanceLockState summaryLockState, OtherAllowanceVersionInput version)
    {
        if(summaryLockState == OtherAllowanceLockState.Locked)
            throw new InvalidOperationException("Bản ghi tổng hợp đã khóa, không thể đổi trạng thái dòng phụ cấp.");

        ArgumentNullException.ThrowIfNull(version);
        EnsureExpectedVersion(version.ActualVersionUtc, version.ExpectedVersionUtc);
    }

    private static void EnsureExpectedVersion(DateTime actualVersionUtc, DateTime? expectedVersionUtc)
    {
        if(actualVersionUtc != expectedVersionUtc)
            throw new OtherAllowanceConflictException("Dòng phụ cấp khác đã thay đổi. Vui lòng tải lại dữ liệu.");
    }
}

public enum OtherAllowanceLockState
{
    Unlocked = 0,
    Locked = 1
}

public sealed record OtherAllowanceVersionInput(DateTime ActualVersionUtc, DateTime? ExpectedVersionUtc);

public sealed record OtherAllowanceEditabilityInput(
    OtherAllowanceLockState AllowanceLockState,
    OtherAllowanceLockState SummaryLockState,
    DateTime ActualVersionUtc,
    DateTime? ExpectedVersionUtc);
