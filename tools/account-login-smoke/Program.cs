using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;
using Vnta.Hrm.Infrastructure;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Identity;

var options = ParseOptions(args);
var connectionString = string.IsNullOrWhiteSpace(options.ConnectionString)
    ? Environment.GetEnvironmentVariable("VNTA_DB")
    : options.ConnectionString;

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A jifeng_hrm connection string is required. Pass --connection or set VNTA_DB.");
}

DatabaseConnectionStringResolver.EnsureExpectedDatabase(connectionString);

Environment.SetEnvironmentVariable("VNTA_DB", connectionString);

var configuration = new ConfigurationBuilder().Build();

var services = new ServiceCollection();
services.AddLogging();
services.AddInfrastructureServices(configuration);

await using var serviceProvider = services.BuildServiceProvider();
await using var scope = serviceProvider.CreateAsyncScope();

var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var accountService = scope.ServiceProvider.GetRequiredService<IEmployeeAccountService>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

var result = new Dictionary<string, object?>();

var accountRows = await accountService.GetAsync();

var candidate = string.IsNullOrWhiteSpace(options.EmployeeCode)
    ? accountRows.FirstOrDefault(item => !item.HasAccount && !string.IsNullOrWhiteSpace(item.EmployeeCode))
    : accountRows.FirstOrDefault(item =>
        string.Equals(item.EmployeeCode, options.EmployeeCode, StringComparison.OrdinalIgnoreCase));

if (candidate is null)
{
    Console.Error.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
    {
        error = string.IsNullOrWhiteSpace(options.EmployeeCode)
            ? "No employee without account was found for transactional smoke."
            : $"Employee '{options.EmployeeCode}' was not found."
    }));
    Environment.ExitCode = 1;
    return;
}

if (candidate.HasAccount)
{
    Console.Error.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
    {
        error = $"Employee '{candidate.EmployeeCode}' already has an account. Pick an employee without an account for rollback smoke."
    }));
    Environment.ExitCode = 1;
    return;
}

await using var transaction = await dbContext.Database.BeginTransactionAsync();

var temporaryPassword = $"Tmp@{candidate.EmployeeCode}1";
var resetPassword = $"Reset@{candidate.EmployeeCode}1";
var reviewerUserId = await dbContext.Users
    .AsNoTracking()
    .Where(user => user.UserName == options.ReviewerUserName)
    .Select(user => user.Id)
    .SingleAsync();

try
{
    var opened = await accountService.OpenAsync(
        new OpenEmployeeAccountRequest(candidate.EmployeeId, temporaryPassword, InternalAccountRoles.Employee, InternalAccountRoles.Employee));
    var userAfterOpen = await dbContext.Users.SingleAsync(row => row.EmployeeId == candidate.EmployeeId);
    var temporaryPasswordAcceptedBeforeReset = await userManager.CheckPasswordAsync(userAfterOpen, temporaryPassword);
    var approved = await accountService.ApproveAsync(
        new ReviewEmployeeAccountRequest(candidate.EmployeeId, reviewerUserId, null));
    var reset = await accountService.ResetPasswordAsync(
        new ResetEmployeeAccountPasswordRequest(candidate.EmployeeId, resetPassword));
    var deactivated = await accountService.DeactivateAsync(
        new EmployeeAccountStateChangeRequest(candidate.EmployeeId));
    var activated = await accountService.ActivateAsync(
        new EmployeeAccountStateChangeRequest(candidate.EmployeeId));
    var userAfterReset = await dbContext.Users.SingleAsync(row => row.EmployeeId == candidate.EmployeeId);
    var temporaryPasswordAcceptedAfterReset = await userManager.CheckPasswordAsync(userAfterReset, temporaryPassword);
    var resetPasswordAccepted = await userManager.CheckPasswordAsync(userAfterReset, resetPassword);

    result["employeeCode"] = candidate.EmployeeCode;
    result["employeeId"] = candidate.EmployeeId;
    result["reviewerUserName"] = options.ReviewerUserName;
    result["opened"] = new
    {
        opened.HasAccount,
        opened.UserName,
        opened.ApprovalStatus,
        opened.IsActive,
        opened.AccessLevel,
        opened.RoleNames
    };
    result["approved"] = new
    {
        approved.ApprovalStatus,
        approved.IsActive
    };
    result["resetPassword"] = new
    {
        reset.UserName,
        reset.ApprovalStatus,
        reset.IsActive
    };
    result["deactivated"] = new
    {
        deactivated.ApprovalStatus,
        deactivated.IsActive
    };
    result["activated"] = new
    {
        activated.ApprovalStatus,
        activated.IsActive
    };
    result["passwordChecks"] = new
    {
        TemporaryPasswordAcceptedBeforeReset = temporaryPasswordAcceptedBeforeReset,
        TemporaryPasswordAcceptedAfterReset = temporaryPasswordAcceptedAfterReset,
        ResetPasswordAccepted = resetPasswordAccepted
    };

    await transaction.RollbackAsync();
    dbContext.ChangeTracker.Clear();

    var persistedAfterRollback = await dbContext.Users
        .AsNoTracking()
        .AnyAsync(user => user.EmployeeId == candidate.EmployeeId);

    result["persistedAfterRollback"] = persistedAfterRollback;
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    result["error"] = ex.ToString();
    Environment.ExitCode = 1;
}

Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
{
    WriteIndented = true
}));

return;

static SmokeOptions ParseOptions(string[] args)
{
    string? employeeCode = null;
    string? reviewerUserName = null;
    string? connectionString = null;

    for (var index = 0; index < args.Length; index++)
    {
        switch (args[index])
        {
            case "--employee-code":
                employeeCode = ReadValue(args, ref index, "--employee-code");
                break;
            case "--reviewer":
                reviewerUserName = ReadValue(args, ref index, "--reviewer");
                break;
            case "--connection":
                connectionString = ReadValue(args, ref index, "--connection");
                break;
        }
    }

    return new SmokeOptions(
        employeeCode,
        string.IsNullOrWhiteSpace(reviewerUserName) ? "admin" : reviewerUserName.Trim(),
        string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim());
}

static string ReadValue(string[] args, ref int index, string optionName)
{
    if (index + 1 >= args.Length)
    {
        throw new InvalidOperationException($"Missing value for option '{optionName}'.");
    }

    index++;
    return args[index];
}

internal sealed record SmokeOptions(
    string? EmployeeCode,
    string ReviewerUserName,
    string? ConnectionString);
