using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiem;

[Collection(ResponsibilityAllowancePostgreSqlCollection.Name)]
public sealed class ResponsibilityAllowancePostgreSqlIntegrationTests(
    ResponsibilityAllowancePostgreSqlFixture fixture)
{
    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Recalculate_keeps_locked_abc_and_locked_summary_unchanged()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: true, responsibilityAmount: 9_900_000m);
        var abc = CreateAbc(seed, summary.Id, 2026, 7, isLocked: true, actualAmount: 4_200_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, abc);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var service = CreateWorkflowService(commandContext);

            var result = await service.RecalculateAbcAsync(
                new RefreshPayrollResponsibilityAllowanceAbcRequest(2026, 7, seed.EmployeeId));

            Assert.Equal(1, result.Refresh.SkippedLocked);
            Assert.Equal(1, result.Calculate.SkippedLocked);
            Assert.Equal(0, result.Calculate.Updated);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persistedSummary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(x => x.Id == summary.Id);
        var persistedAbc = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(x => x.Id == abc.Id);

        Assert.True(persistedSummary.IsLocked);
        Assert.Equal(9_900_000m, persistedSummary.ResponsibilityAllowanceAmount);
        Assert.True(persistedAbc.IsLocked);
        Assert.Equal(4_200_000m, persistedAbc.ActualResponsibilityAllowanceAmount);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Ensure_assignments_for_summaries_creates_an_unassigned_position_rule_record()
    {
        fixture.RequireDatabase();
        var includedEmployee = await SeedEmployeeAsync(fixture);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var grade = CreateGrade(2026, 7, now);
        var mapping = CreateMapping(2026, 7, grade.Id, includedEmployee.PositionId, now);
        var summary = CreateSummary(includedEmployee.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(grade, mapping, summary);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var service = CreateWorkflowService(commandContext);
            var listBeforeCalculation = await service.GetGradeConfigAsync(2026, 7);

            var unassignedSummaryEmployee = Assert.Single(listBeforeCalculation.EmployeeAssignments);
            Assert.Equal(includedEmployee.EmployeeId, unassignedSummaryEmployee.EmployeeId);
            Assert.Equal(summary.Id, unassignedSummaryEmployee.Id);

            var assignmentPage = await service.SearchEmployeeAssignmentsAsync(
                new PayrollResponsibilityAllowanceEmployeeAssignmentQuery(2026, 7, null, null, 0, 50));

            var assignmentListItem = Assert.Single(assignmentPage.Rows);
            Assert.Equal(includedEmployee.EmployeeId, assignmentListItem.EmployeeId);
            Assert.Equal(1, assignmentPage.Summary.TotalCount);
            Assert.Single(assignmentPage.ActiveGrades);

            var assignedPage = await service.SearchEmployeeAssignmentsAsync(
                new PayrollResponsibilityAllowanceEmployeeAssignmentQuery(2026, 7, null, "assigned", 0, 50));
            var unassignedPage = await service.SearchEmployeeAssignmentsAsync(
                new PayrollResponsibilityAllowanceEmployeeAssignmentQuery(2026, 7, null, "unassigned", 0, 50));

            Assert.Empty(assignedPage.Rows);
            Assert.Single(unassignedPage.Rows);

            var result = await service.EnsureEmployeeAssignmentsForSummariesAsync(2026, 7);

            Assert.Equal(1, result.TotalEmployees);
            Assert.Equal(1, result.Updated);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var assignments = await verificationContext.PayrollResponsibilityAllowanceEmployeeAssignments
            .ToListAsync();

        var includedAssignment = Assert.Single(assignments);
        Assert.Equal(summary.Id, includedAssignment.PayrollAllowanceSummaryRecordId);
        Assert.Null(includedAssignment.GradeId);
        Assert.True(includedAssignment.IsAssignGradeFromPosition);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Other_allowance_page_search_translates_and_sums_filtered_amounts()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var matchingDetail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Hỗ trợ ăn ca",
            IsFixedAmount = true,
            AllowanceAmount = 500_000m,
            CreatedAtUtc = now,
            CreatedBy = "integration-test"
        };
        var otherDetail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Hỗ trợ điện thoại",
            IsFixedAmount = true,
            AllowanceAmount = 300_000m,
            CreatedAtUtc = now,
            CreatedBy = "integration-test"
        };

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, matchingDetail, otherDetail);
            await setupContext.SaveChangesAsync();
        }

        await using var commandContext = fixture.CreateDbContext();
        var page = await new DatabaseOtherAllowanceQueryService(commandContext).SearchPageAsync(
            new OtherAllowanceFilter(7, 2026, SearchText: "ăn ca"));

        var row = Assert.Single(page.Rows);
        Assert.Equal(matchingDetail.Id, row.Id);
        Assert.Equal(500_000m, page.TotalAllowanceAmount);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Other_allowance_update_persists_allowed_fields_and_synchronizes_summary_amount()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        summary.OtherAllowanceAmount = 200_000m;
        var createdAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var detail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Hỗ trợ ban đầu",
            IsFixedAmount = true,
            AllowanceAmount = 200_000m,
            Note = "Ghi chú cũ",
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "integration-test"
        };

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, detail);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var service = new DatabaseOtherAllowanceUpdateService(commandContext);
            var updated = await service.UpdateAsync(new UpdateOtherAllowanceRequest(
                detail.Id,
                "Hỗ trợ điều chỉnh",
                IsFixedAmount: false,
                AllowanceAmount: 500_000m,
                Note: "Ghi chú mới",
                OriginalUpdatedAtUtc: createdAtUtc,
                RequestedBy: "integration-test-update"));

            Assert.False(updated.IsFixedAmount);
            Assert.Equal(0m, updated.AllowanceAmount);
            Assert.Equal("Hỗ trợ điều chỉnh", updated.AllowanceName);
            Assert.Equal("Ghi chú mới", updated.Note);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persistedDetail = await verificationContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == detail.Id);
        var persistedSummary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == summary.Id);
        Assert.False(persistedDetail.IsFixedAmount);
        Assert.Equal(0m, persistedDetail.AllowanceAmount);
        Assert.Equal("integration-test-update", persistedDetail.UpdatedBy);
        Assert.Equal(0m, persistedSummary.OtherAllowanceAmount);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Other_allowance_update_rejects_locked_or_stale_rows()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        var createdAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var lockedDetail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Đã khóa",
            IsFixedAmount = true,
            AllowanceAmount = 100_000m,
            IsLocked = true,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "integration-test"
        };
        var staleDetail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Đã thay đổi",
            IsFixedAmount = true,
            AllowanceAmount = 100_000m,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc.AddMinutes(1),
            CreatedBy = "integration-test"
        };

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, lockedDetail, staleDetail);
            await setupContext.SaveChangesAsync();
        }

        await using var commandContext = fixture.CreateDbContext();
        var service = new DatabaseOtherAllowanceUpdateService(commandContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(new UpdateOtherAllowanceRequest(
            lockedDetail.Id,
            "Đã khóa",
            true,
            200_000m,
            null,
            createdAtUtc,
            "integration-test")));
        await Assert.ThrowsAsync<OtherAllowanceConflictException>(() => service.UpdateAsync(new UpdateOtherAllowanceRequest(
            staleDetail.Id,
            "Đã thay đổi",
            true,
            200_000m,
            null,
            createdAtUtc,
            "integration-test")));
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Other_allowance_lock_state_uses_version_and_rejects_locked_summary()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        var createdAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var detail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Hỗ trợ khóa",
            IsFixedAmount = true,
            AllowanceAmount = 100_000m,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "integration-test"
        };

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, detail);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var service = new DatabaseOtherAllowanceLockStateService(commandContext);
            await service.SetLockStateAsync(new SetOtherAllowanceLockStateRequest(
                detail.Id,
                IsLocked: true,
                OriginalUpdatedAtUtc: createdAtUtc,
                RequestedBy: "integration-test-lock"));
        }

        DateTime? lockedVersion;
        await using (var verificationContext = fixture.CreateDbContext())
        {
            var persistedDetail = await verificationContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == detail.Id);
            Assert.True(persistedDetail.IsLocked);
            Assert.Equal("integration-test-lock", persistedDetail.UpdatedBy);
            lockedVersion = persistedDetail.UpdatedAtUtc;
        }

        await using (var staleCommandContext = fixture.CreateDbContext())
        {
            var service = new DatabaseOtherAllowanceLockStateService(staleCommandContext);
            await Assert.ThrowsAsync<OtherAllowanceConflictException>(() => service.SetLockStateAsync(
                new SetOtherAllowanceLockStateRequest(detail.Id, false, createdAtUtc, "integration-test")));
        }

        await using (var lockSummaryContext = fixture.CreateDbContext())
        {
            var persistedSummary = await lockSummaryContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == summary.Id);
            persistedSummary.IsLocked = true;
            await lockSummaryContext.SaveChangesAsync();
        }

        await using var lockedSummaryCommandContext = fixture.CreateDbContext();
        var lockedSummaryService = new DatabaseOtherAllowanceLockStateService(lockedSummaryCommandContext);
        await Assert.ThrowsAsync<InvalidOperationException>(() => lockedSummaryService.SetLockStateAsync(
            new SetOtherAllowanceLockStateRequest(detail.Id, false, lockedVersion, "integration-test")));
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Recalculate_employee_assignments_copies_previous_manual_grades_and_reapplies_position_rules()
    {
        fixture.RequireDatabase();
        var manualEmployee = await SeedEmployeeAsync(fixture);
        var positionEmployee = await SeedEmployeeAsync(fixture);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var previousManualGrade = CreateGrade(2026, 6, now);
        var currentPositionGrade = CreateGrade(2026, 7, now);
        var mapping = CreateMapping(2026, 7, currentPositionGrade.Id, positionEmployee.PositionId, now);
        var previousManualSummary = CreateSummary(manualEmployee.EmployeeId, 2026, 6, isLocked: false, responsibilityAmount: 0m);
        var currentManualSummary = CreateSummary(manualEmployee.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        var currentPositionSummary = CreateSummary(positionEmployee.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        var previousManualAssignment = new PayrollResponsibilityAllowanceEmployeeAssignmentRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = previousManualSummary.Id,
            GradeId = previousManualGrade.Id,
            IsAssignGradeFromPosition = false,
            CreatedAtUtc = now
        };
        var currentManualAssignment = new PayrollResponsibilityAllowanceEmployeeAssignmentRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = currentManualSummary.Id,
            GradeId = null,
            IsAssignGradeFromPosition = true,
            CreatedAtUtc = now
        };
        var currentPositionAssignment = new PayrollResponsibilityAllowanceEmployeeAssignmentRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = currentPositionSummary.Id,
            GradeId = previousManualGrade.Id,
            IsAssignGradeFromPosition = true,
            CreatedAtUtc = now
        };

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(
                previousManualGrade,
                currentPositionGrade,
                mapping,
                previousManualSummary,
                currentManualSummary,
                currentPositionSummary,
                previousManualAssignment,
                currentManualAssignment,
                currentPositionAssignment);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var service = CreateWorkflowService(commandContext);
            var result = await service.RecalculateEmployeeAssignmentsAsync(2026, 7);

            Assert.Equal(2, result.TotalEmployees);
            Assert.Equal(2, result.Updated);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var assignmentsBySummaryId = await verificationContext.PayrollResponsibilityAllowanceEmployeeAssignments
            .ToDictionaryAsync(assignment => assignment.PayrollAllowanceSummaryRecordId);

        var persistedManualAssignment = assignmentsBySummaryId[currentManualSummary.Id];
        Assert.Equal(previousManualGrade.Id, persistedManualAssignment.GradeId);
        Assert.False(persistedManualAssignment.IsAssignGradeFromPosition);

        var persistedPositionAssignment = assignmentsBySummaryId[currentPositionSummary.Id];
        Assert.Equal(currentPositionGrade.Id, persistedPositionAssignment.GradeId);
        Assert.True(persistedPositionAssignment.IsAssignGradeFromPosition);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Recalculate_rolls_back_refresh_rows_when_the_calculation_save_fails()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var grade = CreateGrade(2026, 7, now);
        var mapping = CreateMapping(2026, 7, grade.Id, seed.PositionId, now);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(grade, mapping);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext(new ThrowOnSecondSaveChangesInterceptor()))
        {
            var service = CreateWorkflowService(commandContext);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecalculateAbcAsync(
                new RefreshPayrollResponsibilityAllowanceAbcRequest(2026, 7, seed.EmployeeId)));

            Assert.Equal("Forced failure after refresh save.", exception.Message);
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.False(await verificationContext.PayrollAllowanceSummaryRecords.AnyAsync(x =>
            x.EmployeeId == seed.EmployeeId && x.PayrollYear == 2026 && x.PayrollMonth == 7));
        Assert.False(await verificationContext.PayrollResponsibilityAllowanceAbcRows.AnyAsync(x =>
            x.EmployeeId == seed.EmployeeId && x.Year == 2026 && x.Month == 7));
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Refresh_creates_abc_for_an_inactive_employee_when_the_summary_row_exists()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture, status: 5);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.Add(summary);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateWorkflowService(commandContext).RefreshAbcAsync(
                new RefreshPayrollResponsibilityAllowanceAbcRequest(2026, 7, null));

            Assert.Equal(1, result.TotalEmployees);
            Assert.Equal(1, result.Inserted);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var abc = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync();

        Assert.Equal(summary.Id, abc.PayrollAllowanceSummaryRecordId);
        Assert.Equal(seed.EmployeeId, abc.EmployeeId);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Refresh_updates_source_snapshot_without_recalculating_abc_or_allowance_amount()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var grade = CreateGrade(2026, 7, now);
        var mapping = CreateMapping(2026, 7, grade.Id, seed.PositionId, now);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_200_000m);
        var abc = CreateAbc(seed, summary.Id, 2026, 7, isLocked: false, actualAmount: 1_200_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(grade, mapping, summary, abc);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateWorkflowService(commandContext).RefreshAbcAsync(
                new RefreshPayrollResponsibilityAllowanceAbcRequest(2026, 7, seed.EmployeeId));

            Assert.Equal(1, result.Updated);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persisted = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(row => row.Id == abc.Id);

        Assert.Equal(grade.Id, persisted.GradeId);
        Assert.Equal("B", persisted.AbcRating);
        Assert.Equal(1_200_000m, persisted.ActualResponsibilityAllowanceAmount);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Recalculate_one_employee_does_not_seed_summary_rows_for_other_employees()
    {
        fixture.RequireDatabase();
        var target = await SeedEmployeeAsync(fixture);
        var otherEmployee = await SeedEmployeeAsync(fixture);

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateWorkflowService(commandContext).RecalculateAbcAsync(
                new RefreshPayrollResponsibilityAllowanceAbcRequest(2026, 7, target.EmployeeId));

            Assert.Equal(1, result.Refresh.TotalEmployees);
            Assert.Equal(1, result.Refresh.SkippedMissingSource);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var summaries = await verificationContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollYear == 2026 && row.PayrollMonth == 7)
            .ToListAsync();

        var summary = Assert.Single(summaries);
        Assert.Equal(target.EmployeeId, summary.EmployeeId);
        Assert.DoesNotContain(summaries, row => row.EmployeeId == otherEmployee.EmployeeId);
        Assert.Empty(await verificationContext.PayrollResponsibilityAllowanceAbcRows.ToListAsync());
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Recalculate_preserves_existing_snapshot_when_assignment_source_is_missing()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_000_000m);
        var abc = CreateAbc(seed, summary.Id, 2026, 7, isLocked: false, actualAmount: 1_000_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, abc);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateWorkflowService(commandContext).RecalculateAbcAsync(
                new RefreshPayrollResponsibilityAllowanceAbcRequest(
                    2026,
                    7,
                    seed.EmployeeId,
                    abc.UpdatedAtUtc ?? abc.CreatedAtUtc));

            Assert.Equal(1, result.Refresh.SkippedMissingSource);
            Assert.Equal(0, result.Calculate.Updated);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persisted = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(row => row.Id == abc.Id);

        Assert.Equal(1_000_000m, persisted.StandardResponsibilityAllowanceAmount);
        Assert.Equal(1_000_000m, persisted.ActualResponsibilityAllowanceAmount);
        Assert.Equal("B", persisted.AbcRating);
        Assert.Equal(20m, persisted.ActualWorkDays);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Recalculate_one_employee_rejects_a_stale_concurrency_timestamp()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_000_000m);
        var abc = CreateAbc(seed, summary.Id, 2026, 7, isLocked: false, actualAmount: 1_000_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, abc);
            await setupContext.SaveChangesAsync();
        }

        await using var commandContext = fixture.CreateDbContext();
        var exception = await Assert.ThrowsAsync<ResponsibilityAllowanceConflictException>(() =>
            CreateWorkflowService(commandContext).RecalculateAbcAsync(
                new RefreshPayrollResponsibilityAllowanceAbcRequest(
                    2026,
                    7,
                    seed.EmployeeId,
                    (abc.UpdatedAtUtc ?? abc.CreatedAtUtc).AddTicks(1))));

        Assert.Equal(
            "Dữ liệu phụ cấp trách nhiệm đã thay đổi. Vui lòng tải lại trước khi thao tác.",
            exception.Message);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Save_adjustment_applies_manual_grade_rounds_bonus_and_synchronizes_summary_amount()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var manualGrade = CreateGrade(2026, 7, now);
        manualGrade.Code = "TN-MANUAL";
        manualGrade.StandardResponsibilityAllowanceAmount = 1_000_000m;
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 0m);
        var abc = CreateAbc(seed, summary.Id, 2026, 7, isLocked: false, actualAmount: 0m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(manualGrade, summary, abc);
            await setupContext.SaveChangesAsync();
        }

        PayrollResponsibilityAllowanceAbcItemDto result;
        await using (var commandContext = fixture.CreateDbContext())
        {
            result = await CreateWorkflowService(commandContext).SaveAdjustmentAsync(
                new SavePayrollResponsibilityAllowanceAdjustmentRequest(
                    EmployeeAssignmentId: null,
                    Year: 2026,
                    Month: 7,
                    EmployeeId: seed.EmployeeId,
                    GradeId: manualGrade.Id,
                    IsActive: true,
                    Note: "manual adjustment",
                    MonthlyPerformanceBonusAmount: 0.87656m,
                    IsPerformanceBonusExcluded: false,
                    OriginalUpdatedAtUtc: abc.UpdatedAtUtc ?? abc.CreatedAtUtc));
        }

        Assert.Equal(manualGrade.Id, result.GradeId);
        Assert.Equal(0.8766m, result.MonthlyPerformanceBonusAmount);
        Assert.Equal(788_940m, result.ActualResponsibilityAllowanceAmount);

        await using var verificationContext = fixture.CreateDbContext();
        var persistedAssignment = await verificationContext.PayrollResponsibilityAllowanceEmployeeAssignments.SingleAsync();
        var persistedAbc = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(x => x.Id == abc.Id);
        var persistedSummary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(x => x.Id == summary.Id);

        Assert.Equal(manualGrade.Id, persistedAssignment.GradeId);
        Assert.False(persistedAssignment.IsAssignGradeFromPosition);
        Assert.Equal("manual adjustment", persistedAssignment.Note);
        Assert.Equal(manualGrade.Id, persistedAbc.GradeId);
        Assert.Equal(0.8766m, persistedAbc.MonthlyPerformanceBonusAmount);
        Assert.Equal(788_940m, persistedAbc.ActualResponsibilityAllowanceAmount);
        Assert.Equal(788_940m, persistedSummary.ResponsibilityAllowanceAmount);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Batch_lock_rejects_any_stale_token_without_locking_other_target_rows()
    {
        fixture.RequireDatabase();
        var firstEmployee = await SeedEmployeeAsync(fixture);
        var secondEmployee = await SeedEmployeeAsync(fixture);
        var firstSummary = CreateSummary(firstEmployee.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_000m);
        var secondSummary = CreateSummary(secondEmployee.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 2_000m);
        var firstAbc = CreateAbc(firstEmployee, firstSummary.Id, 2026, 7, isLocked: false, actualAmount: 1_000m);
        var secondAbc = CreateAbc(secondEmployee, secondSummary.Id, 2026, 7, isLocked: false, actualAmount: 2_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(firstSummary, secondSummary, firstAbc, secondAbc);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            await Assert.ThrowsAsync<ResponsibilityAllowanceConflictException>(() =>
                CreateWorkflowService(commandContext).SetLockStateBatchAsync(
                    new SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest(
                        2026,
                        7,
                        IsLocked: true,
                        EmployeeIds: [firstEmployee.EmployeeId, secondEmployee.EmployeeId],
                        ConcurrencyTokens:
                        [
                            new PayrollResponsibilityAllowanceAbcConcurrencyToken(
                                firstEmployee.EmployeeId,
                                firstAbc.UpdatedAtUtc ?? firstAbc.CreatedAtUtc),
                            new PayrollResponsibilityAllowanceAbcConcurrencyToken(
                                secondEmployee.EmployeeId,
                                (secondAbc.UpdatedAtUtc ?? secondAbc.CreatedAtUtc).AddTicks(1))
                        ])));
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.All(
            await verificationContext.PayrollResponsibilityAllowanceAbcRows.ToListAsync(),
            row => Assert.False(row.IsLocked));
        Assert.Empty(await verificationContext.AuditEvents.ToListAsync());
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Batch_lock_updates_selected_rows_and_writes_an_operation_audit_event()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_000_000m);
        var abc = CreateAbc(seed, summary.Id, 2026, 7, isLocked: false, actualAmount: 1_000_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, abc);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateWorkflowService(commandContext).SetLockStateBatchAsync(
                new SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest(
                    2026,
                    7,
                    IsLocked: true,
                    EmployeeIds: [seed.EmployeeId],
                    ConcurrencyTokens: [new PayrollResponsibilityAllowanceAbcConcurrencyToken(
                        seed.EmployeeId,
                        abc.UpdatedAtUtc ?? abc.CreatedAtUtc)]));

            Assert.Equal(1, result.TargetRowCount);
            Assert.Equal(1, result.UpdatedCount);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persistedAbc = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(x => x.Id == abc.Id);
        var auditEvent = await verificationContext.AuditEvents.SingleAsync();

        Assert.True(persistedAbc.IsLocked);
        Assert.Equal(AuditActions.ResponsibilityAllowance.BatchLockStateChanged, auditEvent.Action);
        Assert.Equal(AuditEntityTypes.ResponsibilityAllowance, auditEvent.EntityType);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Lock_then_unlock_uses_the_returned_version_and_clears_lock_metadata()
    {
        fixture.RequireDatabase();
        var seed = await SeedEmployeeAsync(fixture);
        var summary = CreateSummary(seed.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_000_000m);
        var abc = CreateAbc(seed, summary.Id, 2026, 7, isLocked: false, actualAmount: 1_000_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summary, abc);
            await setupContext.SaveChangesAsync();
        }

        PayrollResponsibilityAllowanceAbcItemDto unlocked;
        await using (var commandContext = fixture.CreateDbContext())
        {
            var service = CreateWorkflowService(commandContext);
            var locked = await service.SetLockStateAsync(
                seed.EmployeeId,
                2026,
                7,
                isLocked: true,
                abc.UpdatedAtUtc ?? abc.CreatedAtUtc);

            Assert.True(locked.IsLocked);
            Assert.NotNull(locked.LockedAtUtc);
            Assert.NotNull(locked.LockedBy);

            unlocked = await service.SetLockStateAsync(
                seed.EmployeeId,
                2026,
                7,
                isLocked: false,
                locked.UpdatedAtUtc);
        }

        Assert.False(unlocked.IsLocked);
        Assert.Null(unlocked.LockedAtUtc);
        Assert.Null(unlocked.LockedBy);

        await using var verificationContext = fixture.CreateDbContext();
        var persisted = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(x => x.Id == abc.Id);
        Assert.False(persisted.IsLocked);
        Assert.Null(persisted.LockedAtUtc);
        Assert.Null(persisted.LockedBy);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Update_performance_bonus_for_period_updates_unlocked_rows_when_concurrency_tokens_match()
    {
        fixture.RequireDatabase();
        var unlockedEmployee = await SeedEmployeeAsync(fixture);
        var lockedEmployee = await SeedEmployeeAsync(fixture);
        var unlockedSummary = CreateSummary(unlockedEmployee.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_000m);
        var lockedSummary = CreateSummary(lockedEmployee.EmployeeId, 2026, 7, isLocked: true, responsibilityAmount: 2_000m);
        var unlockedAbc = CreateAbc(unlockedEmployee, unlockedSummary.Id, 2026, 7, isLocked: false, actualAmount: 1_000m);
        var lockedAbc = CreateAbc(lockedEmployee, lockedSummary.Id, 2026, 7, isLocked: true, actualAmount: 2_000m);

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(unlockedSummary, lockedSummary, unlockedAbc, lockedAbc);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateWorkflowService(commandContext).UpdatePerformanceBonusForPeriodAsync(
                2026,
                7,
                0.9m,
                [
                    new PayrollResponsibilityAllowanceAbcConcurrencyToken(
                        unlockedEmployee.EmployeeId,
                        unlockedAbc.UpdatedAtUtc ?? unlockedAbc.CreatedAtUtc),
                    new PayrollResponsibilityAllowanceAbcConcurrencyToken(
                        lockedEmployee.EmployeeId,
                        lockedAbc.UpdatedAtUtc ?? lockedAbc.CreatedAtUtc)
                ]);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.Updated);
            Assert.Equal(1, result.SkippedLocked);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persistedUnlockedAbc = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(x => x.Id == unlockedAbc.Id);
        var persistedLockedAbc = await verificationContext.PayrollResponsibilityAllowanceAbcRows.SingleAsync(x => x.Id == lockedAbc.Id);
        var persistedUnlockedSummary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(x => x.Id == unlockedSummary.Id);

        Assert.Equal(0.9m, persistedUnlockedAbc.MonthlyPerformanceBonusAmount);
        Assert.Equal(900m, persistedUnlockedAbc.ActualResponsibilityAllowanceAmount);
        Assert.Equal(900m, persistedUnlockedSummary.ResponsibilityAllowanceAmount);
        Assert.Equal(1m, persistedLockedAbc.MonthlyPerformanceBonusAmount);
        Assert.Equal(2_000m, persistedLockedAbc.ActualResponsibilityAllowanceAmount);
    }

    [PostgreSqlResponsibilityAllowanceFact]
    public async Task Monthly_abc_query_filters_and_pages_rows_while_summary_and_export_cover_the_whole_period()
    {
        fixture.RequireDatabase();
        var employeeA = await SeedEmployeeAsync(fixture);
        var employeeB = await SeedEmployeeAsync(fixture);
        var employeeC = await SeedEmployeeAsync(fixture);
        var summaryA = CreateSummary(employeeA.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 1_000m);
        var summaryB = CreateSummary(employeeB.EmployeeId, 2026, 7, isLocked: true, responsibilityAmount: 2_000m);
        var summaryC = CreateSummary(employeeC.EmployeeId, 2026, 7, isLocked: false, responsibilityAmount: 3_000m);
        var abcA = CreateAbc(employeeA, summaryA.Id, 2026, 7, isLocked: false, actualAmount: 1_000m);
        abcA.GradeId = Guid.NewGuid();
        abcA.GradeCode = "TN-A";
        abcA.GradeName = "A";
        abcA.AbcRating = "A";
        var abcB = CreateAbc(employeeB, summaryB.Id, 2026, 7, isLocked: true, actualAmount: 2_000m);
        abcB.GradeId = Guid.NewGuid();
        abcB.GradeCode = "TN-B";
        abcB.GradeName = "B";
        abcB.AbcRating = "B";
        var abcC = CreateAbc(employeeC, summaryC.Id, 2026, 7, isLocked: false, actualAmount: 3_000m);
        abcC.GradeId = Guid.NewGuid();
        abcC.GradeCode = "TN-C";
        abcC.GradeName = "C";
        abcC.AbcRating = "B";

        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.AddRange(summaryA, summaryB, summaryC, abcA, abcB, abcC);
            await setupContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var service = CreateWorkflowService(queryContext);
        var page = await service.SearchAbcAsync(
            new PayrollResponsibilityAllowanceAbcQuery(2026, 7, null, "abc-b", Skip: 1, Take: 1));
        var exported = await service.ExportAsync(new PayrollResponsibilityAllowanceAbcExportRequest(2026, 7, "xlsx"));

        var expectedBEmployeeCodes = new[] { employeeB.EmployeeCode, employeeC.EmployeeCode }
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(expectedBEmployeeCodes[1], Assert.Single(page.Rows).EmployeeCode);
        Assert.Equal(3, page.Summary.TotalCount);
        Assert.Equal(3, page.Summary.ActiveCount);
        Assert.Equal(1, page.Summary.AbcACount);
        Assert.Equal(2, page.Summary.AbcBCount);
        Assert.Equal(2, page.Summary.OpenCount);
        Assert.Equal(1, page.Summary.LockedCount);
        Assert.Equal(
            new[] { employeeA.EmployeeCode, employeeB.EmployeeCode, employeeC.EmployeeCode }.OrderBy(code => code, StringComparer.Ordinal),
            exported.Select(item => item.EmployeeCode));
        Assert.All(exported, item => Assert.NotEqual(default, item.ActualResponsibilityAllowanceAmount));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportAsync(new PayrollResponsibilityAllowanceAbcExportRequest(2026, 7, "csv")));
    }

    private static TestMonthlyAbcReadService CreateWorkflowService(
        ApplicationDbContext dbContext)
    {
        var auditScope = new AsyncLocalAuditScope();
        return new TestMonthlyAbcReadService(
            dbContext,
            auditScope,
            new AuditedMutation(dbContext, auditScope),
            new DatabaseBasicSalaryWorkdaySource(dbContext),
            NullLogger<TestMonthlyAbcReadService>.Instance);
    }

    private sealed class TestMonthlyAbcReadService(
        ApplicationDbContext dbContext,
        IAuditScope auditScope,
        IAuditedMutation auditedMutation,
        IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
        ILogger<TestMonthlyAbcReadService> logger)
        : PayrollResponsibilityAllowanceServiceBase(
            dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
          IPayrollResponsibilityAllowanceMonthlyAbcQueryService,
          IPayrollResponsibilityAllowanceMonthlyAbcExportService;

    private static async Task<EmployeeSeed> SeedEmployeeAsync(
        ResponsibilityAllowancePostgreSqlFixture fixture,
        int status = 1)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var department = new AttendanceDepartmentRow
        {
            Id = Guid.NewGuid(),
            Code = $"D-{Guid.NewGuid():N}"[..14],
            CenterName = "Test Center",
            DepartmentOrWorkshopName = "Test Department",
            Status = status,
            CreatedAtUtc = now
        };
        var position = new AttendanceGatewayPositionRow
        {
            Id = Guid.NewGuid(),
            Code = $"P-{Guid.NewGuid():N}"[..14],
            Name = "Test Position",
            Status = 1,
            EmployeeCount = 1,
            CreatedAtUtc = now
        };
        var employee = new AttendanceGatewayEmployeeRow
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            PositionId = position.Id,
            EmployeeCode = $"E-{Guid.NewGuid():N}"[..14],
            FirstName = "Integration",
            LastName = "Responsibility",
            HireDate = now,
            Status = 1,
            IsDeleted = false,
            CreatedAtUtc = now
        };

        await using var setupContext = fixture.CreateDbContext();
        setupContext.AddRange(department, position, employee);
        await setupContext.SaveChangesAsync();

        return new EmployeeSeed(employee.Id, employee.EmployeeCode, "Responsibility Integration", department.DepartmentOrWorkshopName, position.Id, position.Name);
    }

    private static PayrollAllowanceSummaryRecordRow CreateSummary(
        Guid employeeId,
        int year,
        int month,
        bool isLocked,
        decimal responsibilityAmount)
    {
        return new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollYear = checked((short)year),
            PayrollMonth = checked((short)month),
            ResponsibilityAllowanceAmount = responsibilityAmount,
            SeniorityAllowanceAmount = 0m,
            AttendanceAllowanceAmount = 0m,
            MealAllowanceAmount = 0m,
            HazardAllowanceAmount = 0m,
            OtherAllowanceAmount = 0m,
            LeaveHolidayAllowanceAmount = 0m,
            IsLocked = isLocked,
            CreatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            CreatedBy = "integration-test"
        };
    }

    private static PayrollResponsibilityAllowanceAbcRow CreateAbc(
        EmployeeSeed employee,
        Guid summaryId,
        int year,
        int month,
        bool isLocked,
        decimal actualAmount)
    {
        return new PayrollResponsibilityAllowanceAbcRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summaryId,
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.EmployeeName,
            DepartmentName = employee.DepartmentName,
            PositionId = employee.PositionId,
            PositionName = employee.PositionName,
            GradeName = string.Empty,
            Year = year,
            Month = month,
            ActualWorkDays = 20m,
            StandardWorkDays = 26m,
            AbcRating = "B",
            MonthlyPerformanceBonusAmount = 1m,
            StandardResponsibilityAllowanceAmount = actualAmount,
            ActualResponsibilityAllowanceAmount = actualAmount,
            IsLocked = isLocked,
            CreatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };
    }

    private static PayrollResponsibilityAllowanceGradeRow CreateGrade(int year, int month, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = year,
            Month = month,
            Code = "TN-TEST",
            Name = "Trách nhiệm kiểm thử",
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
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = year,
            Month = month,
            GradeId = gradeId,
            PositionId = positionId,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

    private sealed record EmployeeSeed(
        Guid EmployeeId,
        string EmployeeCode,
        string EmployeeName,
        string DepartmentName,
        Guid PositionId,
        string PositionName);

    private sealed class ThrowOnSecondSaveChangesInterceptor : SaveChangesInterceptor
    {
        private int saveCount;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref saveCount) == 2)
            {
                throw new InvalidOperationException("Forced failure after refresh save.");
            }

            return ValueTask.FromResult(result);
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ResponsibilityAllowancePostgreSqlCollection
    : ICollectionFixture<ResponsibilityAllowancePostgreSqlFixture>
{
    public const string Name = "Responsibility allowance PostgreSQL integration";
}

public sealed class ResponsibilityAllowancePostgreSqlFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable = "VNTA_RESPONSIBILITY_ALLOWANCE_TEST_DB";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        if (string.IsNullOrWhiteSpace(builder.Database)
            || !builder.Database.StartsWith("vnta_responsibility_allowance_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target a disposable database named vnta_responsibility_allowance_test*.");
        }

        connectionString = builder.ConnectionString;
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (connectionString is null)
        {
            return;
        }

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    public void RequireDatabase()
    {
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} to run responsibility-allowance PostgreSQL integration tests.");
        }
    }

    public ApplicationDbContext CreateDbContext(params IInterceptor[] interceptors)
    {
        RequireDatabase();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);
        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlResponsibilityAllowanceFactAttribute : FactAttribute
{
    public PostgreSqlResponsibilityAllowanceFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNTA_RESPONSIBILITY_ALLOWANCE_TEST_DB")))
        {
            Skip = "Set VNTA_RESPONSIBILITY_ALLOWANCE_TEST_DB to a disposable vnta_responsibility_allowance_test* PostgreSQL database to run these tests.";
        }
    }
}
