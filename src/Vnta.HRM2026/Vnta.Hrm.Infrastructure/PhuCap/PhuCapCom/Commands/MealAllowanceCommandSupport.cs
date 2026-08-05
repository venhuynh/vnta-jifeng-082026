using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;

internal static class MealAllowanceCommandSupport
{
    internal const string SystemActor = "meal-allowance";

    public static string ResolveActor(string? actor) =>
        string.IsNullOrWhiteSpace(actor) ? SystemActor : actor.Trim();

    public static string? NormalizeOptional(string? value) =>
        MealAllowanceReadProjection.NormalizeOptional(value);

    public static DateTime GetDatabaseNow()
    {
        var now = DateTime.UtcNow.AddHours(7);
        return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Unspecified);
    }

    public static DateTime? NormalizeConcurrencyTimestamp(DateTime? value) =>
        value is not { } timestamp
            ? null
            : new DateTime(timestamp.Ticks - timestamp.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Unspecified);

    public static void SetSummaryMealAllowanceAmount(
        PayrollAllowanceSummaryRecordRow summaryRow,
        decimal mealAllowanceAmount,
        string actor,
        DateTime now)
    {
        if(summaryRow.MealAllowanceAmount == mealAllowanceAmount)
            return;

        summaryRow.MealAllowanceAmount = mealAllowanceAmount;
        summaryRow.UpdatedAtUtc = now;
        summaryRow.UpdatedBy = actor;
    }

    /// <summary>Preserves the feature's SaveChanges transaction and translates EF/database races to its public conflict contract.</summary>
    public static async Task SaveChangesWithConcurrencyGuardAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch(DbUpdateConcurrencyException)
        {
            throw new MealAllowanceConflictException(
                "Dữ liệu phụ cấp cơm đã được thay đổi bởi người dùng khác. Vui lòng tải lại và thực hiện lại thao tác.");
        }
        catch(DbUpdateException ex) when(ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new MealAllowanceConflictException(
                "Dữ liệu phụ cấp cơm đã được thay đổi đồng thời hoặc không còn hợp lệ. Vui lòng tải lại và thực hiện lại thao tác.");
        }
    }
}
