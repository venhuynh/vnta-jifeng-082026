using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models.Security;

public sealed class OpenEmployeeAccountFormModel
{
    public Guid EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu tạm không được để trống.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tạm phải có ít nhất 6 ký tự.")]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vai trò không được để trống.")]
    public string RoleName { get; set; } = "Employee";

    public string? AccessLevel { get; set; }
}
