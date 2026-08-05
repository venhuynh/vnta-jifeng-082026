using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

/// <summary>Sở hữu lộ trình triển khai cố định trong phiên UI hiện tại.</summary>
internal sealed class ProjectImplementationProgressSessionState
{
    private static readonly IReadOnlyList<ProjectImplementationPhase> DefaultPhases =
    [
        new(
            Guid.Parse("f6abcc03-8545-4d19-b41b-1098590ca152"),
            1,
            "Lắp đặt các vị trí mới của máy chấm công, dữ liệu sinh trắc học, quy tắc ca kíp",
            4),
        new(
            Guid.Parse("996ff665-bf80-4c6a-badf-63adc3c4ab67"),
            2,
            "Tính công hàng ngày, chốt công tháng.",
            4),
        new(
            Guid.Parse("cc3e48fc-eca4-4c18-8f91-dbaf736f529d"),
            3,
            "Tính lương.",
            4),
        new(
            Guid.Parse("bdfd29f2-a8dc-4c83-a08d-e9024f2389f6"),
            4,
            "Ứng dụng mobile cho phép nhân viên truy cập",
            3),
        new(
            Guid.Parse("6b927efe-9598-4c6a-812f-b1f69377a1a2"),
            5,
            "Áp dụng các quy tắc hành chính.",
            2)
    ];

    internal IReadOnlyList<ProjectImplementationPhase> Phases { get; } = DefaultPhases;

    internal int PhaseCount => Phases.Count;

    internal int TotalDurationWeeks => Phases.Sum(phase => phase.DurationWeeks);

    internal IReadOnlyList<int> TimelineWeeks => Enumerable.Range(1, TotalDurationWeeks).ToArray();
}
