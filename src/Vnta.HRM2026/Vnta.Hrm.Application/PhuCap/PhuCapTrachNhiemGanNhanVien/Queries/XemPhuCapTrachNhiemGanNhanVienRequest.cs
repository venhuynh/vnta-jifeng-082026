using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>
/// Yêu cầu của use case Xem: đồng bộ tập nhân viên từ Phụ cấp tổng hợp,
/// sau đó trả về trang gán cấp bậc theo bộ lọc đang áp dụng.
/// </summary>
public sealed record XemPhuCapTrachNhiemGanNhanVienRequest(
    int Year,
    int Month,
    string? SearchText,
    string? GradePresenceKey,
    int Skip,
    int Take)
{
    public PayrollResponsibilityAllowanceEmployeeAssignmentQuery ToQuery() =>
        new(Year, Month, SearchText, GradePresenceKey, Skip, Take);
}
