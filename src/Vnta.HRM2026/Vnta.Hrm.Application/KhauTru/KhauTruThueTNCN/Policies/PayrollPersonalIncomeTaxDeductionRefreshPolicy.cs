namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public enum PayrollPersonalIncomeTaxDeductionSynchronizationDecision
{
    UpdateSummary = 1,
    Unchanged = 2,
    SkippedLocked = 3
}

/// <summary>Xác định kết quả đồng bộ detail Thuế TNCN sang summary, không phụ thuộc EF.</summary>
public sealed class PayrollPersonalIncomeTaxDeductionRefreshPolicy
{
    public PayrollPersonalIncomeTaxDeductionSynchronizationDecision Decide(
        decimal detailDeductionAmount,
        decimal summaryDeductionAmount,
        bool isDetailLocked,
        bool isSummaryLocked) =>
        isDetailLocked || isSummaryLocked
            ? PayrollPersonalIncomeTaxDeductionSynchronizationDecision.SkippedLocked
            : summaryDeductionAmount == detailDeductionAmount
                ? PayrollPersonalIncomeTaxDeductionSynchronizationDecision.Unchanged
                : PayrollPersonalIncomeTaxDeductionSynchronizationDecision.UpdateSummary;
}
