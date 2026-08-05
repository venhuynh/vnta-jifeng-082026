using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Đơn vị phụ trách một dòng công việc chi tiết của dự án.</summary>
public enum ProjectImplementationTaskOwner
{
    [Display(Name = "VNS")]
    Vns,

    [Display(Name = "JIFENG")]
    Jifeng
}

/// <summary>Trạng thái thực hiện của một dòng công việc chi tiết.</summary>
public enum ProjectImplementationTaskStatus
{
    [Display(Name = "Chưa bắt đầu")]
    NotStarted,

    [Display(Name = "Đang thực hiện")]
    InProgress,

    [Display(Name = "Hoàn thành")]
    Completed,

    [Display(Name = "Tạm dừng")]
    Paused
}

/// <summary>Dòng công việc local-only được hiển thị trong lưới chi tiết thực hiện.</summary>
public sealed class ProjectImplementationTask
{
    public Guid Id { get; init; }

    public string MilestoneGroup { get; init; } = string.Empty;

    public string WorkItem { get; set; } = string.Empty;

    public ProjectImplementationTaskOwner Owner { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public ProjectImplementationTaskStatus Status { get; set; }

    [Range(0, 100)]
    public int CompletionPercent { get; set; }
}

/// <summary>Nguồn trình bày thống nhất cho đơn vị phụ trách và trạng thái công việc.</summary>
internal static class ProjectImplementationTaskCatalog
{
    internal static string GetOwnerLabel(ProjectImplementationTaskOwner owner) => owner switch
    {
        ProjectImplementationTaskOwner.Vns => "VNS",
        ProjectImplementationTaskOwner.Jifeng => "JIFENG",
        _ => string.Empty
    };

    internal static string GetStatusLabel(ProjectImplementationTaskStatus status) => status switch
    {
        ProjectImplementationTaskStatus.NotStarted => "Chưa bắt đầu",
        ProjectImplementationTaskStatus.InProgress => "Đang thực hiện",
        ProjectImplementationTaskStatus.Completed => "Hoàn thành",
        ProjectImplementationTaskStatus.Paused => "Tạm dừng",
        _ => string.Empty
    };

    internal static string GetStatusCssKey(ProjectImplementationTaskStatus status) =>
        status.ToString().ToLowerInvariant();
}
