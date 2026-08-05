using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Domain;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.AttendanceGateway.Integration;

public sealed class DeviceOptionsSyncService
{
    private const int FingerprintBioIndex = 1;
    private const int FaceBioIndex = 2;
    private const int FingerVeinBioIndex = 7;
    private const int PalmBioIndex = 8;
    private const int VisilightFaceBioIndex = 9;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeviceOptionsSyncService> _logger;

    public DeviceOptionsSyncService(IServiceScopeFactory scopeFactory, ILogger<DeviceOptionsSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(string deviceSn, string rawBody, string? flowId, CancellationToken cancellationToken)
    {
        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();
        var values = DeviceOptionsParser.Parse(rawBody);

        if (values.Count == 0)
        {
            _logger.LogWarning("Attendance Gateway FLOW DB [{FlowId}] Received empty table=options payload for device {DeviceSn}.", flowId ?? "<none>", normalizedSerial);
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var device = await dbContext.Devices
            .FirstOrDefaultAsync(x => x.SerialNumber == normalizedSerial, cancellationToken);

        if (device is null)
        {
            _logger.LogWarning("Attendance Gateway FLOW DB [{FlowId}] Could not find device by serial for table=options payload. DeviceSn={DeviceSn}", flowId ?? "<none>", normalizedSerial);
            return false;
        }

        ApplyOptions(device, values, normalizedSerial);

        if (string.IsNullOrWhiteSpace(device.MultiBioDataSupport))
        {
            device.MultiBioDataSupport = BuildLegacyMultiBioDataSupport(values);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Attendance Gateway FLOW DB [{FlowId}] Processed table=options payload for device {DeviceSn}.", flowId ?? "<none>", normalizedSerial);
        return true;
    }

    private static void ApplyOptions(ZktecoDevice device, IReadOnlyDictionary<string, string> values, string normalizedSerial)
    {
        device.SerialNumber = normalizedSerial;
        device.UpdatedAtUtc = VietnamTime.Now.DateTime;

        SetString(values, ["IPADDRESS", "IP", "DEVICEIP"], value => device.IpAddress = value);
        SetString(values, ["MACADDRESS", "MAC"], value => device.MacAddress = value);
        SetString(values, ["OEMVENDOR", "VENDORNAME", "VENDOR", "MANUFACTURER"], value => device.VendorName = value);
        SetString(values, ["DEVICEMODEL", "DEVICETYPE", "MODEL"], value => device.DeviceModel = value);
        SetString(values, ["FWVERSION", "DEVFIRMWAREVERSION", "FIRMWAREVERSION", "FIRMVERSION"], value => device.FirmwareVersion = value);
        SetString(values, ["FPVERSION", "DEVFPVERSION", "FINGERPRINTVERSION"], value => device.FingerprintVersion = value);
        SetString(values, ["TIMEZONE"], value => device.TimeZone = value);
        SetString(values, ["ATTLOGSTAMP"], value => device.AttendanceLogStamp = value);
        SetString(values, ["ATTPHOTOSTAMP", "PHOTOSTAMP"], value => device.AttendancePhotoStamp = value);
        SetString(values, ["OPLOGSTAMP", "OPERLOGSTAMP", "OPERATIONLOGSTAMP"], value => device.OperationLogStamp = value);
        SetString(values, ["TRANSFLAG", "TRANSFERFLAG"], value => device.TransferFlag = value);
        SetString(values, ["DELAY"], value => device.Delay = value);
        SetString(values, ["REALTIME"], value => device.Realtime = value);
        SetString(values, ["TRANSINTERVAL"], value => device.TransInterval = value);
        SetString(values, ["TRANSTIMES"], value => device.TransTimes = value);
        SetString(values, ["ENCRYPT"], value => device.Encrypt = value);
        SetString(values, ["ERRORDELAY"], value => device.ErrorDelay = value);
        SetString(values, ["IRTEMPDETECTIONFUNON"], value => device.IrTempDetectionFunOn = value);
        SetString(values, ["MASKDETECTIONFUNON"], value => device.MaskDetectionFunOn = value);
        SetString(values, ["MULTIBIODATASUPPORT"], value => device.MultiBioDataSupport = value);

        SetInt(values, ["USERCOUNT", "USERCNT"], value => device.UserCount = value);
        SetInt(values, ["TRANSACTIONCOUNT", "ATTLOGCOUNT", "LOGCOUNT"], value => device.AttendanceLogCount = value);
        SetInt(values, ["FPCOUNT", "FINGERPRINTCOUNT"], value => device.FingerprintCount = value);
        SetInt(values, ["TIMEOUT"], value => device.Timeout = value);
        SetInt(values, ["SYNCTIME"], value => device.SyncTime = value);
        SetInt(values, ["PORT"], value => device.Port = value);
    }

    private static string BuildLegacyMultiBioDataSupport(IReadOnlyDictionary<string, string> values)
    {
        var result = Enumerable.Repeat("0", 10).ToArray();

        UpdateSupported(values, result, "FINGERFUNON", FingerprintBioIndex);
        UpdateSupported(values, result, "FACEFUNON", FaceBioIndex, VisilightFaceBioIndex);
        UpdateSupported(values, result, "FVFUNON", FingerVeinBioIndex);
        UpdateSupported(values, result, "PVFUNON", PalmBioIndex);
        UpdateSupported(values, result, "VISILIGHTFUN", VisilightFaceBioIndex);

        return string.Join(":", result);
    }

    private static void UpdateSupported(
        IReadOnlyDictionary<string, string> values,
        string[] result,
        string key,
        params int[] indexes)
    {
        if (!values.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue) || rawValue == "0")
        {
            return;
        }

        foreach (var index in indexes)
        {
            if (index >= 0 && index < result.Length)
            {
                result[index] = rawValue;
            }
        }
    }

    private static void SetString(IReadOnlyDictionary<string, string> values, IEnumerable<string> candidateKeys, Action<string> setter)
    {
        foreach (var key in candidateKeys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                setter(value);
                return;
            }
        }
    }

    private static void SetInt(IReadOnlyDictionary<string, string> values, IEnumerable<string> candidateKeys, Action<int> setter)
    {
        foreach (var key in candidateKeys)
        {
            if (values.TryGetValue(key, out var rawValue) && int.TryParse(rawValue, out var value))
            {
                setter(value);
                return;
            }
        }
    }
}
