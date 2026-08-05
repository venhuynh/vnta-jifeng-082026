using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.DuLieuSinhTracHoc;

public sealed class AttendanceBiometricDataRefreshProgressTracker
{
    private readonly Lock syncLock = new();
    private AttendanceBiometricDataRefreshProgress current = AttendanceBiometricDataRefreshProgress.Idle;

    public AttendanceBiometricDataRefreshProgress Snapshot()
    {
        lock (syncLock)
        {
            return current;
        }
    }

    public void Start(int totalEmployees, string? stage)
    {
        lock (syncLock)
        {
            var now = DateTime.UtcNow;
            current = new AttendanceBiometricDataRefreshProgress(
                true,
                Math.Max(totalEmployees, 0),
                0,
                now,
                now,
                stage);
        }
    }

    public void Update(int processedEmployees, int totalEmployees, string? stage)
    {
        lock (syncLock)
        {
            var now = DateTime.UtcNow;
            var normalizedTotal = Math.Max(totalEmployees, 0);
            var normalizedProcessed = Math.Clamp(processedEmployees, 0, normalizedTotal);

            current = current with
            {
                IsRunning = true,
                TotalEmployees = normalizedTotal,
                ProcessedEmployees = normalizedProcessed,
                StartedAtUtc = current.StartedAtUtc ?? now,
                UpdatedAtUtc = now,
                Stage = stage ?? current.Stage
            };
        }
    }

    public void Complete(int totalEmployees, string? stage)
    {
        lock (syncLock)
        {
            var now = DateTime.UtcNow;
            var normalizedTotal = Math.Max(totalEmployees, 0);

            current = new AttendanceBiometricDataRefreshProgress(
                false,
                normalizedTotal,
                normalizedTotal,
                current.StartedAtUtc ?? now,
                now,
                stage);
        }
    }

    public void Fail(int totalEmployees, int processedEmployees, string? stage)
    {
        lock (syncLock)
        {
            var now = DateTime.UtcNow;
            var normalizedTotal = Math.Max(totalEmployees, 0);
            var normalizedProcessed = Math.Clamp(processedEmployees, 0, normalizedTotal);

            current = new AttendanceBiometricDataRefreshProgress(
                false,
                normalizedTotal,
                normalizedProcessed,
                current.StartedAtUtc ?? now,
                now,
                stage);
        }
    }
}
