namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

/// <summary>
/// Cung cấp dữ liệu chỉ đọc cho màn hình Phụ cấp Phép - Lễ.
/// Contract này không cho phép consumer thực hiện thay đổi nghiệp vụ.
/// </summary>
public interface ILeaveHolidayAllowanceReadService
{
    Task<IReadOnlyList<LeaveHolidayAllowanceListItemDto>> SearchAsync(
        LeaveHolidayAllowanceFilter filter,
        CancellationToken cancellationToken = default);
}
