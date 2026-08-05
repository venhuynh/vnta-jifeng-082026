using System.Net;

namespace Vnta.AttendanceGateway.Protocol.Parsers;

public static class HeaderParser
{
    public static string ExtractQueryParam(string url, string paramName)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var questionMarkIndex = url.IndexOf('?');
        if (questionMarkIndex == -1 || questionMarkIndex == url.Length - 1)
        {
            return string.Empty;
        }

        var queryString = url[(questionMarkIndex + 1)..];
        var parameters = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var param in parameters)
        {
            var kvp = param.Split('=', 2);
            if (kvp.Length == 2 && kvp[0].Equals(paramName, StringComparison.OrdinalIgnoreCase))
            {
                return WebUtility.UrlDecode(kvp[1]);
            }
        }

        return string.Empty;
    }
}
