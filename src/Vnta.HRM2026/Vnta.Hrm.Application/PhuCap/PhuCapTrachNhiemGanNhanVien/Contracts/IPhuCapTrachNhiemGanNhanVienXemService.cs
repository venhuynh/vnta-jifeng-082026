namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Use case riêng của nút Xem tại màn gán phụ cấp trách nhiệm theo nhân viên.</summary>
public interface IPhuCapTrachNhiemGanNhanVienXemService
{
    Task<XemPhuCapTrachNhiemGanNhanVienResult> ExecuteAsync(
        XemPhuCapTrachNhiemGanNhanVienRequest request,
        CancellationToken cancellationToken = default);
}
