namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Một giai đoạn thuộc lộ trình triển khai dự án được thiết lập trực tiếp trên UI.</summary>
public sealed record ProjectImplementationPhase(
    Guid Id,
    int Sequence,
    string Title,
    int DurationWeeks,
    DateOnly? StartDate,
    IReadOnlyList<ProjectImplementationMilestone> Milestones,
    IReadOnlyList<string> AcceptanceCriteria)
{
    public string DurationText => $"Tổng thời gian: {DurationWeeks} tuần";

    public bool HasMilestones => Milestones.Count > 0;

    public int DetailedDurationWeeks => Milestones.Sum(milestone => milestone.DurationWeeks);

    public int RemainingDurationWeeks => Math.Max(0, DurationWeeks - DetailedDurationWeeks);

    public bool HasAcceptanceCriteria => AcceptanceCriteria.Count > 0;

    public IReadOnlyList<ProjectImplementationTask> DetailTasks => Milestones
        .SelectMany(milestone => milestone.Tasks)
        .OrderBy(task => task.StartDate)
        .ThenBy(task => task.MilestoneGroup, StringComparer.Ordinal)
        .ThenBy(task => task.Owner)
        .ThenBy(task => task.WorkItem, StringComparer.Ordinal)
        .ToArray();
}
