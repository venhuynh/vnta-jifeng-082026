namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Một giai đoạn thuộc lộ trình triển khai dự án được thiết lập trực tiếp trên UI.</summary>
public sealed record ProjectImplementationPhase(
    Guid Id,
    int Sequence,
    string Title,
    int DurationWeeks,
    DateOnly? StartDate,
    IReadOnlyList<ProjectImplementationMilestone> Milestones)
{
    public string DurationText => $"Tổng thời gian: {DurationWeeks} tuần";

    public bool HasMilestones => Milestones.Count > 0;

    public int DetailedDurationWeeks => Milestones.Sum(milestone => milestone.DurationWeeks);

    public int RemainingDurationWeeks => Math.Max(0, DurationWeeks - DetailedDurationWeeks);
}
