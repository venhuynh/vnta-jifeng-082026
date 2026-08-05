namespace Vnta.Hrm.Application.QuanTri.LenhMayChamCong;

public static class AdmsDeviceCommandStatus
{
    public const string Pending = "pending";
    public const string Transmitted = "transmitted";
    public const string Success = "success";
    public const string Error = "error";

    // Legacy values kept for backward compatibility with older mock-oriented code paths.
    public const string Responded = "responded";
    public const string NoResponse = "no_response";
    public const string Cancelled = "cancelled";

    public static string ToDisplayText(string status)
    {
        return status.ToLowerInvariant() switch
        {
            Pending => "Chưa gửi",
            Transmitted => "Đã gửi",
            Success => "Thành công",
            Error => "Lỗi",
            Responded => "Đã phản hồi",
            NoResponse => "Không phản hồi",
            Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
    }
}
