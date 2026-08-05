namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Các trạng thái hợp lệ của một hạng mục triển khai.</summary>
public enum ProjectImplementationProgressStatus
{
    NotStarted,
    InProgress,
    WaitingAcceptance,
    Completed,
    Paused
}

/// <summary>Tông màu dùng chung cho trạng thái và huy hiệu tổng hợp.</summary>
public enum ProjectImplementationProgressTone
{
    Info,
    Progress,
    Warning,
    Success,
    Neutral,
    Danger
}

/// <summary>Metadata trình bày và lọc của một trạng thái tiến độ.</summary>
public sealed record ProjectImplementationProgressStatusDefinition(
    ProjectImplementationProgressStatus Value,
    string Key,
    string Label,
    string SummaryLabel,
    string SummaryShortLabel,
    ProjectImplementationProgressTone Tone,
    bool IsCompleted,
    bool IncludeInSummary);

/// <summary>Thông tin một huy hiệu tổng hợp trên màn hình tiến độ.</summary>
public sealed record ProjectImplementationProgressSummaryBadge(
    string Key,
    string Label,
    string ShortLabel,
    int Count,
    ProjectImplementationProgressTone Tone);

/// <summary>Lựa chọn kích thước trang của lưới tiến độ triển khai.</summary>
public sealed record ProjectImplementationProgressPageSizeOption(int Value, string Text);
