using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Identity;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.QuanTri.TaiKhoanNhanVien;

public sealed class DatabaseEmployeeAccountService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
    : IEmployeeAccountService
{
    public async Task<IReadOnlyList<EmployeeAccountListItemDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var employeeRows = await (
                from employee in dbContext.Employees.AsNoTracking()
                where !employee.IsDeleted
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                join position in dbContext.Positions.AsNoTracking()
                    on employee.PositionId equals position.Id into positionGroup
                from position in positionGroup.DefaultIfEmpty()
                orderby employee.EmployeeCode, employee.LastName, employee.FirstName, employee.Id
                select new
                {
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FirstName,
                    employee.LastName,
                    EmployeeEmail = employee.Email,
                    DepartmentPath = department == null ? null : BuildDepartmentPath(department),
                    PositionName = position == null ? null : position.Name
                })
            .ToListAsync(cancellationToken);

        var accountRows = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.EmployeeId != null)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.ApprovedAtUtc)
            .ThenBy(x => x.UserName)
            .ToListAsync(cancellationToken);

        var accountByEmployeeId = accountRows
            .Where(x => x.EmployeeId.HasValue)
            .GroupBy(x => x.EmployeeId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.First());

        var userIds = accountByEmployeeId.Values
            .Select(x => x.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var roleLookup = await LoadRoleLookupAsync(userIds, cancellationToken);

        return employeeRows
            .Select(x =>
            {
                var user = accountByEmployeeId.GetValueOrDefault(x.Id);
                return new EmployeeAccountListItemDto(
                    x.Id,
                    x.EmployeeCode,
                    x.FirstName,
                    x.LastName,
                    x.EmployeeEmail,
                    x.DepartmentPath,
                    x.PositionName,
                    user is not null,
                    user?.Id,
                    user?.UserName,
                    user?.Email,
                    user?.ApprovalStatus.ToString(),
                    user?.IsActive ?? false,
                    user?.AccessLevel,
                    user is null
                        ? []
                        : roleLookup.TryGetValue(user.Id, out var roles)
                            ? roles
                            : []);
            })
            .ToArray();
    }

    public async Task<EmployeeAccountListItemDto> OpenAsync(
        OpenEmployeeAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(request.EmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Nhân viên mở tài khoản không hợp lệ.");
        }

        var temporaryPassword = request.TemporaryPassword?.Trim() ?? string.Empty;
        if(temporaryPassword.Length < 9)
        {
            throw new InvalidOperationException("Mật khẩu tạm phải có ít nhất 9 ký tự.");
        }

        var roleName = Normalize(request.RoleName);
        if(string.IsNullOrWhiteSpace(roleName))
        {
            throw new InvalidOperationException("Vai trò khởi tạo không được để trống.");
        }

        var employee = await dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.EmployeeId && !x.IsDeleted,
                cancellationToken);

        if(employee is null)
        {
            throw new InvalidOperationException("Không tìm thấy nhân viên để mở tài khoản.");
        }

        var userName = Normalize(employee.EmployeeCode);
        if(string.IsNullOrWhiteSpace(userName))
        {
            throw new InvalidOperationException("Nhân viên chưa có mã nhân viên hợp lệ để tạo tài khoản đăng nhập.");
        }

        var existingEmployeeAccount = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId, cancellationToken);
        if(existingEmployeeAccount is not null)
        {
            throw new InvalidOperationException("Nhân viên này đã có tài khoản trong hệ thống.");
        }

        var existingUserName = await userManager.FindByNameAsync(userName);
        if(existingUserName is not null)
        {
            throw new InvalidOperationException("Tài khoản đăng nhập đã tồn tại.");
        }

        if(!await roleManager.RoleExistsAsync(roleName))
        {
            var roleCreateResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if(!roleCreateResult.Succeeded)
            {
                throw new InvalidOperationException(BuildIdentityErrorMessage(roleCreateResult.Errors, "Không thể khởi tạo vai trò cho tài khoản."));
            }
        }

        var accountEmail = Normalize(employee.Email);
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = accountEmail,
            EmailConfirmed = true,
            LockoutEnabled = true,
            EmployeeId = employee.Id,
            ApprovalStatus = EmployeeAccountApprovalStatus.PendingApproval,
            AccessLevel = Normalize(request.AccessLevel) ?? roleName,
            IsActive = false
        };

        var createResult = await userManager.CreateAsync(user, temporaryPassword);
        if(!createResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(createResult.Errors, "Không thể mở tài khoản cho nhân viên."));
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, roleName);
        if(!addRoleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException(BuildIdentityErrorMessage(addRoleResult.Errors, "Không thể gán vai trò cho tài khoản nhân viên."));
        }

        return await GetByEmployeeIdAsync(employee.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại tài khoản nhân viên vừa tạo.");
    }

    public async Task<EmployeeAccountListItemDto> ApproveAsync(
        ReviewEmployeeAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveExistingEmployeeAccountAsync(request.EmployeeId, cancellationToken);
        var reviewedByUserId = ValidateReviewedByUserId(request.ReviewedByUserId);

        user.ApprovalStatus = EmployeeAccountApprovalStatus.Approved;
        user.IsActive = true;
        user.ApprovedByUserId = reviewedByUserId;
        user.ApprovedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        user.RejectedAtUtc = null;
        user.RejectionReason = null;

        var updateResult = await userManager.UpdateAsync(user);
        if(!updateResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(updateResult.Errors, "Không thể phê duyệt tài khoản nhân viên."));
        }

        return await GetByEmployeeIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại tài khoản nhân viên vừa phê duyệt.");
    }

    public async Task<EmployeeAccountListItemDto> RejectAsync(
        ReviewEmployeeAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveExistingEmployeeAccountAsync(request.EmployeeId, cancellationToken);
        var rejectionReason = Normalize(request.RejectionReason);
        if(string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new InvalidOperationException("Lý do từ chối không được để trống.");
        }

        _ = ValidateReviewedByUserId(request.ReviewedByUserId);

        user.ApprovalStatus = EmployeeAccountApprovalStatus.Rejected;
        user.IsActive = false;
        user.RejectedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        user.RejectionReason = rejectionReason;

        var updateResult = await userManager.UpdateAsync(user);
        if(!updateResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(updateResult.Errors, "Không thể từ chối tài khoản nhân viên."));
        }

        return await GetByEmployeeIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại tài khoản nhân viên vừa từ chối.");
    }

    public async Task<EmployeeAccountListItemDto> ResetPasswordAsync(
        ResetEmployeeAccountPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveExistingEmployeeAccountAsync(request.EmployeeId, cancellationToken);
        var temporaryPassword = request.TemporaryPassword?.Trim() ?? string.Empty;
        if(temporaryPassword.Length < 9)
        {
            throw new InvalidOperationException("Mật khẩu tạm phải có ít nhất 9 ký tự.");
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);
        if(!resetResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(resetResult.Errors, "Không thể đặt lại mật khẩu tạm cho tài khoản nhân viên."));
        }

        var unlockResult = await userManager.SetLockoutEndDateAsync(user, null);
        if(!unlockResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(unlockResult.Errors, "Không thể mở khóa tài khoản sau khi đặt lại mật khẩu."));
        }

        var resetFailedCountResult = await userManager.ResetAccessFailedCountAsync(user);
        if(!resetFailedCountResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(resetFailedCountResult.Errors, "Không thể xóa số lần đăng nhập sai sau khi đặt lại mật khẩu."));
        }

        return await GetByEmployeeIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại tài khoản nhân viên vừa đặt lại mật khẩu.");
    }

    public async Task<EmployeeAccountListItemDto> ActivateAsync(
        EmployeeAccountStateChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveExistingEmployeeAccountAsync(request.EmployeeId, cancellationToken);

        if(user.IsActive && user.ApprovalStatus == EmployeeAccountApprovalStatus.Approved)
        {
            throw new InvalidOperationException("Tài khoản nhân viên này đang ở trạng thái kích hoạt.");
        }

        if(user.ApprovalStatus is EmployeeAccountApprovalStatus.PendingApproval or EmployeeAccountApprovalStatus.Draft or EmployeeAccountApprovalStatus.Rejected)
        {
            throw new InvalidOperationException("Tài khoản này không thể kích hoạt trực tiếp. Hãy hoàn thành đúng luồng phê duyệt tài khoản.");
        }

        user.ApprovalStatus = EmployeeAccountApprovalStatus.Approved;
        user.IsActive = true;
        user.RejectedAtUtc = null;
        user.RejectionReason = null;

        var updateResult = await userManager.UpdateAsync(user);
        if(!updateResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(updateResult.Errors, "Không thể kích hoạt lại tài khoản nhân viên."));
        }

        return await GetByEmployeeIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại tài khoản nhân viên vừa kích hoạt.");
    }

    public async Task<EmployeeAccountListItemDto> DeactivateAsync(
        EmployeeAccountStateChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveExistingEmployeeAccountAsync(request.EmployeeId, cancellationToken);

        if(!user.IsActive)
        {
            throw new InvalidOperationException("Tài khoản nhân viên này hiện chưa ở trạng thái kích hoạt.");
        }

        user.ApprovalStatus = EmployeeAccountApprovalStatus.Disabled;
        user.IsActive = false;

        var updateResult = await userManager.UpdateAsync(user);
        if(!updateResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage(updateResult.Errors, "Không thể ngưng kích hoạt tài khoản nhân viên."));
        }

        return await GetByEmployeeIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại tài khoản nhân viên vừa ngưng kích hoạt.");
    }

    private async Task<EmployeeAccountListItemDto?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var rows = await GetAsync(cancellationToken);
        return rows.SingleOrDefault(x => x.EmployeeId == employeeId);
    }

    private async Task<ApplicationUser> ResolveExistingEmployeeAccountAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if(employeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Nhân viên cần thao tác tài khoản không hợp lệ.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.EmployeeId == employeeId, cancellationToken);
        if(user is null)
        {
            throw new InvalidOperationException("Nhân viên này chưa có tài khoản trong hệ thống.");
        }

        return user;
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> LoadRoleLookupAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if(userIds.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }

        var roleRows = await (
                from userRole in dbContext.Set<IdentityUserRole<string>>().AsNoTracking()
                join role in dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                orderby role.Name
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name
                })
            .ToListAsync(cancellationToken);

        return roleRows
            .Where(x => !string.IsNullOrWhiteSpace(x.RoleName))
            .GroupBy(x => x.UserId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(x => x.RoleName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static string BuildDepartmentPath(AttendanceDepartmentRow department) =>
        string.Join(
            " / ",
            new[]
            {
                Normalize(department.CenterName),
                Normalize(department.DepartmentOrWorkshopName),
                Normalize(department.TeamName),
                Normalize(department.GroupName)
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ValidateReviewedByUserId(string? reviewedByUserId)
    {
        var normalized = Normalize(reviewedByUserId);
        if(string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Không xác định được người đang thực hiện phê duyệt tài khoản.");
        }

        return normalized;
    }

    private static string BuildIdentityErrorMessage(
        IEnumerable<IdentityError> errors,
        string fallbackMessage)
    {
        var messages = errors
            .Select(static error => error.Description?.Trim())
            .Where(static description => !string.IsNullOrWhiteSpace(description))
            .ToArray();

        return messages.Length == 0
            ? fallbackMessage
            : string.Join("; ", messages);
    }
}
