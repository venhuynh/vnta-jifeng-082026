namespace Vnta.Hrm.Application.Integrations.AttendanceGateway;

public interface IAdmsDeviceCommandService
{
    Task<AdmsDeviceCommandLookupOptionsDto> GetLookupOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmsDeviceCommandSummaryDto>> SearchAsync(
        AdmsDeviceCommandFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdmsDeviceCommandDetailDto?> GetDetailAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<AdmsDeviceInfoResponseDto?> GetLatestInfoResponseAsync(
        string deviceSn,
        CancellationToken cancellationToken = default);

    Task<AdmsDeviceCommandDetailDto> CreateAsync(
        UpsertAdmsDeviceCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmsDeviceCommandDetailDto> UpdateAsync(
        int id,
        UpsertAdmsDeviceCommandRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task DeleteAllAsync(
        CancellationToken cancellationToken = default);
}
