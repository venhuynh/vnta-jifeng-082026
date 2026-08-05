namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;

/// <summary>Điều chỉnh các giá trị được phép sửa của snapshot phụ cấp cơm.</summary>
public sealed record UpdateMealAllowanceManualValuesRequest(
    Guid Id,
    int QualifiedMealDays,
    string? Note,
    DateTime? OriginalUpdatedAtUtc,
    string? Actor = null);
