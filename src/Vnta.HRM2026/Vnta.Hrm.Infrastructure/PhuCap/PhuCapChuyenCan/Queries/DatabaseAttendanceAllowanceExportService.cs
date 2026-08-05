using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;

/// <summary>Exports an authorized whole-period read model without tracking payroll entities.</summary>
public sealed class DatabaseAttendanceAllowanceExportService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IAttendanceAllowanceRequestValidator requestValidator) : IAttendanceAllowanceExportService
{
    private const int MaximumExportRowCount = 10000;

    public async Task<IReadOnlyList<AttendanceAllowanceExportRowDto>> ExportAsync(AttendanceAllowanceExportRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        var current = auditScope.Current;
        var command = current ?? new AuditCommand(Guid.NewGuid(), AuditActions.AttendanceAllowance.Exported,
            new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker), Guid.NewGuid().ToString("N"), AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string> { ["auditScope"] = "system-fallback" });
        return await auditedMutation.ExecuteAsync(command with { ActionIntent = AuditActions.AttendanceAllowance.Exported }, async token =>
        {
            var rows = await AttendanceAllowanceReadProjection.ApplyStableOrder(AttendanceAllowanceReadProjection.BuildQuery(dbContext,
                    new AttendanceAllowanceResultFilter(PayrollAllowanceKind.Attendance, request.PayrollMonth, request.PayrollYear, null, 0, 1), true))
                .Take(MaximumExportRowCount + 1)
                .Select(x => new AttendanceAllowanceExportRowDto(
                    AttendanceAllowanceReadProjection.SanitizeExportText(x.Employee == null ? null : x.Employee.EmployeeCode),
                    AttendanceAllowanceReadProjection.SanitizeExportText(x.Employee == null ? null : string.Join(" ", new[] { x.Employee.LastName, x.Employee.FirstName }.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()))),
                    AttendanceAllowanceReadProjection.SanitizeExportText(x.Department == null ? null : x.Department.GroupName ?? x.Department.TeamName ?? x.Department.DepartmentOrWorkshopName ?? x.Department.CenterName),
                    AttendanceAllowanceReadProjection.SanitizeExportText(x.Position == null ? null : x.Position.Name), x.Summary.PayrollMonth, x.Summary.PayrollYear,
                    x.Detail.ActualWorkdayCount, x.Detail.StandardWorkdayCount, x.Detail.AttendanceRate, x.Detail.AllowanceAmount,
                    x.Detail.IsLocked || x.Summary.IsLocked, x.Detail.AdministrativeWorkdayCount, x.Detail.LateEarlyDeductionDays,
                    x.Detail.CtlWorkdayCount, x.Detail.Kqcc, x.Detail.HasKpViolation)).ToListAsync(token);
            if(rows.Count > MaximumExportRowCount) throw new InvalidOperationException($"Kỳ lương có quá {MaximumExportRowCount:N0} dòng, chưa thể xuất trong một tệp.");
            return (IReadOnlyList<AttendanceAllowanceExportRowDto>)rows;
        }, rows => new AuditOperationEvent(AuditActions.AttendanceAllowance.Exported, AuditEntityTypes.AttendanceAllowance,
            EntityDisplayName: $"{request.PayrollMonth:00}/{request.PayrollYear}", Outcome: rows.Count == 0 ? AuditOperationOutcome.NoChanges : AuditOperationOutcome.Succeeded,
            Metadata: new Dictionary<string, string> { ["format"] = request.Format.ToString(), ["scope"] = "wholePeriod", ["payrollPeriod"] = $"{request.PayrollMonth:00}/{request.PayrollYear}", ["rowCount"] = rows.Count.ToString() }), cancellationToken);
    }
}
