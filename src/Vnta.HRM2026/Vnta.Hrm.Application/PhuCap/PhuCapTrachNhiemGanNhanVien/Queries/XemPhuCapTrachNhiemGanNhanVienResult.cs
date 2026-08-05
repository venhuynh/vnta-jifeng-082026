using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Kết quả đồng bộ và dữ liệu hiển thị của use case Xem.</summary>
public sealed record XemPhuCapTrachNhiemGanNhanVienResult(
    PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult Synchronization,
    PayrollResponsibilityAllowanceEmployeeAssignmentPageDto Page);
