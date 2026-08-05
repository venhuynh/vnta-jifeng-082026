using System.Threading;
using System.Threading.Tasks;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

/// <summary>
/// Defines the standard contract for handling various Attendance Gateway HTTP-like endpoints.
/// </summary>
public interface IRequestHandler
{
    bool RequiresDeviceAuthorization { get; }

    /// <summary>
    /// Checks if this handler is capable of serving the specific combination of URL pattern and Method.
    /// </summary>
    bool CanHandle(string method, string url);

    /// <summary>
    /// Processes the extracted HTTP request body and its parameters, executing the intended side-effects
    /// (e.g. storing to DB via Core API, fetching commands) and generating a byte array 
    /// formatted correctly to send back down the Pipeline stream.
    /// </summary>
    /// <param name="method">The HTTP verb (GET/POST)</param>
    /// <param name="url">The requested path including Query strings, e.g. /iclock/cdata?SN=123</param>
    /// <param name="bodyRawText">The raw payload of the HTTP body (if any)</param>
    /// <param name="connectionId">The ID of the socket connection, useful for context logging</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A UTF-8 encoded byte array representing the raw HTTP Response for the device</returns>
    Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default);
}

