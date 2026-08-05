using Vnta.Hrm.Web.Client.Models.Security;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class EmployeeAccountDataProvider(IEmployeeAccountService employeeAccountService)
{
    public async Task<IReadOnlyList<EmployeeAccountRecord>> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await employeeAccountService.GetAsync(cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public async Task<EmployeeAccountRecord> OpenAsync(
        OpenEmployeeAccountFormModel model,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeAccountService.OpenAsync(
            new OpenEmployeeAccountRequest(
                model.EmployeeId,
                model.TemporaryPassword,
                model.RoleName,
                model.AccessLevel),
            cancellationToken);

        return MapRecord(row);
    }

    public async Task<EmployeeAccountRecord> ApproveAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeAccountService.ApproveAsync(
            new ReviewEmployeeAccountRequest(employeeId, string.Empty, null),
            cancellationToken);

        return MapRecord(row);
    }

    public async Task<EmployeeAccountRecord> RejectAsync(
        Guid employeeId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeAccountService.RejectAsync(
            new ReviewEmployeeAccountRequest(employeeId, string.Empty, rejectionReason),
            cancellationToken);

        return MapRecord(row);
    }

    public async Task<EmployeeAccountRecord> ResetPasswordAsync(
        Guid employeeId,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeAccountService.ResetPasswordAsync(
            new ResetEmployeeAccountPasswordRequest(employeeId, temporaryPassword),
            cancellationToken);

        return MapRecord(row);
    }

    public async Task<EmployeeAccountRecord> ActivateAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeAccountService.ActivateAsync(
            new EmployeeAccountStateChangeRequest(employeeId),
            cancellationToken);

        return MapRecord(row);
    }

    public async Task<EmployeeAccountRecord> DeactivateAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeAccountService.DeactivateAsync(
            new EmployeeAccountStateChangeRequest(employeeId),
            cancellationToken);

        return MapRecord(row);
    }

    private static EmployeeAccountRecord MapRecord(EmployeeAccountListItemDto source) =>
        new()
        {
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            FirstName = source.FirstName,
            LastName = source.LastName,
            EmployeeEmail = source.EmployeeEmail,
            DepartmentPath = source.DepartmentPath,
            PositionName = source.PositionName,
            HasAccount = source.HasAccount,
            UserId = source.UserId,
            UserName = source.UserName,
            AccountEmail = source.AccountEmail,
            ApprovalStatus = source.ApprovalStatus,
            IsActive = source.IsActive,
            AccessLevel = source.AccessLevel,
            RoleNames = source.RoleNames
        };
}
