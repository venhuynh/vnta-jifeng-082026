namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>
/// Điều phối use case Xem. Logic đồng bộ và query vẫn thuộc các contract hẹp
/// đã có để bảo toàn toàn bộ rule nghiệp vụ hiện hành.
/// </summary>
public sealed class PhuCapTrachNhiemGanNhanVienXemService(
    IPhuCapTrachNhiemGanNhanVienDongBoService synchronizationService,
    IPhuCapTrachNhiemGanNhanVienQueryService queryService)
    : IPhuCapTrachNhiemGanNhanVienXemService
{
    public async Task<XemPhuCapTrachNhiemGanNhanVienResult> ExecuteAsync(
        XemPhuCapTrachNhiemGanNhanVienRequest request,
        CancellationToken cancellationToken = default)
    {
        var synchronization = await synchronizationService.ExecuteAsync(
            request.Year,
            request.Month,
            cancellationToken);
        var page = await queryService.SearchAsync(
            request.ToQuery(),
            cancellationToken);

        return new XemPhuCapTrachNhiemGanNhanVienResult(synchronization, page);
    }
}
