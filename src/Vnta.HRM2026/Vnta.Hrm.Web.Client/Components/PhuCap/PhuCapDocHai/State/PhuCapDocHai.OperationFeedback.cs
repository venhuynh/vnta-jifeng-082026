using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Chuẩn hóa phản hồi lỗi thao tác để người dùng biết nguyên nhân và bước xử lý tiếp theo.</summary>
public partial class PhuCapDocHai
{
    private void ShowOperationFailure(Exception exception, string action)
    {
        var message = GetOperationFailureMessage(exception, action);
        if (exception is HazardAllowanceConflictException or HrmApiException or InvalidOperationException)
        {
            ToastService.ShowWarning(message);
            return;
        }

        ToastService.ShowError(message);
    }

    private static string GetOperationFailureMessage(Exception exception, string action) => exception switch
    {
        HazardAllowanceConflictException =>
            "Dữ liệu phụ cấp độc hại vừa được thay đổi hoặc khóa bởi người dùng khác. Nhấn Xem để tải lại, rồi thực hiện lại thao tác.",
        HrmApiException { Kind: HrmApiErrorKind.Conflict } =>
            "Dữ liệu phụ cấp độc hại vừa được thay đổi hoặc khóa bởi người dùng khác. Nhấn Xem để tải lại, rồi thực hiện lại thao tác.",
        HrmApiException { Kind: HrmApiErrorKind.Unauthenticated } =>
            "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại rồi thực hiện thao tác.",
        HrmApiException { Kind: HrmApiErrorKind.Forbidden } =>
            "Bạn không có quyền thực hiện thao tác này. Liên hệ quản trị viên nếu cần hỗ trợ.",
        HrmApiException { Kind: HrmApiErrorKind.RateLimited } =>
            "Thao tác đang được gửi quá nhanh. Vui lòng chờ một lát rồi thử lại.",
        HrmApiException { Kind: HrmApiErrorKind.Unavailable } =>
            "Dịch vụ tạm thời chưa sẵn sàng. Vui lòng thử lại sau ít phút.",
        HrmApiException { UserMessage: var message } =>
            $"{message} Kiểm tra lại dữ liệu rồi thử lại.",
        InvalidOperationException { Message: var message } when message.Contains("đã khóa", StringComparison.OrdinalIgnoreCase) =>
            $"{message} Hãy mở khóa kỳ lương hoặc chọn dữ liệu chưa khóa rồi thử lại.",
        InvalidOperationException { Message: var message } =>
            $"{message} Kiểm tra lại dữ liệu rồi thử lại.",
        _ =>
            $"Không thể {action} phụ cấp độc hại. Vui lòng thử lại; nếu sự cố tiếp diễn, liên hệ quản trị viên."
    };
}
