using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Vnta.Hrm.Infrastructure.Identity;

namespace Vnta.Hrm.Web.Components.Account.Pages;

public partial class Login
{
    private const string InvalidLoginMessage = "Lỗi: Tài khoản hoặc mật khẩu không chính xác.";
    private const string AccountNotAllowedMessage = "Lỗi: Tài khoản chưa được phép đăng nhập. Vui lòng liên hệ quản trị viên.";
    private const string LoginServiceUnavailableMessage = "Không thể đăng nhập lúc này. Vui lòng thử lại sau hoặc liên hệ quản trị viên.";

    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private ILogger<Login> Logger { get; set; } = default!;
    [Inject] private IdentityRedirectManager RedirectManager { get; set; } = default!;

    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }

    private string? errorMessage;
    private InputModel? input;

    [SupplyParameterFromForm]
    private InputModel Input
    {
        get => input ??= new();
        set => input = value;
    }

    private async Task LoginUser()
    {
        try
        {
            var account = Input.Account.Trim();
            var user = await ResolveUserAsync(account);
            if (user is null
                || !user.IsActive
                || user.ApprovalStatus != EmployeeAccountApprovalStatus.Approved)
            {
                errorMessage = InvalidLoginMessage;
                return;
            }

            var result = await SignInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                Logger.LogInformation("User logged in.");
                RedirectManager.RedirectToPostLogin(ReturnUrl);
            }
            else if (result.RequiresTwoFactor)
            {
                RedirectManager.RedirectTo(
                    "Account/LoginWith2fa",
                    new()
                    {
                        ["returnUrl"] = RedirectManager.GetPostLoginRedirectUri(ReturnUrl),
                        ["rememberMe"] = Input.RememberMe
                    });
            }
            else if (result.IsLockedOut)
            {
                Logger.LogWarning("User account locked out.");
                RedirectManager.RedirectTo("Account/Lockout");
            }
            else if (result.IsNotAllowed)
            {
                errorMessage = AccountNotAllowedMessage;
            }
            else
            {
                errorMessage = InvalidLoginMessage;
            }
        }
        // NavigationException is Blazor's normal redirect control flow. It must propagate to the
        // framework rather than being translated into an Identity-service error for the user.
        catch (NavigationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Lỗi hạ tầng Identity phải trả lại form để màn chờ phía trình duyệt không bị kẹt vĩnh viễn.
            Logger.LogError(exception, "Không thể xử lý đăng nhập vì dịch vụ Identity không sẵn sàng.");
            errorMessage = LoginServiceUnavailableMessage;
        }
    }

    private async Task<ApplicationUser?> ResolveUserAsync(string account)
    {
        if (string.IsNullOrWhiteSpace(account))
        {
            return null;
        }

        var user = await UserManager.FindByNameAsync(account);
        if (user is not null)
        {
            return user;
        }

        // Tương thích tạm thời với tài khoản đặc biệt dùng email làm username.
        return account.Contains('@')
            ? await UserManager.FindByEmailAsync(account)
            : null;
    }

    private sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tài khoản.")]
        public string Account { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
