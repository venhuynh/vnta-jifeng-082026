namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Chuyển tình trạng nhân viên và lưu ngày nghiệp vụ tương ứng.
/// </summary>
public sealed record ChangeEmployeeStatusRequest(
    Guid Id,
    int Status,
    DateTime? SeniorityStartDate,
    DateTime? ResignedDate,
    DateTime? OriginalUpdatedAtUtc);
