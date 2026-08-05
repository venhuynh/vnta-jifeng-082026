namespace Vnta.Hrm.Application.TinhLuong.BangCongTongHop;

/// <summary>
/// Command riêng để tổng hợp bảng công tháng thành dữ liệu đầu vào tính lương.
/// </summary>
public interface IPayrollMonthlyWorkInputRefreshService
{
    Task<RefreshPayrollMonthlyWorkInputsResult> RefreshAsync(
        RefreshPayrollMonthlyWorkInputsRequest request,
        CancellationToken cancellationToken = default);
}
