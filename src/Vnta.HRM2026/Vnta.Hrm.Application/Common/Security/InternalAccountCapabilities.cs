namespace Vnta.Hrm.Application.Common.Security;

public static class InternalAccountCapabilities
{
    public const string EmployeeAccountsOpen = "employee_accounts.open";
    public const string EmployeeAccountsApprove = "employee_accounts.approve";
    public const string AuditRead = "audit.read";
    public const string AuditSensitiveRead = "audit.sensitive.read";
}
