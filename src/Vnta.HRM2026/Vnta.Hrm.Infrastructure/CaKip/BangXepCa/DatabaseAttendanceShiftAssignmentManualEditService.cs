using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.CaKip.BangXepCa;

public sealed class DatabaseAttendanceShiftAssignmentManualEditService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope)
    : IAttendanceShiftAssignmentManualEditService
{
    private const int ResignedEmployeeStatus = 5;
    private const int ActiveShiftStatus = 1;
    private const string ManualCreationType = "Manual";

    public async Task SaveManualAsync(
        UpsertAttendanceShiftAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.EmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Không xác định được nhân viên cần đổi ca.");
        }

        if (request.WorkDate == default)
        {
            throw new InvalidOperationException("Không xác định được ngày cần đổi ca.");
        }

        if (request.ShiftId == Guid.Empty)
        {
            throw new InvalidOperationException("Hãy chọn ca làm việc.");
        }

        await EnsureShiftAssignmentsTableAsync(cancellationToken);

        var employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(
                employee => employee.Id == request.EmployeeId
                    && !employee.IsDeleted
                    && employee.Status != ResignedEmployeeStatus,
                cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException("Không tìm thấy nhân viên đang làm việc để đổi ca.");
        }

        var shiftExists = await dbContext.Shifts
            .AsNoTracking()
            .AnyAsync(
                shift => shift.Id == request.ShiftId && shift.Status == ActiveShiftStatus,
                cancellationToken);
        if (!shiftExists)
        {
            throw new InvalidOperationException("Ca làm việc không tồn tại hoặc đã ngừng sử dụng.");
        }

        var row = await dbContext.ShiftAssignments.SingleOrDefaultAsync(
            assignment => assignment.EmployeeId == request.EmployeeId
                && assignment.WorkDate == request.WorkDate,
            cancellationToken);

        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        if (row is null)
        {
            row = new AttendanceShiftAssignmentRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                ShiftId = request.ShiftId,
                WorkDate = request.WorkDate,
                CreationType = ManualCreationType,
                Notes = BuildManualNote(request.Source),
                CreatedAtUtc = now,
                UpdatedAtUtc = null
            };

            dbContext.ShiftAssignments.Add(row);
            RefineAuditActionIfActive(AuditActions.ShiftAssignment.ManualCreated);
        }
        else
        {
            row.ShiftId = request.ShiftId;
            row.CreationType = ManualCreationType;
            row.Notes = BuildManualNote(request.Source);
            row.UpdatedAtUtc = now;
            RefineAuditActionIfActive(AuditActions.ShiftAssignment.ManualUpdated);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void RefineAuditActionIfActive(string action)
    {
        if (auditScope.Current is not null)
        {
            auditScope.RefineAction(action);
        }
    }

    private static string BuildManualNote(string? source) =>
        $"Manual shift edit; source:{Normalize(source) ?? "Unknown"}";

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private async Task EnsureShiftAssignmentsTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.shift_assignments (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "ShiftId" uuid NOT NULL,
                "WorkDate" date NOT NULL,
                "CreationType" character varying(30) NOT NULL,
                "SourceBatchId" uuid NULL,
                "Notes" character varying(1000) NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_shift_assignments" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_shift_assignments_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_shift_assignments_ShiftId"
                    FOREIGN KEY ("ShiftId") REFERENCES public.shifts ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_shift_assignments_EmployeeId_WorkDate"
                ON public.shift_assignments ("EmployeeId", "WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_shift_assignments_WorkDate"
                ON public.shift_assignments ("WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_shift_assignments_ShiftId_WorkDate"
                ON public.shift_assignments ("ShiftId", "WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_shift_assignments_CreationType"
                ON public.shift_assignments ("CreationType");
            """,
            cancellationToken);
    }
}
