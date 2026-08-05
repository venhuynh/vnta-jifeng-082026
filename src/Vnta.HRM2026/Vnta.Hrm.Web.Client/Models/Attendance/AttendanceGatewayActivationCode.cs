namespace Vnta.Hrm.Web.Client.Models;

public static class AttendanceGatewayActivationCode
{
    public const string OptionalActivationCodePattern = AttendanceDeviceActivationCode.OptionalActivationCodePattern;

    public static string Generate(string serialNumber) =>
        AttendanceDeviceActivationCode.Generate(serialNumber);

    public static bool Validate(string serialNumber, string activationCode) =>
        AttendanceDeviceActivationCode.Validate(serialNumber, activationCode);

    public static string NormalizeSerial(string serialNumber) =>
        AttendanceDeviceActivationCode.NormalizeSerial(serialNumber);

    public static string NormalizeActivationCode(string activationCode) =>
        AttendanceDeviceActivationCode.NormalizeActivationCode(activationCode);

    public static bool HasExpectedShape(string? activationCode) =>
        AttendanceDeviceActivationCode.HasExpectedShape(activationCode);
}
