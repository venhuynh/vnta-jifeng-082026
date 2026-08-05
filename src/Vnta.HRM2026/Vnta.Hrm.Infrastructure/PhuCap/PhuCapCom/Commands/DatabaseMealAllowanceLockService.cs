using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;

public sealed class DatabaseMealAllowanceLockService(
    ApplicationDbContext dbContext,
    IMealAllowanceRequestValidator requestValidator)
    : IMealAllowanceLockService
{
    public async Task<SetMealAllowanceLockStateBatchResult> SetLockStateBatchAsync(
        SetMealAllowanceLockStateBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();

        var ids = request.RecordIds?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? [];
        var query =
            from row in dbContext.PayrollMealAllowanceRecords
            join summary in dbContext.PayrollAllowanceSummaryRecords
                on row.PayrollAllowanceSummaryRecordId equals summary.Id
            where summary.PayrollMonth == request.PayrollMonth && summary.PayrollYear == request.PayrollYear
            select row;

        query = request.Scope switch
        {
            MealAllowanceLockActionScope.SelectedRows when ids.Length > 0 =>
                query.Where(row => ids.Contains(row.PayrollAllowanceSummaryRecordId)),
            MealAllowanceLockActionScope.SelectedRows => throw new InvalidOperationException(
                "Phải chọn ít nhất một dòng phụ cấp cơm để cập nhật trạng thái khóa."),
            MealAllowanceLockActionScope.WholePeriod when ids.Length == 0 => query,
            MealAllowanceLockActionScope.WholePeriod => throw new InvalidOperationException(
                "Thao tác toàn bộ kỳ lương không nhận danh sách dòng cụ thể."),
            _ => throw new InvalidOperationException("Phạm vi cập nhật trạng thái khóa không hợp lệ.")
        };

        var rows = await query.ToListAsync(cancellationToken);
        var actor = MealAllowanceCommandSupport.ResolveActor(request.Actor);
        var now = MealAllowanceCommandSupport.GetDatabaseNow();
        var updatedCount = 0;
        foreach(var row in rows.Where(row => row.IsLocked != request.IsLocked))
        {
            row.IsLocked = request.IsLocked;
            row.UpdatedAtUtc = now;
            row.UpdatedBy = actor;
            updatedCount++;
        }

        if(updatedCount > 0)
            await MealAllowanceCommandSupport.SaveChangesWithConcurrencyGuardAsync(dbContext, cancellationToken);

        return new SetMealAllowanceLockStateBatchResult(
            request.PayrollYear, request.PayrollMonth, rows.Count, updatedCount);
    }
}
