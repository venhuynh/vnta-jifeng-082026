using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Boundary UI hẹp cho workflow đồng bộ và hiển thị khi người dùng nhấn Xem.</summary>
public sealed class PhuCapTrachNhiemGanNhanVienXemDataProvider(
    IPhuCapTrachNhiemGanNhanVienXemService xemService)
{
    public Task<XemPhuCapTrachNhiemGanNhanVienResult> ExecuteAsync(
        XemPhuCapTrachNhiemGanNhanVienRequest request,
        CancellationToken cancellationToken = default) =>
        xemService.ExecuteAsync(request, cancellationToken);
}
