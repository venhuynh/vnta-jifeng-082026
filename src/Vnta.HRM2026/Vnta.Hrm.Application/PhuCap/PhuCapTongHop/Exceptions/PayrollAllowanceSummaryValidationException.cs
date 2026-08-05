namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Exceptions;

/// <summary>Validation failure for an allowance-summary command; maps to the existing bad-request response.</summary>
public sealed class PayrollAllowanceSummaryValidationException(string message) : InvalidOperationException(message);
