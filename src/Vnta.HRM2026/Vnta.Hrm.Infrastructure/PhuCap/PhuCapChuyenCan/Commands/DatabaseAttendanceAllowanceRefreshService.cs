using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>Recalculates authoritative attendance snapshots for one row or a payroll period.</summary>
public sealed class DatabaseAttendanceAllowanceRefreshService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IAttendanceAllowanceWorkdayInputSource workdaySource,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    AttendanceAllowanceWorkdayMetricPolicy workdayMetricPolicy,
    AttendanceAllowanceCalculationPolicy calculationPolicy,
    IAttendanceAllowanceRefreshRequestValidator requestValidator) : IAttendanceAllowanceRefreshService
{
    public async Task<RefreshAttendanceAllowanceResult> RefreshAsync(RefreshAttendanceAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        try { return await auditedMutation.ExecuteAsync(AttendanceAllowanceCommandSupport.CreateOperationAuditCommand(auditScope, AuditActions.AttendanceAllowance.Refresh), token => RefreshCoreAsync(request, token), AttendanceAllowanceCommandSupport.RefreshAudit, cancellationToken); }
        catch(DbUpdateConcurrencyException) { throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Dữ liệu phụ cấp chuyên cần đã thay đổi trong khi tính lại. Hãy tải lại và thực hiện lại thao tác."); }
    }

    private async Task<RefreshAttendanceAllowanceResult> RefreshCoreAsync(RefreshAttendanceAllowanceRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var month = (short)request.TargetPayrollMonth; var year = (short)request.TargetPayrollYear;
        var summaries = await dbContext.PayrollAllowanceSummaryRecords.Where(s => s.PayrollMonth == month && s.PayrollYear == year && (!request.PayrollAllowanceSummaryRecordId.HasValue || s.Id == request.PayrollAllowanceSummaryRecordId.Value)).ToListAsync(token);
        if(summaries.Count == 0)
        {
            if(request.PayrollAllowanceSummaryRecordId.HasValue) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Không tìm thấy dòng phụ cấp chuyên cần thuộc kỳ lương cần làm mới.");
            return new(month, year, 0, 0, 0, request.PayrollAllowanceSummaryRecordId);
        }
        var ids = summaries.Select(x => x.Id).ToArray();
        var details = await dbContext.PayrollAttendanceAllowanceRecords.Where(x => ids.Contains(x.PayrollAllowanceSummaryRecordId)).ToDictionaryAsync(x => x.PayrollAllowanceSummaryRecordId, token);
        foreach(var tracked in dbContext.ChangeTracker.Entries<PayrollAttendanceAllowanceRecordRow>().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Unchanged).Select(x => x.Entity).Where(x => ids.Contains(x.PayrollAllowanceSummaryRecordId))) details[tracked.PayrollAllowanceSummaryRecordId] = tracked;
        var employeeIds = summaries.Select(x => x.EmployeeId).Where(x => x != Guid.Empty).Distinct().ToArray();
        var workdays = await workdaySource.LoadByEmployeeIdAsync(new(year, month, employeeIds), token);
        var standards = await basicSalaryWorkdaySource.LoadStandardWorkingDaysAsync(year, month, employeeIds, token);
        var now = AttendanceAllowanceCommandSupport.ToDatabaseTimestamp(DateTime.UtcNow); var actor = AttendanceAllowanceCommandSupport.CurrentActorId(auditScope); var updated = 0; var skipped = 0;
        foreach(var summary in summaries)
        {
            details.TryGetValue(summary.Id, out var detail);
            if(summary.IsLocked || detail?.IsLocked == true) { skipped++; continue; }
            var metric = workdays.TryGetValue(summary.EmployeeId, out var rows) ? workdayMetricPolicy.Calculate(rows) : workdayMetricPolicy.Calculate([]);
            var standard = standards.GetValueOrDefault(summary.EmployeeId);
            if(detail is null) { detail = new PayrollAttendanceAllowanceRecordRow { PayrollAllowanceSummaryRecordId = summary.Id, CreatedAtUtc = now, CreatedBy = AttendanceAllowanceCommandSupport.SystemActor }; dbContext.PayrollAttendanceAllowanceRecords.Add(detail); details[summary.Id] = detail; }
            var calculation = calculationPolicy.Calculate(new AttendanceAllowanceCalculationInput(standard, metric.AttendanceWorkdayCount, null, metric.KpViolationState));
            detail.StandardWorkdayCount = standard; detail.ActualWorkdayCount = metric.AttendanceWorkdayCount; detail.AdministrativeWorkdayCount = metric.AdministrativeWorkdayCount; detail.LateEarlyDeductionDays = metric.LateEarlyDeductionDays; detail.AttendanceRate = calculation.AttendanceRate; detail.AllowanceAmount = calculation.ActualAllowanceAmount; detail.AppliedRuleKey = calculation.AppliedRule.ToStorageValue(); detail.AttendanceClass = calculation.AttendanceClass.ToStorageValue(); detail.CtlWorkdayCount = metric.AttendanceWorkdayCount; detail.LateEarlyMinutes = metric.LateEarlyMinutes; detail.Kqcc = calculation.MissingWorkdayCount; detail.HasKpViolation = calculation.KpViolationState == AttendanceAllowanceKpViolationState.Present; detail.RefreshedAtUtc = now; detail.RefreshedBy = actor; detail.UpdatedAtUtc = now; detail.UpdatedBy = actor;
            summary.AttendanceAllowanceAmount = detail.AllowanceAmount; summary.UpdatedAtUtc = now; summary.UpdatedBy = actor; updated++;
        }
        return new(month, year, summaries.Count, updated, skipped, request.PayrollAllowanceSummaryRecordId);
    }
}
