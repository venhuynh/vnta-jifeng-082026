using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiem;

[Collection(ResponsibilityAllowancePostgreSqlCollection.Name)]
public sealed class ResponsibilityPositionAssignmentPostgreSqlIntegrationTests(
    ResponsibilityAllowancePostgreSqlFixture fixture)
{
    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Copy_from_previous_period_maps_grade_code_and_preserves_inactive_state()
    {
        fixture.RequireDatabase();
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var sourcePosition = CreatePosition("SRC");
        var skippedPosition = CreatePosition("SKIP");
        var sourceGrade = CreateGrade(2026, 6, "MANAGER", "Manager source", now);
        var skippedGrade = CreateGrade(2026, 6, "UNMAPPED", "Unmapped source", now);
        var targetGrade = CreateGrade(2026, 7, "MANAGER", "Manager target", now);
        var previousTargetGrade = CreateGrade(2026, 7, "OTHER", "Other target", now);
        var sourceMapping = CreateMapping(2026, 6, sourceGrade.Id, sourcePosition.Id, true, "source note", now);
        var skippedMapping = CreateMapping(2026, 6, skippedGrade.Id, skippedPosition.Id, false, "inactive source", now);
        var targetMapping = CreateMapping(2026, 7, previousTargetGrade.Id, sourcePosition.Id, false, "old target", now);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(
                sourcePosition,
                skippedPosition,
                sourceGrade,
                skippedGrade,
                targetGrade,
                previousTargetGrade,
                sourceMapping,
                skippedMapping,
                targetMapping);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var service = new FocusedResponsibilityPositionAssignmentService(commandContext);
            var result = await service.CopyFromPreviousPeriodAsync(
                new CopyResponsibilityPositionAssignmentsRequest(2026, 7));

            Assert.Equal(2, result.SourceCount);
            Assert.Equal(0, result.CreatedCount);
            Assert.Equal(1, result.UpdatedCount);
            Assert.Equal(1, result.SkippedMissingGradeCount);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var copied = await verificationContext.PayrollResponsibilityAllowanceGradePositions
            .SingleAsync(mapping => mapping.Id == targetMapping.Id);
        Assert.Equal(targetGrade.Id, copied.GradeId);
        Assert.True(copied.IsActive);
        Assert.Equal("source note", copied.Note);
        Assert.False(await verificationContext.PayrollResponsibilityAllowanceGradePositions.AnyAsync(mapping =>
            mapping.Year == 2026 && mapping.Month == 7 && mapping.PositionId == skippedPosition.Id));
    }

    private static AttendanceGatewayPositionRow CreatePosition(string suffix) =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = $"P-{suffix}-{Guid.NewGuid():N}"[..20],
            Name = $"Position {suffix}",
            Status = 1,
            CreatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

    private static PayrollResponsibilityAllowanceGradeRow CreateGrade(
        int year,
        int month,
        string code,
        string name,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = year,
            Month = month,
            Code = code,
            Name = name,
            StandardResponsibilityAllowanceAmount = 1_000_000m,
            DisplayOrder = 1,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

    private static PayrollResponsibilityAllowanceGradePositionRow CreateMapping(
        int year,
        int month,
        Guid gradeId,
        Guid positionId,
        bool isActive,
        string note,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = year,
            Month = month,
            GradeId = gradeId,
            PositionId = positionId,
            IsActive = isActive,
            Note = note,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

    private sealed class FocusedResponsibilityPositionAssignmentService(ApplicationDbContext dbContext)
        : ResponsibilityPositionAssignmentPersistenceOperations(dbContext);
}
