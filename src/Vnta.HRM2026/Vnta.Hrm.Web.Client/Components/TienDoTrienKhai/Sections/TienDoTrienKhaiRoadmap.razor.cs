using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;

/// <summary>Trình bày roadmap tuần tự và các thẻ giai đoạn của dự án.</summary>
public partial class TienDoTrienKhaiRoadmap
{
    [Parameter, EditorRequired] public IReadOnlyList<ProjectImplementationPhase> Phases { get; set; } = [];

    [Parameter, EditorRequired] public IReadOnlyList<int> TimelineWeeks { get; set; } = [];

    [Parameter, EditorRequired] public int PhaseCount { get; set; }

    [Parameter, EditorRequired] public int TotalDurationWeeks { get; set; }

    private int TimelineColumnCount => TotalDurationWeeks > 0 ? TotalDurationWeeks : 1;

    private string TimelineGridStyle => $"--implementation-timeline-columns: {TimelineColumnCount};";

    private static string GetTimelineSegmentCssClass(ProjectImplementationPhase phase) =>
        $"implementation-progress-timeline-segment implementation-progress-timeline-segment-{phase.Sequence}";

    private static string GetTimelineSegmentStyle(ProjectImplementationPhase phase) =>
        $"grid-column: span {phase.DurationWeeks};";
}
