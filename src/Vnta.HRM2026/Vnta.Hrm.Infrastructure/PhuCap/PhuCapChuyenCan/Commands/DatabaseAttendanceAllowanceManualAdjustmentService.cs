using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>
/// Owns the atomic workday-adjustment aggregate update.
/// Legacy single-field methods remain compatibility adapters; new callers use
/// <see cref="UpdateWorkdaysAsync"/> so both values share one transaction and version.
/// </summary>
public sealed class DatabaseAttendanceAllowanceManualAdjustmentService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    AttendanceAllowanceCalculationPolicy calculationPolicy,
    IAttendanceAllowanceManualAdjustmentRequestValidator requestValidator,
    AttendanceAllowanceWorkdayAdjustmentPolicy workdayAdjustmentPolicy)
    : IAttendanceAllowanceManualAdjustmentService, IAttendanceAllowanceWorkdayAdjustmentService
{
    public Task<AttendanceAllowanceResultListItemDto> UpdateActualWorkdayAsync(UpdateAttendanceAllowanceActualWorkdayRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        return UpdateAsync(request.Id, request.OriginalUpdatedAtUtc, request.ActualWorkdayCount, null, cancellationToken);
    }

    public Task<AttendanceAllowanceResultListItemDto> UpdateStandardWorkdayAsync(UpdateAttendanceAllowanceStandardWorkdayRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        return UpdateAsync(request.Id, request.OriginalUpdatedAtUtc, null, request.StandardWorkdayCount, cancellationToken);
    }

    public Task<AttendanceAllowanceResultListItemDto> UpdateWorkdaysAsync(
        UpdateAttendanceAllowanceWorkdaysRequest request,
        CancellationToken cancellationToken = default)
    {
        workdayAdjustmentPolicy.Validate(request).ThrowIfInvalid();
        return UpdateAsync(
            request.Id,
            request.OriginalUpdatedAtUtc,
            request.ActualWorkdayCount,
            request.StandardWorkdayCount,
            cancellationToken);
    }

    private async Task<AttendanceAllowanceResultListItemDto> UpdateAsync(Guid id, DateTime? originalUpdatedAtUtc, decimal? actual, decimal? standard, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
        try
        {
            var detail = await dbContext.PayrollAttendanceAllowanceRecords.SingleOrDefaultAsync(x => x.PayrollAllowanceSummaryRecordId == id, token)
                ?? throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Không tìm thấy dòng phụ cấp chuyên cần để cập nhật.");
            var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleOrDefaultAsync(x => x.Id == id, token)
                ?? throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Không tìm thấy kỳ lương hiện tại của dòng phụ cấp chuyên cần.");
            if(!standard.HasValue && actual.HasValue && (actual.Value < 0 || actual.Value > detail.StandardWorkdayCount))
                throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Validation, "Số ngày công thực tế phải từ 0 đến số ngày công chuẩn của kỳ lương.");
            if(!actual.HasValue && standard.HasValue && detail.ActualWorkdayCount > standard.Value)
                throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Validation, "Số ngày công chuẩn không được nhỏ hơn số ngày công thực tế.");
            if(detail.IsLocked || summary.IsLocked)
                throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Locked, "Dòng hoặc kỳ lương phụ cấp chuyên cần đã khóa, không thể điều chỉnh.");

            var now = AttendanceAllowanceCommandSupport.ToDatabaseTimestamp(DateTime.UtcNow);
            var actor = AttendanceAllowanceCommandSupport.CurrentActorId(auditScope);
            var claimedDetail = await dbContext.PayrollAttendanceAllowanceRecords
                .Where(x => x.PayrollAllowanceSummaryRecordId == id && x.UpdatedAtUtc == originalUpdatedAtUtc && !x.IsLocked)
                .Where(x => dbContext.PayrollAllowanceSummaryRecords.Any(s => s.Id == x.PayrollAllowanceSummaryRecordId && !s.IsLocked))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.UpdatedBy, actor), token);
            if(claimedDetail != 1) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Dòng phụ cấp chuyên cần đã được cập nhật hoặc khóa bởi phiên khác. Hãy tải lại dữ liệu trước khi lưu.");
            var claimedSummary = await dbContext.PayrollAllowanceSummaryRecords.Where(x => x.Id == id && !x.IsLocked)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.UpdatedBy, actor), token);
            if(claimedSummary != 1) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Kỳ lương phụ cấp chuyên cần đã được khóa hoặc cập nhật bởi phiên khác. Hãy tải lại dữ liệu trước khi lưu.");

            await AttendanceAllowanceCommandSupport.ReloadClaimedRowsAsync(dbContext, detail, summary, token);
            var targetActual = actual ?? detail.ActualWorkdayCount;
            var targetStandard = standard ?? detail.StandardWorkdayCount;
            workdayAdjustmentPolicy.Validate(new UpdateAttendanceAllowanceWorkdaysRequest(
                id,
                targetActual,
                targetStandard,
                originalUpdatedAtUtc)).ThrowIfInvalid();
            var snapshot = calculationPolicy.Calculate(new AttendanceAllowanceCalculationInput(targetStandard, targetActual, null, AttendanceAllowanceCommandSupport.ToKpViolationState(detail.HasKpViolation)));
            detail.StandardWorkdayCount = targetStandard; detail.ActualWorkdayCount = targetActual;
            detail.AttendanceRate = snapshot.AttendanceRate; detail.AllowanceAmount = snapshot.ActualAllowanceAmount;
            detail.AppliedRuleKey = snapshot.AppliedRule.ToStorageValue(); detail.AttendanceClass = snapshot.AttendanceClass.ToStorageValue();
            detail.CtlWorkdayCount = null; detail.LateEarlyMinutes = null; detail.Kqcc = snapshot.MissingWorkdayCount;
            detail.RefreshedAtUtc = now; detail.RefreshedBy = actor; detail.UpdatedAtUtc = now; detail.UpdatedBy = actor;
            summary.AttendanceAllowanceAmount = detail.AllowanceAmount; summary.UpdatedAtUtc = now; summary.UpdatedBy = actor;
            await dbContext.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch(DbUpdateConcurrencyException)
        {
            throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Dữ liệu phụ cấp chuyên cần đã thay đổi trong khi lưu. Hãy tải lại và thực hiện lại thao tác.");
        }
        return await AttendanceAllowanceCommandSupport.GetByIdAsync(dbContext, id, token)
            ?? throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Không thể tải lại dòng phụ cấp chuyên cần vừa lưu.");
    }
}
