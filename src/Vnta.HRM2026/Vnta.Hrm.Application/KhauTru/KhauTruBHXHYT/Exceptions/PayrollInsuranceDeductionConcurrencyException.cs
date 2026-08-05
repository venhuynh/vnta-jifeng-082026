namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Bản ghi đã thay đổi sau khi người dùng mở popup điều chỉnh.
/// </summary>
public sealed class PayrollInsuranceDeductionConcurrencyException(string message)
    : InvalidOperationException(message);
