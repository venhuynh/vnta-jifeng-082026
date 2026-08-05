namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Một mốc công việc có lịch thực hiện và các dòng công việc chi tiết.</summary>
public sealed record ProjectImplementationMilestone(
    string Code,
    string Title,
    int DurationWeeks,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<ProjectImplementationTask> Tasks);
