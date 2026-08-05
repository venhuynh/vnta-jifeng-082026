using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>Feature-local EF helpers shared by command use cases; it contains no use-case orchestration.</summary>
internal static class HazardAllowancePersistence
{
    public static async Task SaveChangesWithConcurrencyGuardAsync(
        this DbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new HazardAllowanceConflictException(
                "Dữ liệu phụ cấp độc hại đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại và thực hiện lại thao tác.");
        }
    }

    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static DateTime ToDatabaseTimestamp(DateTime value) =>
        PostgreSqlTimestamp.ToTimestampWithoutTimeZone(value);

    public static string BuildDepartmentPath(AttendanceDepartmentRow department) =>
        string.Join(" / ", new[]
        {
            NormalizeOptional(department.CenterName),
            NormalizeOptional(department.DepartmentOrWorkshopName),
            NormalizeOptional(department.TeamName),
            NormalizeOptional(department.GroupName)
        }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    public static HazardAllowanceListItemDto MapToDto(
        PayrollAllowanceSummaryRecordRow summary,
        PayrollHazardAllowanceRecordRow detail,
        AttendanceGatewayEmployeeRow? employee,
        string? departmentName = null,
        string? positionName = null) =>
        new(
            summary.Id, summary.EmployeeId, NormalizeOptional(employee?.EmployeeCode), BuildEmployeeName(employee),
            summary.PayrollMonth, summary.PayrollYear, detail.QualifiedWorkdayCount, detail.LateEarlyDeductionDays,
            detail.PayableWorkdayCount, detail.HazardAllowancePerDay, detail.HazardAllowanceAmount,
            detail.IsEligibleDepartment, detail.ExclusionReason, detail.IsLocked || summary.IsLocked, detail.CreatedAtUtc,
            detail.CreatedBy, detail.UpdatedAtUtc, detail.UpdatedBy, summary.UpdatedAtUtc)
        {
            IsEligibleForAllowance = detail.IsEligibleForAllowance,
            DepartmentName = NormalizeOptional(departmentName),
            PositionName = NormalizeOptional(positionName)
        };

    public static bool ApplyDetailSnapshot(
        PayrollHazardAllowanceRecordRow detail,
        HazardAllowanceCalculationResult snapshot,
        DateTime updatedAtUtc,
        string updatedBy)
    {
        if (detail.QualifiedWorkdayCount == snapshot.QualifiedWorkdayCount
            && detail.LateEarlyDeductionDays == snapshot.LateEarlyDeductionDays
            && detail.PayableWorkdayCount == snapshot.PayableWorkdayCount
            && detail.HazardAllowancePerDay == snapshot.HazardAllowancePerDay
            && detail.HazardAllowanceAmount == snapshot.HazardAllowanceAmount
            && detail.IsEligibleDepartment == snapshot.IsEligibleDepartment
            && detail.IsEligibleForAllowance == snapshot.IsEligibleForAllowance
            && string.Equals(detail.ExclusionReason, snapshot.ExclusionReason, StringComparison.Ordinal))
            return false;

        ApplySnapshot(detail, snapshot);
        detail.UpdatedAtUtc = updatedAtUtc;
        detail.UpdatedBy = updatedBy;
        return true;
    }

    public static void ApplySnapshot(PayrollHazardAllowanceRecordRow detail, HazardAllowanceCalculationResult snapshot)
    {
        detail.QualifiedWorkdayCount = snapshot.QualifiedWorkdayCount;
        detail.LateEarlyDeductionDays = snapshot.LateEarlyDeductionDays;
        detail.PayableWorkdayCount = snapshot.PayableWorkdayCount;
        detail.HazardAllowancePerDay = snapshot.HazardAllowancePerDay;
        detail.HazardAllowanceAmount = snapshot.HazardAllowanceAmount;
        detail.IsEligibleDepartment = snapshot.IsEligibleDepartment;
        detail.IsEligibleForAllowance = snapshot.IsEligibleForAllowance;
        detail.ExclusionReason = snapshot.ExclusionReason;
    }

    private static string? BuildEmployeeName(AttendanceGatewayEmployeeRow? employee) => employee is null
        ? null
        : string.Join(" ", new[] { employee.LastName, employee.FirstName }
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim()));
}
