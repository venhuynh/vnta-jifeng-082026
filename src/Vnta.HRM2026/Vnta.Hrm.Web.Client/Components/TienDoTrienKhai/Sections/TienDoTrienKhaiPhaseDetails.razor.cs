using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Persistence;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;

/// <summary>Trình bày và cho phép cập nhật cục bộ các đầu việc chi tiết của một giai đoạn.</summary>
public partial class TienDoTrienKhaiPhaseDetails
{
    [Inject] private IProjectImplementationProgressStore ProgressStore { get; set; } = default!;

    private sealed record TaskOwnerOption(ProjectImplementationTaskOwner Value, string Text);

    private sealed record TaskStatusOption(ProjectImplementationTaskStatus Value, string Text);

    private static readonly IReadOnlyList<TaskOwnerOption> OwnerOptions =
    [
        new(ProjectImplementationTaskOwner.Vns, ProjectImplementationTaskCatalog.GetOwnerLabel(ProjectImplementationTaskOwner.Vns)),
        new(ProjectImplementationTaskOwner.Jifeng, ProjectImplementationTaskCatalog.GetOwnerLabel(ProjectImplementationTaskOwner.Jifeng))
    ];

    private static readonly IReadOnlyList<TaskStatusOption> StatusOptions =
    [
        new(ProjectImplementationTaskStatus.NotStarted, ProjectImplementationTaskCatalog.GetStatusLabel(ProjectImplementationTaskStatus.NotStarted)),
        new(ProjectImplementationTaskStatus.InProgress, ProjectImplementationTaskCatalog.GetStatusLabel(ProjectImplementationTaskStatus.InProgress)),
        new(ProjectImplementationTaskStatus.Completed, ProjectImplementationTaskCatalog.GetStatusLabel(ProjectImplementationTaskStatus.Completed)),
        new(ProjectImplementationTaskStatus.Paused, ProjectImplementationTaskCatalog.GetStatusLabel(ProjectImplementationTaskStatus.Paused))
    ];

    [Parameter, EditorRequired] public ProjectImplementationPhase Phase { get; set; } = default!;

    private IReadOnlyList<ProjectImplementationTask> Tasks => Phase.DetailTasks;

    private int TaskCount => Tasks.Count;

    private string DetailsTitleId => $"implementation-progress-phase-{Phase.Sequence}-details";

    private string AcceptanceTitleId => $"implementation-progress-phase-{Phase.Sequence}-acceptance";

    private DateTimeOffset? LastSavedAt { get; set; }

    private string? SaveError { get; set; }

    private async Task OnEditModelSaving(GridEditModelSavingEventArgs e)
    {
        if(e.IsNew)
        {
            e.Cancel = true;
            return;
        }

        var editedTask = (ProjectImplementationTask)e.EditModel;
        editedTask.WorkItem = editedTask.WorkItem?.Trim() ?? string.Empty;

        try
        {
            await ProgressStore.UpdateTaskAsync(new UpdateProjectImplementationTaskRequest(
                editedTask.Id,
                editedTask.WorkItem,
                editedTask.Owner,
                editedTask.StartDate,
                editedTask.EndDate,
                editedTask.Status,
                editedTask.CompletionPercent));

            e.CopyChangesToDataItem();
            e.Reload = false;
            LastSavedAt = DateTimeOffset.Now;
            SaveError = null;
        }
        catch
        {
            e.Cancel = true;
            SaveError = "Không thể lưu thay đổi. Vui lòng thử lại.";
        }
    }

    private static string GetOwnerLabel(ProjectImplementationTaskOwner owner) =>
        ProjectImplementationTaskCatalog.GetOwnerLabel(owner);

    private static string GetOwnerCssClass(ProjectImplementationTaskOwner owner) =>
        owner == ProjectImplementationTaskOwner.Vns
            ? "implementation-progress-detail-owner implementation-progress-detail-owner-vns"
            : "implementation-progress-detail-owner implementation-progress-detail-owner-jifeng";

    private static string GetStatusLabel(ProjectImplementationTaskStatus status) =>
        ProjectImplementationTaskCatalog.GetStatusLabel(status);

    private static string GetStatusCssClass(ProjectImplementationTaskStatus status) =>
        $"implementation-progress-detail-status implementation-progress-detail-status-{ProjectImplementationTaskCatalog.GetStatusCssKey(status)}";

    private static string FormatDate(DateOnly value) =>
        value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string FormatSavedAt(DateTimeOffset value) =>
        value.ToLocalTime().ToString("HH:mm:ss dd/MM", CultureInfo.InvariantCulture);
}
