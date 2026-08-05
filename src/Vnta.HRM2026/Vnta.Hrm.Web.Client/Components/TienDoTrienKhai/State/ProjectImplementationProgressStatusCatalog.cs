using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

/// <summary>Nguồn metadata duy nhất cho trạng thái, filter và cách trình bày tiến độ.</summary>
internal static class ProjectImplementationProgressStatusCatalog
{
    internal const string AllKey = "all";
    internal const string OverdueKey = "overdue";

    internal static readonly ProjectImplementationProgressStatusDefinition NotStarted = new(
        ProjectImplementationProgressStatus.NotStarted,
        "not-started",
        "Chưa bắt đầu",
        "Hạng mục chưa bắt đầu",
        "Chưa bắt đầu",
        ProjectImplementationProgressTone.Info,
        IsCompleted: false,
        IncludeInSummary: true);

    internal static readonly ProjectImplementationProgressStatusDefinition InProgress = new(
        ProjectImplementationProgressStatus.InProgress,
        "in-progress",
        "Đang triển khai",
        "Hạng mục đang triển khai",
        "Đang làm",
        ProjectImplementationProgressTone.Progress,
        IsCompleted: false,
        IncludeInSummary: true);

    internal static readonly ProjectImplementationProgressStatusDefinition WaitingAcceptance = new(
        ProjectImplementationProgressStatus.WaitingAcceptance,
        "waiting-acceptance",
        "Chờ nghiệm thu",
        "Hạng mục chờ nghiệm thu",
        "Chờ nghiệm thu",
        ProjectImplementationProgressTone.Warning,
        IsCompleted: false,
        IncludeInSummary: true);

    internal static readonly ProjectImplementationProgressStatusDefinition Completed = new(
        ProjectImplementationProgressStatus.Completed,
        "completed",
        "Hoàn tất",
        "Hạng mục đã hoàn tất",
        "Hoàn tất",
        ProjectImplementationProgressTone.Success,
        IsCompleted: true,
        IncludeInSummary: true);

    internal static readonly ProjectImplementationProgressStatusDefinition Paused = new(
        ProjectImplementationProgressStatus.Paused,
        "paused",
        "Tạm dừng",
        "Hạng mục đang tạm dừng",
        "Tạm dừng",
        ProjectImplementationProgressTone.Neutral,
        IsCompleted: false,
        IncludeInSummary: false);

    internal static IReadOnlyList<ProjectImplementationProgressStatusDefinition> Definitions { get; } =
    [
        NotStarted,
        InProgress,
        WaitingAcceptance,
        Completed,
        Paused
    ];

    internal static ProjectImplementationProgressStatusDefinition Get(ProjectImplementationProgressStatus status) =>
        Definitions.FirstOrDefault(definition => definition.Value == status) ?? NotStarted;

    internal static bool TryGetByKey(string key, out ProjectImplementationProgressStatusDefinition definition)
    {
        definition = Definitions.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal)) ?? NotStarted;
        return Definitions.Any(item => string.Equals(item.Key, key, StringComparison.Ordinal));
    }

    internal static bool IsFilterKey(string key) =>
        string.Equals(key, AllKey, StringComparison.Ordinal)
        || string.Equals(key, OverdueKey, StringComparison.Ordinal)
        || Definitions.Any(definition => string.Equals(definition.Key, key, StringComparison.Ordinal));

    internal static string GetToneCssKey(ProjectImplementationProgressTone tone) => tone.ToString().ToLowerInvariant();
}
