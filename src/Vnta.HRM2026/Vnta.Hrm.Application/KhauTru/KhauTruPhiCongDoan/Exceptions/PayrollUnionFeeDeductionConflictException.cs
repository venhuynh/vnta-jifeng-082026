namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

/// <summary>Báo hiệu dòng phí công đoàn đã thay đổi hoặc bị khóa sau khi người dùng mở form.</summary>
public sealed class PayrollUnionFeeDeductionConflictException(string message)
    : InvalidOperationException(message);
