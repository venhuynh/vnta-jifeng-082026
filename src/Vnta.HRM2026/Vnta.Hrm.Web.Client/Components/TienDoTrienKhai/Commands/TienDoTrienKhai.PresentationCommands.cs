namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Điều phối các thao tác trình bày giữa host và các section.</summary>
public partial class TienDoTrienKhai
{
    private Task OnColumnChooserRequested()
    {
        GridSection?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private Task OnEmptyStateActionClick()
    {
        SessionState.ResetFilters();
        return Task.CompletedTask;
    }
}
