namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Hạng mục theo dõi tiến độ triển khai được quản lý trực tiếp trong phiên UI.</summary>
public sealed class ProjectImplementationProgressItem
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string WorkItem { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime DueDate { get; set; }

    public int ProgressPercent { get; set; }

    public ProjectImplementationProgressStatus Status { get; set; }

    public string Note { get; set; } = string.Empty;
}
