namespace Vnta.Hrm.Application.QuanTri.GiamSatAdms;

public interface IAdmsMonitorReadService
{
    Task<AdmsMonitorSnapshotDto> GetSnapshotAsync(
        int activityLimit,
        int rawLimit);
}
