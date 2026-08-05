using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

public sealed class DatabaseHazardAllowanceManualAdjustmentService(
    ApplicationDbContext dbContext,
    IHazardAllowanceManualAdjustmentPolicy manualAdjustmentPolicy,
    IHazardAllowanceRequestValidator requestValidator)
    : IHazardAllowanceManualAdjustmentService
{
    public async Task<HazardAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateHazardAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();

        var values = manualAdjustmentPolicy.ValidateAndNormalize(new HazardAllowanceManualAdjustmentInput(
            request.QualifiedWorkdayCount, request.LateEarlyDeductionDays, request.HazardAllowancePerDay,
            request.HazardAllowanceAmount, request.IsEligibleDepartment, request.ExclusionReason));
        if (dbContext.ChangeTracker.HasChanges()) dbContext.ChangeTracker.Clear();

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleOrDefaultAsync(
            row => row.PayrollAllowanceSummaryRecordId == request.PayrollAllowanceSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng phụ cấp độc hại cần điều chỉnh.");
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleOrDefaultAsync(
            row => row.Id == request.PayrollAllowanceSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng hợp phụ cấp liên quan.");
        if (detail.IsLocked || summary.IsLocked)
            throw new InvalidOperationException("Dòng phụ cấp độc hại đã khóa, không thể điều chỉnh.");
        if ((detail.UpdatedAtUtc ?? detail.CreatedAtUtc) != request.OriginalDetailUpdatedAtUtc
            || (summary.UpdatedAtUtc ?? summary.CreatedAtUtc) != request.OriginalSummaryUpdatedAtUtc)
            throw new HazardAllowanceConflictException("Dòng phụ cấp độc hại đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");

        var now = HazardAllowancePersistence.ToDatabaseTimestamp(DateTime.UtcNow);
        var actor = HazardAllowancePersistence.NormalizeOptional(request.RequestedBy) ?? "system";
        HazardAllowancePersistence.ApplySnapshot(detail, new HazardAllowanceCalculationResult(
            values.QualifiedWorkdayCount, values.LateEarlyDeductionDays, values.PayableWorkdayCount,
            values.HazardAllowancePerDay, values.HazardAllowanceAmount, values.IsEligibleDepartment, values.ExclusionReason,
            values.IsEligibleDepartment));
        detail.UpdatedAtUtc = now;
        detail.UpdatedBy = actor;
        summary.HazardAllowanceAmount = detail.HazardAllowanceAmount;
        summary.UpdatedAtUtc = now;
        summary.UpdatedBy = actor;
        await dbContext.SaveChangesWithConcurrencyGuardAsync(cancellationToken);

        var employee = await dbContext.Employees.AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == summary.EmployeeId, cancellationToken);
        var department = employee is null
            ? null
            : await dbContext.Departments.AsNoTracking()
                .Where(row => row.Id == employee.DepartmentId)
                .SingleOrDefaultAsync(cancellationToken);
        var departmentName = department is null ? null : HazardAllowancePersistence.BuildDepartmentPath(department);
        var positionName = employee is null
            ? null
            : await dbContext.Positions.AsNoTracking()
                .Where(row => row.Id == employee.PositionId)
                .Select(row => row.Name)
                .SingleOrDefaultAsync(cancellationToken);
        return HazardAllowancePersistence.MapToDto(summary, detail, employee, departmentName, positionName);
    }
}
