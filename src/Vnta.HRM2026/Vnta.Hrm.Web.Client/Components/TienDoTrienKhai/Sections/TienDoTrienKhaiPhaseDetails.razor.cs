using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;

/// <summary>Trình bày chi tiết các mốc công việc và trách nhiệm phối hợp của một giai đoạn.</summary>
public partial class TienDoTrienKhaiPhaseDetails
{
    [Parameter, EditorRequired] public ProjectImplementationPhase Phase { get; set; } = default!;

    private string DetailsTitleId => $"implementation-progress-phase-{Phase.Sequence}-details";

    private static string FormatDate(DateOnly value) =>
        value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
