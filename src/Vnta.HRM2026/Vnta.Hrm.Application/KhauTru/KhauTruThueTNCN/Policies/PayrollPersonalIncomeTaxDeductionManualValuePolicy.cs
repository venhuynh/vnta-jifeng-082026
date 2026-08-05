namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

/// <summary>Quy tắc thuần cho điều chỉnh thủ công số tiền khấu trừ Thuế TNCN.</summary>
public sealed class PayrollPersonalIncomeTaxDeductionManualValuePolicy
{
    public decimal ValidateAndNormalize(UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest request)
    {
        if (request.PayrollDeductionSummaryRecordId == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu dòng tổng hợp khấu trừ để điều chỉnh Thuế TNCN.");
        }

        if (request.DeductionAmount < 0)
        {
            throw new InvalidOperationException("Số tiền khấu trừ không được nhỏ hơn 0.");
        }

        var normalizedAmount = decimal.Round(request.DeductionAmount, 2, MidpointRounding.AwayFromZero);
        if (normalizedAmount != request.DeductionAmount)
        {
            throw new InvalidOperationException("Số tiền khấu trừ chỉ được có tối đa 2 chữ số thập phân.");
        }

        if (!request.OriginalUpdatedAtUtc.HasValue)
        {
            throw new PayrollPersonalIncomeTaxDeductionConflictException(
                "Thiếu mốc đối soát cập nhật. Vui lòng tải lại dữ liệu trước khi lưu.");
        }

        return normalizedAmount;
    }
}
