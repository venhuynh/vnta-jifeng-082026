using System.Text;

namespace Vnta.AttendanceGateway.Protocol;

public static class ZktecoResponseBuilder
{
    /// <summary>
    /// Tạo ra một chuỗi HTTP 1.1 nguyên thủy đúng định dạng mà Firmware AttendanceGateway bắt buộc yêu cầu.
    /// AttendanceGateway là một client rất khắt khe về format HTTP Header và khoảng trắng.
    /// </summary>
    public static byte[] BuildOkResponse(int count = 0)
    {
        // AttendanceGateway firmware requires body string to simply represent OK
        var content = count > 0 ? $"OK: {count}\n" : "OK\n";
        return BuildHttpResponse(content);
    }
    
    public static byte[] BuildHttpResponse(string bodyContent, string statusCode = "200 OK")
    {
        var builder = new StringBuilder();
        builder.AppendLine($"HTTP/1.1 {statusCode}");
        builder.AppendLine("content-Type: text/plain; charset=UTF-8");
        builder.AppendLine($"content-Length: {Encoding.UTF8.GetByteCount(bodyContent)}");
        builder.AppendLine("content-encoding: UTF-8");
        builder.AppendLine("Date: " + DateTime.UtcNow.ToString("R")); // RFC 1123 format
        builder.AppendLine("connection: close");
        builder.AppendLine(); // Empty line between headers and body is strictly required by HTTP Protocol
        
        if (!string.IsNullOrEmpty(bodyContent))
            builder.Append(bodyContent);
        
        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}
