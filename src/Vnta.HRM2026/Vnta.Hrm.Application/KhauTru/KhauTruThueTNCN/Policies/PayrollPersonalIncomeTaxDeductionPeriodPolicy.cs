namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

/// <summary>Kiểm tra kỳ lương dùng chung cho các use case Thuế TNCN.</summary>
public sealed class PayrollPersonalIncomeTaxDeductionPeriodPolicy
{
    public void Validate(int payrollYear, int payrollMonth)
    {
        if (payrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.");
        }

        if (payrollYear is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Năm kỳ lương không hợp lệ.");
        }
    }
}
