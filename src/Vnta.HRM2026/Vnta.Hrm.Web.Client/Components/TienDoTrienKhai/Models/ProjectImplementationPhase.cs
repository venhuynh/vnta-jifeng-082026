using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public string DurationText => $"Tổng thời gian: {DurationWeeks} tuần";

    [JsonIgnore]
    public bool HasMilestones => Milestones.Count > 0;

    [JsonIgnore]
    public int DetailedDurationWeeks => Milestones.Sum(milestone => milestone.DurationWeeks);

    [JsonIgnore]
    public int RemainingDurationWeeks => Math.Max(0, DurationWeeks - DetailedDurationWeeks);

    [JsonIgnore]
    public bool HasAcceptanceCriteria => AcceptanceCriteria.Count > 0;

    [JsonIgnore]
    public IReadOnlyList<ProjectImplementationTask> DetailTasks => Milestones
        .SelectMany(milestone => milestone.Tasks)
        .OrderBy(task => task.StartDate)
        .ThenBy(task => task.MilestoneGroup, StringComparer.Ordinal)
        .ThenBy(task => task.Owner)
        .ThenBy(task => task.WorkItem, StringComparer.Ordinal)
        .ToArray();
}
