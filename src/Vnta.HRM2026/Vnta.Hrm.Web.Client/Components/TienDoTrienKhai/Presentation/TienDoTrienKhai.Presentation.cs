using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Chuyển state local-only thành dữ liệu trình bày cho lộ trình dự án.</summary>
public partial class TienDoTrienKhai
{
    private IReadOnlyList<ProjectImplementationPhase> Phases => SessionState.Phases;

    private int PhaseCount => SessionState.PhaseCount;

    private int TotalDurationWeeks => SessionState.TotalDurationWeeks;

    private IReadOnlyList<int> TimelineWeeks => SessionState.TimelineWeeks;
}
