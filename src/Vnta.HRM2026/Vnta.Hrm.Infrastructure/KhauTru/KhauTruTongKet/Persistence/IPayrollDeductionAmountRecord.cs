namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

public interface IPayrollDeductionAmountRecord
{
    Guid PayrollDeductionSummaryRecordId { get; set; }

    decimal DeductionAmount { get; set; }

    bool IsLocked { get; set; }

    DateTime CreatedAtUtc { get; set; }

    DateTime? UpdatedAtUtc { get; set; }
}
