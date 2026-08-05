using System.Text;
using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Protocol.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.AttendanceGateway.Integration;

public sealed class DeviceCommandPollingService
{
    // Giới hạn kích thước payload trả về cho máy chấm công trong một lần poll.
    private const int MaxBufferCmd = 2 * 1024 * 1024;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeviceCommandPollingService> _logger;

    public DeviceCommandPollingService(IServiceScopeFactory scopeFactory, ILogger<DeviceCommandPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<DeviceCommandDto?> GetNextPendingCommandAsync(
        string deviceSn,
        string? flowId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();

        // Chuẩn hóa serial để việc so khớp với dữ liệu trong DB luôn nhất quán.
        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();

        // Lấy các lệnh chưa từng gửi cho thiết bị này.
        var unsentCommands = await dbContext.DeviceCommands
            .Where(x => x.DeviceSn == normalizedSerial && x.TransTime == null)
            .OrderBy(x => x.CommitTime)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (unsentCommands.Count > 0)
        {
            var payloadBuilder = new StringBuilder();
            var hasChanges = false;
            // Bảng device_cmd đang dùng "timestamp without time zone", nên thời điểm ghi xuống DB
            // phải được chuẩn hóa về Unspecified để Npgsql không từ chối giá trị UTC.
            var transmittedAt = ToDatabaseTimestamp(DateTime.UtcNow);

            foreach (var command in unsentCommands)
            {
                // Attendance Gateway mong đợi payload theo định dạng C:{Id}:{NộiDungLệnh}.
                var currentCommand = $"C:{command.Id}:{command.Content}";
                var currentCommandLength = Encoding.UTF8.GetByteCount(currentCommand);
                var currentPayloadLength = Encoding.UTF8.GetByteCount(payloadBuilder.ToString());

                // Không vượt quá kích thước buffer mà thiết bị có thể nhận trong một lần.
                if ((currentCommandLength + currentPayloadLength) > MaxBufferCmd)
                {
                    break;
                }

                payloadBuilder.AppendLine(currentCommand);

                // Đánh dấu lệnh đã được phát ra để tránh gửi lặp ngay ở lần poll kế tiếp.
                command.TransTime = transmittedAt;
                hasChanges = true;

                // Một số nhóm lệnh chỉ nên gửi đơn lẻ trong một request.
                if (IsSendOneCommand(command.Content))
                {
                    break;
                }
            }

            if (hasChanges)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var payload = payloadBuilder.AppendLine().ToString();
            if (payloadBuilder.Length > 0)
            {
                _logger.LogInformation(
                    "Attendance Gateway FLOW DB [{FlowId}] Loaded outbound command payload. DeviceSn={DeviceSn}, Payload={Payload}",
                    flowId ?? "<none>",
                    normalizedSerial,
                    payload.Trim());
                return new DeviceCommandDto
                {
                    CommandId = "db-poll",
                    Payload = payload
                };
            }
        }

        // Tìm các lệnh đã gửi nhưng chưa có phản hồi từ thiết bị.
        var notRespondedCommands = await dbContext.DeviceCommands
            .Where(x => x.DeviceSn == normalizedSerial && x.ResponseTime == null && x.TransTime != null)
            .OrderBy(x => x.TransTime)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (notRespondedCommands.Count > 0)
        {
            var hasReset = false;
            var now = ToDatabaseTimestamp(DateTime.UtcNow);

            foreach (var command in notRespondedCommands)
            {
                // Nếu lệnh đã phát đi quá lâu mà chưa có phản hồi, trả nó về trạng thái pending để poll lại.
                if (command.TransTime.HasValue && now.Subtract(command.TransTime.Value).TotalSeconds >= 30)
                {
                    command.TransTime = null;
                    hasReset = true;
                }

                // Giữ nguyên hành vi cũ: gặp nhóm lệnh đơn thì dừng kiểm tra tiếp.
                if (IsSendOneCommand(command.Content))
                {
                    break;
                }
            }

            if (hasReset)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Attendance Gateway FLOW DB [{FlowId}] Reset stale transmitted commands for device {DeviceSn} back to pending state.",
                    flowId ?? "<none>",
                    normalizedSerial);
            }
        }

        return null;
    }

    private static bool IsSendOneCommand(string? content)
    {
        // Các lệnh này theo đặc tính giao thức thường được gửi riêng lẻ thay vì gộp nhiều dòng.
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return content.Contains("QUERY ATTLOG", StringComparison.OrdinalIgnoreCase)
               || content.Contains("CHECK", StringComparison.OrdinalIgnoreCase)
               || content.Contains("LOG", StringComparison.OrdinalIgnoreCase)
               || content.Contains("VERIFY", StringComparison.OrdinalIgnoreCase)
               || content.Contains("INFO", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime ToDatabaseTimestamp(DateTime value)
    {
        return VietnamTime.ToVietnamLocalTimestamp(value);
    }
}
