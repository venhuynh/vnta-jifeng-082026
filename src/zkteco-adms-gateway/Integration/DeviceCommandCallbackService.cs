using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Domain;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.AttendanceGateway.Integration;

public sealed class DeviceCommandCallbackService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeviceCommandCallbackService> _logger;

    public DeviceCommandCallbackService(IServiceScopeFactory scopeFactory, ILogger<DeviceCommandCallbackService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(string deviceSn, string rawBody, string? flowId, CancellationToken cancellationToken)
    {
        var lines = DeviceCommandCallbackParser.SplitLines(rawBody);
        if (lines.Count == 0)
        {
            return 0;
        }

        var firstLine = DeviceCommandCallbackParser.ParseLine(lines[0]);
        if (firstLine is null)
        {
            _logger.LogWarning("Attendance Gateway FLOW DB [{FlowId}] Could not parse first devicecmd callback line for device {DeviceSn}: {Line}", flowId ?? "<none>", deviceSn, lines[0]);
            return 0;
        }

        if (string.Equals(firstLine.CommandType, "INFO", StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessInfoCallbackAsync(deviceSn, rawBody, lines, firstLine, flowId, cancellationToken);
        }

        return await ProcessMultiLineCallbackAsync(deviceSn, lines, flowId, cancellationToken);
    }

    private async Task<int> ProcessInfoCallbackAsync(
        string deviceSn,
        string rawBody,
        IReadOnlyList<string> lines,
        DeviceCommandCallbackLine firstLine,
        string? flowId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();
        var responseAt = ToDatabaseTimestamp(DateTime.UtcNow);

        var command = await dbContext.DeviceCommands
            .FirstOrDefaultAsync(x => x.Id == firstLine.Id && x.DeviceSn == normalizedSerial, cancellationToken);

        if (command is null)
        {
            _logger.LogWarning("Attendance Gateway FLOW DB [{FlowId}] Could not find INFO command result target in device_cmd. DeviceSn={DeviceSn}, CommandId={CommandId}",
                flowId ?? "<none>", normalizedSerial, firstLine.Id);
            return 0;
        }

        // INFO trả về toàn bộ body thiết bị, nên lưu nguyên khối callback để tiện tra cứu lại sau này.
        command.ReturnValue = rawBody.Trim();
        command.ResponseTime = responseAt;

        var device = await dbContext.Devices
            .FirstOrDefaultAsync(x => x.SerialNumber == normalizedSerial, cancellationToken);

        if (device is not null)
        {
            var infoValues = DeviceCommandCallbackParser.ParseInfoBody(lines.Skip(1));
            ApplyDeviceInfo(device, infoValues, normalizedSerial);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Attendance Gateway FLOW DB [{FlowId}] Processed INFO devicecmd callback. DeviceSn={DeviceSn}, CommandId={CommandId}",
            flowId ?? "<none>", normalizedSerial, firstLine.Id);
        return 1;
    }

    private async Task<int> ProcessMultiLineCallbackAsync(
        string deviceSn,
        IReadOnlyList<string> lines,
        string? flowId,
        CancellationToken cancellationToken)
    {
        var parsedLines = lines
            .Select(DeviceCommandCallbackParser.ParseLine)
            .Where(x => x is not null)
            .Cast<DeviceCommandCallbackLine>()
            .ToList();

        if (parsedLines.Count == 0)
        {
            return 0;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();
        var ids = parsedLines.Select(x => x.Id).Distinct().ToArray();
        var responseAt = ToDatabaseTimestamp(DateTime.UtcNow);

        var commands = await dbContext.DeviceCommands
            .Where(x => x.DeviceSn == normalizedSerial && ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var updatedCount = 0;
        foreach (var parsedLine in parsedLines)
        {
            if (!commands.TryGetValue(parsedLine.Id, out var command))
            {
                continue;
            }

            command.ReturnValue = parsedLine.RawLine.Trim();
            command.ResponseTime = responseAt;
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Attendance Gateway FLOW DB [{FlowId}] Processed {UpdatedCount} non-INFO devicecmd callback line(s) for device {DeviceSn}.",
            flowId ?? "<none>", updatedCount, normalizedSerial);
        return updatedCount;
    }

    private static void ApplyDeviceInfo(ZktecoDevice device, IReadOnlyDictionary<string, string> values, string normalizedSerial)
    {
        device.SerialNumber = normalizedSerial;
        device.UpdatedAtUtc = VietnamTime.Now.DateTime;

        SetString(values, ["IPADDRESS", "IP", "DEVICEIP"], value => device.IpAddress = value);
        SetString(values, ["MACADDRESS", "MAC"], value => device.MacAddress = value);
        SetString(values, ["OEMVENDOR", "VENDORNAME", "VENDOR", "MANUFACTURER"], value => device.VendorName = value);
        SetString(values, ["NAME", "DEVICENAME"], value => device.DeviceModel = value);
        SetString(values, ["DEVFIRMWAREVERSION", "FIRMWAREVERSION", "FWVERSION", "FIRMVERSION"], value => device.FirmwareVersion = value);
        SetString(values, ["DEVFPVERSION", "FINGERPRINTVERSION", "FPVERSION"], value => device.FingerprintVersion = value);
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

    private static DateTime ToDatabaseTimestamp(DateTime value)
    {
        return VietnamTime.ToVietnamLocalTimestamp(value);
    }
}
