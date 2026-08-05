namespace Vnta.Hrm.Application.Common.Security;

public static class InternalAccountRoles
{
    public const string Admin = "Admin";
    public const string SystemAdmin = "SystemAdmin";
    public const string HrAdmin = "HrAdmin";
    public const string PayrollAdmin = "PayrollAdmin";
    public const string AttendanceAdmin = "AttendanceAdmin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static readonly IReadOnlyList<string> CoreRoles =
    [
        Admin,
        SystemAdmin,
        HrAdmin,
        PayrollAdmin,
        AttendanceAdmin,
        Manager,
        Employee
    ];

    public static readonly IReadOnlyList<string> BootstrapAdministratorRoles =
    [
        Admin,
        SystemAdmin
    ];

    public static readonly IReadOnlyList<string> EmployeeAccountAdministratorRoles =
    [
        Admin,
        SystemAdmin,
        HrAdmin
    ];

    public static readonly IReadOnlyList<string> EmployeeAccountApprovalRoles =
    [
        SystemAdmin,
        HrAdmin
    ];

    public static readonly IReadOnlyList<string> HumanResourcesRoles =
    [
        SystemAdmin,
        HrAdmin
    ];

    public static readonly IReadOnlyList<string> ShiftManagementRoles =
    [
        SystemAdmin,
        AttendanceAdmin,
        HrAdmin
    ];

    public static readonly IReadOnlyList<string> AttendanceAdministrationRoles =
    [
        SystemAdmin,
        AttendanceAdmin,
        HrAdmin
    ];

    public static readonly IReadOnlyList<string> PayrollAdministrationRoles =
    [
        SystemAdmin,
        PayrollAdmin,
        HrAdmin
    ];

    public static readonly IReadOnlyList<string> DeviceAdministrationRoles =
    [
        SystemAdmin,
        AttendanceAdmin
    ];
}
