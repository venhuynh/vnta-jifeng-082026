using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;

public sealed class DatabaseMealAllowanceManualAdjustmentService(
    ApplicationDbContext dbContext,
    IMealAllowanceRequestValidator requestValidator)
    : IMealAllowanceManualAdjustmentService
{
    public async Task<MealAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateMealAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        var row = await dbContext.PayrollMealAllowanceRecords
            .SingleOrDefaultAsync(item => item.PayrollAllowanceSummaryRecordId == request.Id, cancellationToken);
        if(row is null)
            throw new KeyNotFoundException("Dòng phụ cấp cơm không còn tồn tại.");
        if(row.IsLocked)
            throw new InvalidOperationException("Dòng phụ cấp cơm đã khóa nên không thể điều chỉnh.");
        if(MealAllowanceCommandSupport.NormalizeConcurrencyTimestamp(row.UpdatedAtUtc)
            != MealAllowanceCommandSupport.NormalizeConcurrencyTimestamp(request.OriginalUpdatedAtUtc))
        {
            throw new MealAllowanceConflictException(
                "Dòng phụ cấp cơm đã được thay đổi. Vui lòng tải lại và thực hiện lại thao tác.");
        }

        var actor = MealAllowanceCommandSupport.ResolveActor(request.Actor);
        var now = MealAllowanceCommandSupport.GetDatabaseNow();
        row.QualifiedMealDays = request.QualifiedMealDays;
        row.MealAllowanceAmount = MealAllowancePolicy.CalculateAllowanceAmount(
            new MealAllowanceAmountInput(row.QualifiedMealDays, row.MealAllowancePerQualifiedDay));
        row.RuleCode = MealAllowancePolicy.ManualAdjustmentRuleCode;
        row.RuleVersion = MealAllowancePolicy.ManualAdjustmentRuleVersion;
        row.Note = MealAllowanceCommandSupport.NormalizeOptional(request.Note);
        row.UpdatedAtUtc = now;
        row.UpdatedBy = actor;

        var summaryRow = await dbContext.PayrollAllowanceSummaryRecords
            .SingleAsync(item => item.Id == row.PayrollAllowanceSummaryRecordId, cancellationToken);
        MealAllowanceCommandSupport.SetSummaryMealAllowanceAmount(summaryRow, row.MealAllowanceAmount, actor, now);
        await MealAllowanceCommandSupport.SaveChangesWithConcurrencyGuardAsync(dbContext, cancellationToken);

        return await MealAllowanceReadProjection
            .BuildFilteredQuery(dbContext, new MealAllowanceFilter(null, null, null, 1))
            .Where(item => item.Result.PayrollAllowanceSummaryRecordId == row.PayrollAllowanceSummaryRecordId)
            .Select(item => MealAllowanceReadProjection.MapToDto(item))
            .SingleAsync(cancellationToken);
    }
}
