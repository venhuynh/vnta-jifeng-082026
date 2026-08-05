namespace Vnta.Hrm.Application.QuanTri.GiamSatAdms;

public interface IAdmsMonitorRuntimeState
{
    bool HasActiveSessions { get; }

    int ActivateSession();

    int DeactivateSession();
}
