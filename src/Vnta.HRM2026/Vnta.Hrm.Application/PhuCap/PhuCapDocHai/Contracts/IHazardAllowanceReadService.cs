namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Contract chỉ đọc của bounded context Phụ cấp độc hại.
/// </summary>
public interface IHazardAllowanceReadService
{
    /// <summary>Giữ endpoint danh sách cũ hoạt động cho consumer chưa chuyển sang phân trang.</summary>
    Task<IReadOnlyList<HazardAllowanceListItemDto>> SearchAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Đọc trang dữ liệu và total count theo filter server-side.</summary>
    Task<HazardAllowancePageDto> SearchPageAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Đếm các badge trên toàn bộ tập filter, không phụ thuộc trang đang xem.</summary>
    Task<HazardAllowanceSummaryDto> GetSummaryAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default);

}
