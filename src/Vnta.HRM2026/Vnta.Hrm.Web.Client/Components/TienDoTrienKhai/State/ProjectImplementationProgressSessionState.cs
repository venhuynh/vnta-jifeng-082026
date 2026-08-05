using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

/// <summary>Sở hữu lộ trình triển khai cố định trong phiên UI hiện tại.</summary>
internal sealed class ProjectImplementationProgressSessionState
{
    private static IReadOnlyList<ProjectImplementationMilestone> CreatePhaseOneMilestones() =>
    [
        CreateMilestone(
            "1.1",
            "Lắp đặt MCC",
            new DateOnly(2026, 8, 3),
            [
                "Bàn giao 2 máy chấm công khuyến mãi V5L"
            ],
            [
                "Xác định vị trí lắp đặt phù hợp và không ngược sáng.",
                "Chịu trách nhiệm lắp đặt.",
                "Khuyến cáo cần thêm UPS lưu trữ cho trường hợp cúp điện thì MCC vẫn hoạt động được."
            ]),
        CreateMilestone(
            "1.2",
            "Triển khai máy chủ giao tiếp với các MCC",
            new DateOnly(2026, 8, 10),
            [
                "Cài đặt máy chủ ADMS để giao tiếp với các máy chấm công và tải được dữ liệu sinh trắc học."
            ],
            [
                "Tạo máy ảo Ubuntu và cung cấp các thông tin đăng nhập.",
                "Cài đặt các MCC trỏ về địa chỉ IP của máy chủ."
            ]),
        CreateMilestone(
            "1.3",
            "Quản lý thông tin nhân viên, dữ liệu sinh trắc học",
            new DateOnly(2026, 8, 17),
            [
                "Quản lý danh mục nhân viên.",
                "Quản lý danh mục phòng ban.",
                "Quản lý danh mục chức vụ.",
                "Quản lý danh mục dữ liệu sinh trắc học, đồng bộ được với các máy mới."
            ],
            [
                "Danh sách phòng ban.",
                "Danh sách chức vụ.",
                "Danh sách nhân viên.",
                "Danh sách các ca làm việc.",
                "Danh sách xếp ca theo tháng.",
                "Quy tắc xếp ca cho nhân viên mới.",
                "Quy tắc tính tăng ca."
            ],
            "Cung cấp file dữ liệu")
    ];

    private static IReadOnlyList<ProjectImplementationPhase> CreateDefaultPhases() =>
    [
        new(
            Guid.Parse("f6abcc03-8545-4d19-b41b-1098590ca152"),
            1,
            "Lắp đặt các vị trí mới của máy chấm công, dữ liệu sinh trắc học, quy tắc ca kíp",
            4,
            new DateOnly(2026, 8, 3),
            CreatePhaseOneMilestones()),
        new(
            Guid.Parse("996ff665-bf80-4c6a-badf-63adc3c4ab67"),
            2,
            "Tính công hàng ngày, chốt công tháng.",
            4,
            null,
            []),
        new(
            Guid.Parse("cc3e48fc-eca4-4c18-8f91-dbaf736f529d"),
            3,
            "Tính lương.",
            4,
            null,
            []),
        new(
            Guid.Parse("bdfd29f2-a8dc-4c83-a08d-e9024f2389f6"),
            4,
            "Ứng dụng mobile cho phép nhân viên truy cập",
            3,
            null,
            []),
        new(
            Guid.Parse("6b927efe-9598-4c6a-812f-b1f69377a1a2"),
            5,
            "Áp dụng các quy tắc hành chính.",
            2,
            null,
            [])
    ];

    internal IReadOnlyList<ProjectImplementationPhase> Phases { get; } = CreateDefaultPhases();

    internal int PhaseCount => Phases.Count;

    internal int TotalDurationWeeks => Phases.Sum(phase => phase.DurationWeeks);

    internal IReadOnlyList<int> TimelineWeeks => Enumerable.Range(1, TotalDurationWeeks).ToArray();

    private static ProjectImplementationMilestone CreateMilestone(
        string code,
        string title,
        DateOnly startDate,
        IReadOnlyList<string> vnsItems,
        IReadOnlyList<string> jifengItems,
        string? jifengLeadIn = null)
    {
        var endDate = startDate.AddDays(6);
        IReadOnlyList<ProjectImplementationTask> jifengLeadInTask = string.IsNullOrWhiteSpace(jifengLeadIn)
            ? []
            : CreateDetailTasks(
                code,
                title,
                startDate,
                endDate,
                ProjectImplementationTaskOwner.Jifeng,
                [jifengLeadIn]);

        return new ProjectImplementationMilestone(
            code,
            title,
            1,
            startDate,
            endDate,
            [
                ..CreateDetailTasks(code, title, startDate, endDate, ProjectImplementationTaskOwner.Vns, vnsItems),
                ..jifengLeadInTask,
                ..CreateDetailTasks(code, title, startDate, endDate, ProjectImplementationTaskOwner.Jifeng, jifengItems)
            ]);
    }

    private static IReadOnlyList<ProjectImplementationTask> CreateDetailTasks(
        string milestoneCode,
        string milestoneTitle,
        DateOnly startDate,
        DateOnly endDate,
        ProjectImplementationTaskOwner owner,
        IReadOnlyList<string> workItems) =>
        workItems
            .Select(workItem => new ProjectImplementationTask
            {
                Id = Guid.NewGuid(),
                MilestoneGroup = $"{milestoneCode} · {milestoneTitle}",
                WorkItem = workItem,
                Owner = owner,
                StartDate = startDate,
                EndDate = endDate,
                Status = ProjectImplementationTaskStatus.NotStarted,
                CompletionPercent = 0
            })
            .ToArray();
}
