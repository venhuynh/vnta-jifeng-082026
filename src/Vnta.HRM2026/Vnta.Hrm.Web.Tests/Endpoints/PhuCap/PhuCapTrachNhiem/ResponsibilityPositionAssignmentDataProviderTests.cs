using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTrachNhiem;

public sealed class ResponsibilityPositionAssignmentDataProviderTests
{
    [Fact]
    public async Task Load_maps_grade_options_and_position_assignments_to_the_screen_model_without_losing_display_fields()
    {
        var gradeId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 7, 15, 8, 30, 0, DateTimeKind.Utc);
        var readService = new CapturingReadService(
            [new ResponsibilityPositionAssignmentGradeOptionDto(gradeId, 2026, 7, "TN01", "Trách nhiệm 01", 1_250_000m, 3, true, "grade note")],
            new ResponsibilityPositionAssignmentPageDto(
                [new ResponsibilityPositionAssignmentItemDto(Guid.NewGuid(), 2026, 7, gradeId, "TN01", "Trách nhiệm 01", positionId, "TP", "Trưởng phòng", false, "mapping note", createdAt, createdAt.AddMinutes(1))],
                14));
        var provider = new ResponsibilityPositionAssignmentDataProvider(readService, null!, null!, null!, null!, null!);

        var result = await provider.LoadAsync(2026, 7, "trưởng", 20, 10);

        Assert.Equal(new ResponsibilityPositionAssignmentQuery(2026, 7, "trưởng", 20, 10), readService.Query);
        var grade = Assert.Single(result.Grades);
        Assert.Equal((gradeId, "TN01", "Trách nhiệm 01", 1_250_000m, 3, true, "grade note"),
            (grade.Id, grade.Code, grade.Name, grade.StandardResponsibilityAllowanceAmount, grade.DisplayOrder, grade.IsActive, grade.Note));
        var mapping = Assert.Single(result.Mappings);
        Assert.Equal((positionId, "TP", "Trưởng phòng", false, "mapping note", createdAt.AddMinutes(1)),
            (mapping.PositionId, mapping.PositionCode, mapping.PositionName, mapping.IsActive, mapping.Note, mapping.UpdatedAtUtc));
        Assert.Equal(14, result.TotalCount);
    }

    private sealed class CapturingReadService(
        IReadOnlyList<ResponsibilityPositionAssignmentGradeOptionDto> grades,
        ResponsibilityPositionAssignmentPageDto page) : IResponsibilityPositionAssignmentReadService
    {
        public ResponsibilityPositionAssignmentQuery? Query { get; private set; }

        public Task<IReadOnlyList<ResponsibilityPositionAssignmentGradeOptionDto>> GetGradeOptionsAsync(int year, int month, CancellationToken cancellationToken = default) =>
            Task.FromResult(grades);

        public Task<ResponsibilityPositionAssignmentPageDto> SearchPageAsync(ResponsibilityPositionAssignmentQuery query, CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult(page);
        }
    }
}
