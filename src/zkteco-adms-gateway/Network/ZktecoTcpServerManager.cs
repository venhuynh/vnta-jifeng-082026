using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Vnta.AttendanceGateway.Configuration;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Logging;
using Vnta.AttendanceGateway.Protocol.Routing;
using Microsoft.Extensions.Options;

namespace Vnta.AttendanceGateway.Network;

public class ZktecoTcpServerManager
{
    private readonly ILogger<ZktecoTcpServerManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ZktecoRequestRouter _router;
    private readonly AttendanceGatewayRawCommunicationLogger _rawCommunicationLogger;
    private readonly RealtimeGatewayLogPublisher _realtimeGatewayLogPublisher;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly int _port;
    private readonly ConcurrentDictionary<string, ClientConnection> _activeConnections = new();
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private string? _lastListenerError;
    private DateTimeOffset? _lastListenerErrorAtUtc;
    private DateTimeOffset? _lastStartedAtUtc;

    public ZktecoTcpServerManager(
        ILogger<ZktecoTcpServerManager> logger,
        ILoggerFactory loggerFactory,
        ZktecoRequestRouter router,
        AttendanceGatewayRawCommunicationLogger rawCommunicationLogger,
        RealtimeGatewayLogPublisher realtimeGatewayLogPublisher,
        AdmsActivityPublisher admsActivityPublisher,
        IOptions<AttendanceGatewayOptions> options)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _router = router;
        _rawCommunicationLogger = rawCommunicationLogger;
        _realtimeGatewayLogPublisher = realtimeGatewayLogPublisher;
        _admsActivityPublisher = admsActivityPublisher;
        _port = options.Value.ListenerPort;
    }

    public async Task StartListeningAsync(CancellationToken token = default)
    {
        await _stateLock.WaitAsync(token);
        try
        {
            if (_listener != null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            var listener = new TcpListener(IPAddress.Any, _port);

            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                listener.Stop();
                _cts.Dispose();
                _cts = null;
                _lastListenerError = BuildListenerStartErrorMessage(ex);
                _lastListenerErrorAtUtc = DateTimeOffset.UtcNow;
                _logger.LogError(ex, "Unable to start Attendance Gateway TCP Listener on port {Port}. {ErrorMessage}", _port, _lastListenerError);
                return;
            }

            _listener = listener;
            _lastStartedAtUtc = DateTimeOffset.UtcNow;
            _lastListenerError = null;
            _lastListenerErrorAtUtc = null;

            _logger.LogInformation("Attendance Gateway TCP Listener is ONLINE and listening on Port: {Port}", _port);
            _acceptTask = AcceptClientsAsync(_cts.Token);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var tcpClient = await _listener!.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleNewClientAsync(tcpClient, token), token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _lastListenerError = ex.Message;
            _lastListenerErrorAtUtc = DateTimeOffset.UtcNow;
            _logger.LogError(ex, "TCP Listener loop crashed critically.");
        }
    }

    private async Task HandleNewClientAsync(TcpClient tcpClient, CancellationToken token)
    {
        var clientLogger = _loggerFactory.CreateLogger<ClientConnection>();
        var connection = new ClientConnection(tcpClient, clientLogger, _router, _rawCommunicationLogger, _realtimeGatewayLogPublisher, _admsActivityPublisher);

        _activeConnections.TryAdd(connection.ConnectionId, connection);

        try
        {
            await connection.ProcessAsync(token);
        }
        finally
        {
            _activeConnections.TryRemove(connection.ConnectionId, out _);
            await connection.DisposeAsync();
        }
    }

    public async Task StopListeningAsync(CancellationToken token = default)
    {
        await _stateLock.WaitAsync(token);
        try
        {
            if (_listener == null)
            {
                return;
            }

            _logger.LogInformation("Receiving STOP command. Disconnecting active VNTA devices...");

            if (_cts != null)
            {
                await _cts.CancelAsync();
            }

            _listener.Stop();

            if (_acceptTask != null)
            {
                try
                {
                    await Task.WhenAny(_acceptTask, Task.Delay(3000, token));
                }
                catch
                {
                }
            }

            foreach (var conn in _activeConnections.Values)
            {
                await conn.DisposeAsync();
            }

            _activeConnections.Clear();
            _listener = null;
            _cts?.Dispose();
            _cts = null;

            _logger.LogInformation("TCP Listener went OFFLINE successfully.");
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public object GetStatus()
    {
        return new
        {
            IsRunning = _listener != null,
            State = _listener != null ? "Running" : string.IsNullOrWhiteSpace(_lastListenerError) ? "Stopped" : "StartFailed",
            ActiveConnections = _activeConnections.Count,
            Port = _port,
            LastListenerError = _lastListenerError,
            LastListenerErrorAtUtc = _lastListenerErrorAtUtc,
            LastStartedAtUtc = _lastStartedAtUtc
        };
    }

    private string BuildListenerStartErrorMessage(SocketException ex)
    {
        if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            return $"Port TCP {_port} đang bị chiếm bởi một process khác hoặc một instance ADMS khác.";
        }

        return $"Không thể bind TCP listener trên port {_port}. SocketError={ex.SocketErrorCode}.";
    }
}


