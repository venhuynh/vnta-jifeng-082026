namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public sealed class PayrollPersonalIncomeTaxDeductionConflictException(string message)
    : InvalidOperationException(message);
