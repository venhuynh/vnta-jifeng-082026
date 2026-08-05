using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Vnta.Hrm.Infrastructure.Identity;
using Vnta.Hrm.Web.Client;

namespace Vnta.Hrm.Web.Components.Account {
    /// <summary>
    /// Cung cấp trạng thái xác thực ở phía máy chủ và tuần tự hóa thông tin người dùng cần thiết
    /// vào <see cref="PersistentComponentState"/> trước khi giao diện được chuyển sang chế độ
    /// tương tác WebAssembly.
    /// </summary>
    /// <remarks>
    /// Dữ liệu được lưu được phía client đọc thông qua <see cref="UserInfo"/> và được giữ cố định
    /// trong suốt vòng đời của ứng dụng WebAssembly. Vì vậy, lớp này chỉ chuyển các thông tin phục
    /// vụ hiển thị/phân quyền ở client, không thay thế cho việc kiểm tra quyền ở phía máy chủ.
    /// </remarks>
    internal sealed class PersistingServerAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable {
        #region Trường dữ liệu và trạng thái nội bộ

        /// <summary>
        /// Vùng trạng thái bền vững dùng để truyền dữ liệu từ quá trình prerender phía máy chủ
        /// sang ứng dụng WebAssembly sau khi khởi tạo tương tác.
        /// </summary>
        private readonly PersistentComponentState state;

        /// <summary>
        /// Tùy chọn Identity, được dùng để xác định chính xác tên các claim định danh,
        /// thư điện tử, tên người dùng và vai trò.
        /// </summary>
        private readonly IdentityOptions options;

        /// <summary>
        /// Dịch vụ Identity để truy vấn người dùng, vai trò và các claim quyền được cấp trực tiếp.
        /// </summary>
        private readonly UserManager<ApplicationUser> userManager;

        /// <summary>
        /// Đăng ký callback lưu trạng thái; cần được giải phóng khi provider không còn được sử dụng.
        /// </summary>
        private readonly PersistingComponentStateSubscription subscription;

        /// <summary>
        /// Tác vụ trạng thái xác thực mới nhất nhận được từ lớp cơ sở.
        /// Callback lưu trạng thái sẽ chờ tác vụ này để lấy <see cref="AuthenticationState.User"/>.
        /// </summary>
        private Task<AuthenticationState>? authenticationStateTask;

        #endregion

        #region Khởi tạo

        /// <summary>
        /// Khởi tạo provider và đăng ký cơ chế lưu thông tin người dùng khi component state được persist.
        /// </summary>
        /// <param name="persistentComponentState">Đối tượng quản lý trạng thái có thể truyền sang client.</param>
        /// <param name="optionsAccessor">Accessor cung cấp cấu hình claim của ASP.NET Core Identity.</param>
        /// <param name="userManager">Dịch vụ quản lý người dùng của hệ thống.</param>
        public PersistingServerAuthenticationStateProvider(
            PersistentComponentState persistentComponentState,
            IOptions<IdentityOptions> optionsAccessor,
            UserManager<ApplicationUser> userManager) {
            state = persistentComponentState;
            options = optionsAccessor.Value;
            this.userManager = userManager;

            AuthenticationStateChanged += OnAuthenticationStateChanged;
            subscription = state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
        }

        #endregion

        #region Theo dõi trạng thái xác thực

        /// <summary>
        /// Lưu lại tác vụ trạng thái xác thực mới nhất do lớp cơ sở phát ra.
        /// </summary>
        /// <param name="task">Tác vụ bất đồng bộ trả về trạng thái xác thực hiện hành.</param>
        private void OnAuthenticationStateChanged(Task<AuthenticationState> task) {
            authenticationStateTask = task;
        }

        #endregion

        #region Lưu thông tin người dùng cho WebAssembly

        /// <summary>
        /// Tạo và lưu <see cref="UserInfo"/> vào component state trước khi phản hồi prerender kết thúc.
        /// </summary>
        /// <remarks>
        /// Vai trò được chuẩn hóa (loại bỏ giá trị rỗng, cắt khoảng trắng, khử trùng lặp và sắp xếp)
        /// trước khi xác định capability. Quyền trực tiếp được lấy từ Identity khi có bản ghi người dùng;
        /// nếu không, các claim hiện diện trên principal được dùng làm phương án dự phòng.
        /// </remarks>
        private async Task OnPersistingAsync() {
            if(authenticationStateTask is null) {
                throw new UnreachableException($"Authentication state not set in {nameof(OnPersistingAsync)}().");
            }

            var authenticationState = await authenticationStateTask;
            var principal = authenticationState.User;

            if(principal.Identity?.IsAuthenticated == true) {
                var userId = principal.FindFirst(options.ClaimsIdentity.UserIdClaimType)?.Value;
                if(string.IsNullOrWhiteSpace(userId)) {
                    return;
                }

                var user = await userManager.GetUserAsync(principal);
                var roles = user is null
                    ? principal.FindAll(options.ClaimsIdentity.RoleClaimType).Select(static claim => claim.Value).ToArray()
                    : (await userManager.GetRolesAsync(user)).ToArray();

                var normalizedRoles = roles
                    .Where(static role => !string.IsNullOrWhiteSpace(role))
                    .Select(static role => role.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var explicitClaims = user is null
                    ? principal.FindAll(InternalAccountClaimTypes.Permission).Select(static claim => claim.Value).ToArray()
                    : (await userManager.GetClaimsAsync(user))
                        .Where(static claim => claim.Type == InternalAccountClaimTypes.Permission)
                        .Select(static claim => claim.Value)
                        .ToArray();
                var normalizedPermissions = InternalAccountCapabilityResolver.ResolveCapabilities(normalizedRoles, explicitClaims);

                string email = user?.Email
                    ?? principal.FindFirst(options.ClaimsIdentity.EmailClaimType)?.Value
                    ?? string.Empty;
                string name = user?.UserName
                    ?? principal.FindFirst(options.ClaimsIdentity.UserNameClaimType)?.Value
                    ?? email
                    ?? userId;

                state.PersistAsJson(nameof(UserInfo), new UserInfo {
                    UserId = userId,
                    Email = email ?? string.Empty,
                    Name = name ?? userId,
                    Role = normalizedRoles.FirstOrDefault(),
                    Roles = normalizedRoles,
                    Permissions = normalizedPermissions,
                    EmployeeId = user?.EmployeeId?.ToString("D"),
                    AccessLevel = user?.AccessLevel,
                    ApprovalStatus = user?.ApprovalStatus.ToString(),
                    IsActive = user?.IsActive ?? true
                });
            }
        }

        #endregion

        #region Giải phóng tài nguyên

        /// <summary>
        /// Hủy đăng ký callback và sự kiện để tránh giữ tham chiếu đến provider sau khi nó bị giải phóng.
        /// </summary>
        public void Dispose() {
            subscription.Dispose();
            AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        #endregion
    }
}

