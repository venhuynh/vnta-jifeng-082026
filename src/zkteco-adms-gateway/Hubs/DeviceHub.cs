using Microsoft.AspNetCore.SignalR;

namespace Vnta.AttendanceGateway.Hubs;

public class DeviceHub : Hub
{
    // Frontend clients will connect to this hub to receive real-time updates
    // about device connection events (Handshake accepted, rejected, etc).
}
