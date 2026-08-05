namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Một giai đoạn thuộc lộ trình triển khai dự án được thiết lập trực tiếp trên UI.</summary>
public sealed record ProjectImplementationPhase(
    Guid Id,
    int Sequence,
    string Title,
    int DurationWeeks)
{
    public string DurationText => $"Tổng thời gian: {DurationWeeks} tuần";
}
