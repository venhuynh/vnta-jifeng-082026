using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;

/// <summary>Canonical, read-only SQL projections for leave/holiday allowance snapshots.</summary>
internal static class LeaveHolidayAllowanceReadProjection
{
    public static IQueryable<LeaveHolidayAllowanceListItemDto> CreateItemsForPeriod(
        ApplicationDbContext dbContext,
        int payrollYear,
        int payrollMonth,
        string? searchPattern = null)
    {
        // Keep every filter and sort operation on the mapped entities. Applying them after
        // projecting to a C# record causes Npgsql to reject member access on that record.
        return
            from detail in dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.AsNoTracking()
            join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            where summary.PayrollYear == payrollYear
                && summary.PayrollMonth == payrollMonth
                && (searchPattern == null
                    || (employee != null && employee.EmployeeCode != null && EF.Functions.ILike(employee.EmployeeCode!, searchPattern!))
                    || (employee != null && EF.Functions.ILike(
                        ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
                        searchPattern!))
                    || (employee != null && employee.LastName != null && EF.Functions.ILike(employee.LastName!, searchPattern!))
                    || (employee != null && employee.FirstName != null && EF.Functions.ILike(employee.FirstName!, searchPattern!))
                    || (department != null && department.DepartmentOrWorkshopName != null && EF.Functions.ILike(department.DepartmentOrWorkshopName!, searchPattern!))
                    || (department != null && department.TeamName != null && EF.Functions.ILike(department.TeamName!, searchPattern!))
                    || (department != null && department.GroupName != null && EF.Functions.ILike(department.GroupName!, searchPattern!))
                    || (department != null && department.CenterName != null && EF.Functions.ILike(department.CenterName!, searchPattern!))
                    || (position != null && position.Name != null && EF.Functions.ILike(position.Name!, searchPattern!))
                    || (detail.Note != null && EF.Functions.ILike(detail.Note!, searchPattern!)))
            orderby employee.EmployeeCode ?? string.Empty,
                ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
                detail.PayrollAllowanceSummaryRecordId
            select new LeaveHolidayAllowanceListItemDto(
                detail.PayrollAllowanceSummaryRecordId,
                summary.EmployeeId,
                employee == null ? null : employee.EmployeeCode,
                employee == null ? null : ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
                department == null ? null : (department.DepartmentOrWorkshopName ?? department.TeamName ?? department.GroupName ?? department.CenterName),
                position == null ? null : position.Name,
                summary.PayrollMonth,
                summary.PayrollYear,
                detail.DailyWageAmount,
                detail.LeaveDayCount,
                detail.HolidayDayCount,
                detail.LeaveHolidayAllowanceAmount,
                detail.Note,
                summary.IsLocked,
                detail.CreatedAtUtc,
                detail.CreatedBy,
                summary.UpdatedAtUtc ?? detail.UpdatedAtUtc ?? detail.CreatedAtUtc,
                summary.UpdatedBy ?? detail.UpdatedBy,
                detail.UpdatedAtUtc ?? detail.CreatedAtUtc);
    }

    public static IQueryable<LeaveHolidayAllowanceListItemDto> CreateItem(
        ApplicationDbContext dbContext,
        Guid payrollAllowanceSummaryRecordId)
    {
        return
            from detail in dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.AsNoTracking()
            join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            where detail.PayrollAllowanceSummaryRecordId == payrollAllowanceSummaryRecordId
            select new LeaveHolidayAllowanceListItemDto(
                detail.PayrollAllowanceSummaryRecordId,
                summary.EmployeeId,
                employee == null ? null : employee.EmployeeCode,
                employee == null ? null : ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
                department == null ? null : (department.DepartmentOrWorkshopName ?? department.TeamName ?? department.GroupName ?? department.CenterName),
                position == null ? null : position.Name,
                summary.PayrollMonth,
                summary.PayrollYear,
                detail.DailyWageAmount,
                detail.LeaveDayCount,
                detail.HolidayDayCount,
                detail.LeaveHolidayAllowanceAmount,
                detail.Note,
                summary.IsLocked,
                detail.CreatedAtUtc,
                detail.CreatedBy,
                summary.UpdatedAtUtc ?? detail.UpdatedAtUtc ?? detail.CreatedAtUtc,
                summary.UpdatedBy ?? detail.UpdatedBy,
                detail.UpdatedAtUtc ?? detail.CreatedAtUtc);
    }
}
