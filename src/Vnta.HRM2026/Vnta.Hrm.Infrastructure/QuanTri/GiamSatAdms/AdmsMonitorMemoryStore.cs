using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.QuanTri.GiamSatAdms;

public sealed class AdmsMonitorMemoryStore
{
    private const string AuthorizationDeviceNotFoundEventType = "authorization-device-not-found";
    private const int MaxActivityEvents = 120;
    private const int MaxRawEvents = 120;
    private const int MaxRecentEventIds = 512;

    private readonly object syncRoot = new();
    private readonly Dictionary<string, DeviceStateSnapshot> deviceStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AttendanceGatewayRealtimeEventDto> activityEvents = [];
    private readonly List<AttendanceGatewayRealtimeEventDto> rawEvents = [];
    private readonly HashSet<string> recentEventIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> recentEventOrder = new();
    private DateTimeOffset? lastReceivedAtUtc;

    public RealtimeEventStoreResult TryStoreRealtimeEvent(AttendanceGatewayRealtimeEventDto eventDto)
    {
        lock(syncRoot) {
            var externalEventId = NormalizeOptional(eventDto.Id);
            if(!string.IsNullOrWhiteSpace(externalEventId) && !recentEventIds.Add(externalEventId)) {
                return new RealtimeEventStoreResult(true, false);
            }

            if(!string.IsNullOrWhiteSpace(externalEventId)) {
                recentEventOrder.Enqueue(externalEventId);
                TrimRecentEventIds();
            }

            var shouldStoreInRealtimePanels = ShouldStoreInRealtimePanels(eventDto);
            if(shouldStoreInRealtimePanels) {
                var targetList = eventDto.IsSemantic ? activityEvents : rawEvents;
                targetList.Insert(0, eventDto);
                TrimEvents(targetList, eventDto.IsSemantic ? MaxActivityEvents : MaxRawEvents);
            }

            if(lastReceivedAtUtc is null || eventDto.ReceivedAtUtc > lastReceivedAtUtc.Value) {
                lastReceivedAtUtc = eventDto.ReceivedAtUtc;
            }

            return new RealtimeEventStoreResult(false, shouldStoreInRealtimePanels);
        }
    }

    public AdmsMonitorDeviceStateDto? UpsertDeviceStateFromRealtimeEvent(AttendanceGatewayRealtimeEventDto eventDto)
    {
        var eventType = NormalizeRequired(eventDto.EventType);
        if(!ShouldProjectIntoDeviceState(eventType)) {
            return null;
        }

        var deviceKey = ResolveDeviceKey(eventDto.Sn, eventDto.DeviceName);
        if(string.IsNullOrWhiteSpace(deviceKey)) {
            return null;
        }

        lock(syncRoot) {
            if(!deviceStates.TryGetValue(deviceKey, out var state)) {
                state = new DeviceStateSnapshot { DeviceKey = deviceKey };
                deviceStates[deviceKey] = state;
            }

            var canonicalDeviceSn = NormalizeOptional(eventDto.Sn);
            var canonicalDeviceName = NormalizeOptional(eventDto.DeviceName);
            var canonicalConnectionId = NormalizeOptional(eventDto.ConnectionId);
            var summaryText = NormalizeOptional(eventDto.SummaryText);
            var previousConnectionId = state.LastConnectionId;
            var isConnectionClosed = eventType.Equals("connection-closed", StringComparison.OrdinalIgnoreCase);
            var isConnectionOpened = eventType.Equals("connection-opened", StringComparison.OrdinalIgnoreCase);

            state.DeviceSn = canonicalDeviceSn ?? state.DeviceSn;
            state.DeviceName = canonicalDeviceName ?? state.DeviceName;
            state.LastConnectionId = canonicalConnectionId ?? state.LastConnectionId;
            state.LastEventType = eventType;
            state.LastSummaryText = summaryText ?? state.LastSummaryText;
            state.LastSeenAtUtc = eventDto.ReceivedAtUtc;
            state.IsOnline = !isConnectionClosed;

            if(isConnectionOpened) {
                state.ConnectionOpenedAtUtc = eventDto.ReceivedAtUtc;
            }

            if(!isConnectionClosed
               && state.ConnectionOpenedAtUtc is null
               && !string.Equals(previousConnectionId, canonicalConnectionId, StringComparison.Ordinal)) {
                state.ConnectionOpenedAtUtc = eventDto.ReceivedAtUtc;
            }

            state.ConnectionClosedAtUtc = isConnectionClosed ? eventDto.ReceivedAtUtc : null;

            return ToDeviceStateDto(state);
        }
    }

    public AdmsMonitorSnapshotDto GetSnapshot(int activityLimit, int rawLimit)
    {
        lock(syncRoot) {
            var resolvedActivityLimit = Math.Max(1, activityLimit);
            var resolvedRawLimit = Math.Max(1, rawLimit);

            var devices = deviceStates.Values
                .Select(ToDeviceStateDto)
                .OrderByDescending(static x => x.IsOnline)
                .ThenByDescending(static x => x.LastSeenAtUtc)
                .ThenBy(static x => x.DeviceName ?? x.DeviceSn ?? x.DeviceKey, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new AdmsMonitorSnapshotDto(
                devices,
                activityEvents.Take(resolvedActivityLimit).ToList(),
                rawEvents.Take(resolvedRawLimit).ToList(),
                lastReceivedAtUtc);
        }
    }

    public void Reset()
    {
        lock(syncRoot) {
            deviceStates.Clear();
            activityEvents.Clear();
            rawEvents.Clear();
            recentEventIds.Clear();
            recentEventOrder.Clear();
            lastReceivedAtUtc = null;
        }
    }

    private void TrimRecentEventIds()
    {
        while(recentEventOrder.Count > MaxRecentEventIds) {
            var removedId = recentEventOrder.Dequeue();
            recentEventIds.Remove(removedId);
        }
    }

    private static void TrimEvents(List<AttendanceGatewayRealtimeEventDto> events, int maxCount)
    {
        if(events.Count <= maxCount) {
            return;
        }

        events.RemoveRange(maxCount, events.Count - maxCount);
    }

    private static AdmsMonitorDeviceStateDto ToDeviceStateDto(DeviceStateSnapshot state) =>
        new(
            state.DeviceKey,
            state.DeviceSn,
            state.DeviceName,
            state.LastConnectionId,
            state.LastEventType,
            state.LastSummaryText,
            state.ConnectionOpenedAtUtc,
            state.ConnectionClosedAtUtc,
            state.LastSeenAtUtc,
            state.IsOnline);

    private static string NormalizeRequired(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ResolveDeviceKey(string? deviceSn, string? deviceName)
    {
        var canonicalDeviceSn = NormalizeOptional(deviceSn);
        if(!string.IsNullOrWhiteSpace(canonicalDeviceSn)) {
            return $"sn:{canonicalDeviceSn}";
        }

        var canonicalDeviceName = NormalizeOptional(deviceName);
        if(!string.IsNullOrWhiteSpace(canonicalDeviceName)) {
            return $"name:{canonicalDeviceName}";
        }

        return null;
    }

    private static bool ShouldStoreInRealtimePanels(AttendanceGatewayRealtimeEventDto eventDto)
    {
        if(!eventDto.IsSemantic) {
            return true;
        }

        return !IsDeviceConnectionLifecycleEvent(eventDto.EventType);
    }

    private static bool ShouldProjectIntoDeviceState(string eventType) =>
        IsDeviceConnectionLifecycleEvent(eventType)
        || string.Equals(eventType, AuthorizationDeviceNotFoundEventType, StringComparison.OrdinalIgnoreCase);

    private static bool IsDeviceConnectionLifecycleEvent(string? eventType) =>
        string.Equals(eventType, "connection-opened", StringComparison.OrdinalIgnoreCase)
        || string.Equals(eventType, "connection-closed", StringComparison.OrdinalIgnoreCase);

    private sealed class DeviceStateSnapshot
    {
        public string DeviceKey { get; init; } = string.Empty;

        public string? DeviceSn { get; set; }

        public string? DeviceName { get; set; }

        public string? LastConnectionId { get; set; }

        public string? LastEventType { get; set; }

        public string? LastSummaryText { get; set; }

        public DateTimeOffset? ConnectionOpenedAtUtc { get; set; }

        public DateTimeOffset? ConnectionClosedAtUtc { get; set; }

        public DateTimeOffset LastSeenAtUtc { get; set; }

        public bool IsOnline { get; set; }
    }

    public readonly record struct RealtimeEventStoreResult(
        bool IsDuplicate,
        bool IsBufferedInPanel);
}
