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

    private static IReadOnlyList<ProjectImplementationMilestone> CreatePhaseTwoMilestones() =>
    [
        CreateMilestone(
            "2.1",
            "Tính được công, tăng ca từng ngày",
            new DateOnly(2026, 8, 31),
            [
                "Dựa vào các quy tắc tính công được mô tả ở mục 1.3 tính được công của nhân viên có chấm công"
            ],
            [
                "Đối soát hằng ngày để chốt công",
                "Chốt quy tắc tính tăng ca cho từng ca làm việc",
                "Chốt quy tắc xử lý bảng xếp ca"
            ]),
        CreateMilestone(
            "2.1",
            "Xử lý được các trường hợp chấm công bất thường",
            new DateOnly(2026, 9, 7),
            [
                "Áp dụng các CODE kết quả chấm công.",
                "Xử lý các trường hợp bất thường và lưu lịch sử xử lý"
            ],
            [
                "Xác nhận rõ quy tắc xử lý của từng CODE để tổng hợp công tháng",
                "Giải thích kỹ và cách tính công, đi trễ về sớm, khấu trừ ngày làm việc của từng nhân viên khi xử lý các log bất thường"
            ]),
        CreateMilestone(
            "2.2",
            "Theo dõi được các trường hợp nghỉ thai sản, được phép đi trễ về sớm, nghỉ phép",
            new DateOnly(2026, 9, 14),
            [
                "Có bảng quản lý các trường hợp đặc biệt đang nghỉ thai sản, nghỉ ốm, nghỉ phép",
                "Quản lý và chạy được quy tắc nghỉ phép từng nhân viên"
            ],
            [
                "Cung cấp quy tắc tính nghỉ phép",
                "Cung cấp quy tắc đăng ký nghỉ phép"
            ]),
        CreateMilestone(
            "2.3",
            "Chốt công tháng",
            new DateOnly(2026, 9, 21),
            [
                "Dựa vào quy tắc tính công hằng ngày để ra được bảng chốt công tháng sẵn sàng cho việc tính lương"
            ],
            [
                "Phối hợp với VNS để đối soát việc chốt công hằng ngày trong 2 tháng"
            ])
    ];

    private static IReadOnlyList<ProjectImplementationPhase> CreateDefaultPhases() =>
    [
        new(
            Guid.Parse("f6abcc03-8545-4d19-b41b-1098590ca152"),
            1,
            "Lắp đặt các vị trí mới của máy chấm công, dữ liệu sinh trắc học, quy tắc ca kíp",
            4,
            new DateOnly(2026, 8, 3),
            CreatePhaseOneMilestones(),
            [
                "Ứng dụng web app có thể giao tiếp được và tải được dữ liệu chấm công, thông tin nhân viên.",
                "Nhập thông tin từ file excel.",
                "Xuất dữ liệu chấm công ra file template cho khách hàng nhập vào phần mềm đang chạy."
            ]),
        new(
            Guid.Parse("996ff665-bf80-4c6a-badf-63adc3c4ab67"),
            2,
            "Tính công hàng ngày, chốt công tháng.",
            4,
            new DateOnly(2026, 8, 31),
            CreatePhaseTwoMilestones(),
            [
                "Ứng dụng WebApp tính công hằng ngày đúng theo quy tắc được mô tả.",
                "Chốt công hằng ngày và xuất được bảng công tổng hợp tháng theo quy tắc của khách hàng đã mô tả.",
                "Kết quả tính công tháng của 2 tháng trước phải đúng với kết quả của công ty."
            ]),
        new(
            Guid.Parse("cc3e48fc-eca4-4c18-8f91-dbaf736f529d"),
            3,
            "Tính lương.",
            4,
            null,
            [],
            []),
        new(
            Guid.Parse("bdfd29f2-a8dc-4c83-a08d-e9024f2389f6"),
            4,
            "Ứng dụng mobile cho phép nhân viên truy cập",
            3,
            null,
            [],
            []),
        new(
            Guid.Parse("6b927efe-9598-4c6a-812f-b1f69377a1a2"),
            5,
            "Áp dụng các quy tắc hành chính.",
            2,
            null,
            [],
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
