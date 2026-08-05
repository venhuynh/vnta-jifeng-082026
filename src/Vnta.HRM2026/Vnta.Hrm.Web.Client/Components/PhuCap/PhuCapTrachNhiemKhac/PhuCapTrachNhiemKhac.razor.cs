using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>
/// Route host. It composes feature sections while the coordinator owns all workflow state.
/// </summary>
public partial class PhuCapTrachNhiemKhac : IDisposable
{
    [Inject]
    private OtherResponsibilityAllowanceCoordinator Coordinator { get; set; } = default!;

    protected override void OnInitialized() => Coordinator.Initialize(RequestRenderAsync);

    private Task RequestRenderAsync() => InvokeAsync(StateHasChanged);

    public void Dispose() => Coordinator.Dispose();
}
