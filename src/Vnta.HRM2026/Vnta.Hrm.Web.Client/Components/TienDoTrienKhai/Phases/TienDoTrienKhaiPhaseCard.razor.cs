using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Phases;

/// <summary>Khung trình bày dùng chung cho một thẻ giai đoạn.</summary>
public partial class TienDoTrienKhaiPhaseCard
{
    [Parameter, EditorRequired] public ProjectImplementationPhase Phase { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string PhaseTitleId => $"implementation-progress-phase-{Phase.Sequence}-title";

    private static string FormatDate(DateOnly value) =>
        value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
