using Vnta.AttendanceGateway.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace Vnta.AttendanceGateway.Logging;

public sealed class AttendanceGatewayRawCommunicationLogger
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AttendanceGatewayRawCommunicationLogger> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly bool _enabled;
    private readonly string _logDirectory;

    public AttendanceGatewayRawCommunicationLogger(
        IOptions<AttendanceGatewayOptions> options,
        IHostEnvironment environment,
        ILogger<AttendanceGatewayRawCommunicationLogger> logger)
    {
        _environment = environment;
        _logger = logger;
        _enabled = options.Value.RawCommunicationLogEnabled;
        _logDirectory = ResolveLogPath(options.Value.RawCommunicationLogPath);
    }

    public Task LogReceiveAsync(string flowId, string connectionId, string payload, CancellationToken cancellationToken)
        => WriteAsync("RECEIVE", flowId, connectionId, payload, cancellationToken);

    public Task LogSendAsync(string flowId, string connectionId, string payload, CancellationToken cancellationToken)
        => WriteAsync("SEND", flowId, connectionId, payload, cancellationToken);

    public Task LogGatewayErrorAsync(string flowId, string connectionId, string payload, CancellationToken cancellationToken)
        => WriteAsync("SYSTEM-ERROR", flowId, connectionId, payload, cancellationToken);

    private async Task WriteAsync(string direction, string flowId, string connectionId, string payload, CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_logDirectory);

            var now = DateTimeOffset.Now;
            var filePath = Path.Combine(_logDirectory, $"vnta-attendance-gateway-raw-{now:yyyyMMdd}.log");
            var text = BuildLogBlock(now, direction, flowId, connectionId, payload);

            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(filePath, text, Encoding.UTF8, cancellationToken);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not write VNTA Attendance Gateway raw communication log. Direction={Direction}, FlowId={FlowId}", direction, flowId);
        }
    }

    private static string BuildLogBlock(DateTimeOffset timestamp, string direction, string flowId, string connectionId, string payload)
    {
        var builder = new StringBuilder();
        builder.Append('[').Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ");
        builder.Append("[INFO] [VNTA-Attendance-Gateway-RAW] [T:").Append(Environment.CurrentManagedThreadId).Append("] ");
        builder.Append('[').Append(direction).Append("] ");
        builder.Append("FlowId=").Append(flowId).Append(", Connection=").Append(connectionId).AppendLine();
        builder.AppendLine("----- BEGIN RAW PAYLOAD -----");
        builder.AppendLine(payload.TrimEnd('\r', '\n'));
        builder.AppendLine("----- END RAW PAYLOAD -----");
        builder.AppendLine();
        return builder.ToString();
    }

    private string ResolveLogPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = "Logs/vnta-attendance-gateway-raw";
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(_environment.ContentRootPath, configuredPath);
    }
}


