namespace Vnta.Hrm.Web.Client.Services.Adms;

public sealed record AttendanceBiometricDeviceCommandCreateResult(
    int CommandsCreated,
    int MatchedEmployees,
    int DeviceCount);
