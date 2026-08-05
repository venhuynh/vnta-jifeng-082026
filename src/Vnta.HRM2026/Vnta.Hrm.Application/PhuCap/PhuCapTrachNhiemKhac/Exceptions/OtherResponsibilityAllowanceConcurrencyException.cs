namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Exceptions;

public sealed class OtherResponsibilityAllowanceConcurrencyException(string message)
    : InvalidOperationException(message);
