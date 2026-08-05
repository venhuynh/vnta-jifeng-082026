namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public sealed record AttendanceBiometricDataRefreshProgress(
    bool IsRunning,
    int TotalEmployees,
    int ProcessedEmployees,
    DateTime? StartedAtUtc,
    DateTime? UpdatedAtUtc,
    string? Stage)
{
    public double ProgressPercent => TotalEmployees <= 0
        ? 0
        : Math.Clamp((double)ProcessedEmployees / TotalEmployees * 100d, 0d, 100d);

    public static AttendanceBiometricDataRefreshProgress Idle { get; } =
        new(false, 0, 0, null, null, null);
}
