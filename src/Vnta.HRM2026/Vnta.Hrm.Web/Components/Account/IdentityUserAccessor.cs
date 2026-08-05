using Vnta.Hrm.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Vnta.Hrm.Web.Components.Account {
    internal sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager) {
        public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context) {
            var user = await userManager.GetUserAsync(context.User);

            if(user is null) {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", "Lỗi: Không thể tải thông tin tài khoản hiện tại.", context);
            }

            return user;
        }
    }
}


