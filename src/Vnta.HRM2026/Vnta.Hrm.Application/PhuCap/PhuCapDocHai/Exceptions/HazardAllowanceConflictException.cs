namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Báo hiệu một command Phụ cấp độc hại đụng dữ liệu vừa được thao tác ở phiên khác.
/// HTTP boundary phải map exception này thành 409 Conflict, không trả lỗi EF thô về client.
/// </summary>
public sealed class HazardAllowanceConflictException(string message) : InvalidOperationException(message);
