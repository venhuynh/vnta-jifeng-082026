namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Một mốc công việc cùng trách nhiệm phối hợp của VNS và JIFENG.</summary>
public sealed record ProjectImplementationMilestone(
    string Code,
    string Title,
    int DurationWeeks,
    IReadOnlyList<string> VnsItems,
    IReadOnlyList<string> JifengItems,
    string? JifengLeadIn = null);
