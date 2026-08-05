using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Phases;

/// <summary>Contract dùng chung cho năm component giai đoạn của roadmap.</summary>
public abstract class TienDoTrienKhaiPhaseComponentBase : ComponentBase
{
    [Parameter, EditorRequired] public ProjectImplementationPhase Phase { get; set; } = default!;
}
