using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Navigation;

namespace Vnta.Hrm.Web.Client.Components.Layout.Shared;

public partial class NavMenu : IDisposable
{
    private const string RootPath = "/";

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public EventCallback<string> CurrentModuleChanged { get; set; }

    private bool IsMenuLoading { get; set; } = true;

    private IReadOnlyList<VntaNavMenuNode> VisibleNodes { get; set; } = [];

    private string CurrentPath { get; set; } = RootPath;

    private int MenuTreeVersion { get; set; }

    private bool ShowLoadingState => IsMenuLoading;

    private bool ShowEmptyState => !IsMenuLoading && !VisibleNodes.Any(VntaNavMenuCatalog.IsVisibleInSidebar);

    private string RenderKey => $"menu-{MenuTreeVersion}-{CurrentPath}";

    protected override void OnInitialized()
    {
        CurrentPath = ResolveCurrentPath();
        NavigationManager.LocationChanged += OnLocationChanged;
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        await RefreshMenuAsync();
        await NotifyCurrentModuleChangedAsync();
        await base.OnInitializedAsync();
    }

    private Task HandleSelectionChanged(TreeViewNodeEventArgs _)
        => NotifyCurrentModuleChangedAsync();

    private async Task RefreshMenuAsync()
    {
        IsMenuLoading = true;
        CurrentPath = ResolveCurrentPath();

        VisibleNodes = await BuildVisibleNodesAsync();
        MenuTreeVersion++;

        IsMenuLoading = false;
    }

    private async Task<IReadOnlyList<VntaNavMenuNode>> BuildVisibleNodesAsync()
    {
        if (AuthenticationStateTask is null)
        {
            return [];
        }

        var authState = await AuthenticationStateTask;
        return VntaNavMenuAccessEvaluator.BuildVisibleNodes(authState.User);
    }

    private string ResolveCurrentPath(string? uri = null)
    {
        var relativePath = NavigationManager.ToBaseRelativePath(uri ?? NavigationManager.Uri);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return RootPath;
        }

        var route = relativePath.Split('#')[0];
        return route.StartsWith('/') ? route : $"/{route}";
    }

    private Task NotifyCurrentModuleChangedAsync()
        => CurrentModuleChanged.InvokeAsync(ResolveCurrentModule());

    private string ResolveCurrentModule()
        => VntaNavMenuAccessEvaluator.ResolveCurrentModule(VisibleNodes, CurrentPath);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _ = InvokeAsync(async () =>
        {
            CurrentPath = ResolveCurrentPath(e.Location);
            await NotifyCurrentModuleChangedAsync();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
