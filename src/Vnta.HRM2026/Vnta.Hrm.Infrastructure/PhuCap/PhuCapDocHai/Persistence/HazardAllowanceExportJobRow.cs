namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

public sealed class HazardAllowanceExportJobRow
{
    public Guid Id { get; set; }

    public string FilterJson { get; set; } = string.Empty;

    public string RequestedBy { get; set; } = string.Empty;

    public HazardAllowanceExportJobStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? FileName { get; set; }

    public string? OutputPath { get; set; }

    public string? ErrorMessage { get; set; }
}
