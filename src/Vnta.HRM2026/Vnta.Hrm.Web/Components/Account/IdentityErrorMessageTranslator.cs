using Microsoft.AspNetCore.Identity;

namespace Vnta.Hrm.Web.Components.Account {
    internal static class IdentityErrorMessageTranslator {
        public static string Format(IEnumerable<IdentityError> errors) => string.Join(
            " ",
            errors.Select(error => error.Code switch {
                "PasswordMismatch" => "Mật khẩu hiện tại không chính xác.",
                "PasswordTooShort" => "Mật khẩu chưa đủ độ dài tối thiểu.",
                "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất một ký tự đặc biệt.",
                "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
                "PasswordRequiresLower" => "Mật khẩu phải có ít nhất một chữ cái thường.",
                "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất một chữ cái in hoa.",
                "PasswordRequiresUniqueChars" => "Mật khẩu chưa có đủ số ký tự khác nhau theo chính sách.",
                _ => "Không thể hoàn tất thao tác. Vui lòng kiểm tra lại thông tin và thử lại."
            }));
    }
}
