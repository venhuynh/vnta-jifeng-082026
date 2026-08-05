using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Navigation;

public static class VntaNavMenuCatalog
{
    public const string AuthenticatedRole = "__authenticated__";
    public const string AdminRole = InternalAccountRoles.Admin;
    public const string SystemAdminRole = InternalAccountRoles.SystemAdmin;
    public const string HrAdminRole = InternalAccountRoles.HrAdmin;
    public const string PayrollAdminRole = InternalAccountRoles.PayrollAdmin;
    public const string AttendanceAdminRole = InternalAccountRoles.AttendanceAdmin;

    private const string DevExpressIconCssClass = "navmenu-devexpress-icon";
    private const string UiDemoTone = "navmenu-tone-ui-demo";
    private const string ContactsTone = "navmenu-tone-contacts";
    private const string PlanningTone = "navmenu-tone-planning";
    private const string AnalyticsTone = "navmenu-tone-analytics";
    private const string OverviewTone = "navmenu-tone-overview";
    private const string ImplementationTone = "navmenu-tone-implementation";
    private const string HrTone = "navmenu-tone-hr";
    private const string ShiftTone = "navmenu-tone-shift";
    private const string AttendanceTone = "navmenu-tone-attendance";
    private const string AllowanceTone = "navmenu-tone-allowance";
    private const string PayrollTone = "navmenu-tone-payroll";
    private const string DeductionTone = "navmenu-tone-payroll";
    private const string AdminTone = "navmenu-tone-admin";
    private const string AdmsTone = "navmenu-tone-adms";
    private const string WorkCalendarRoute = "/attendance/work-calendar";
    private const string ShiftSchedulingSettingsRoute = "/attendance/shift-scheduling-settings";
    private const string ShiftRosterRoute = "/attendance/shift-roster";
    private const string ShiftSettingsRoute = "/attendance/shifts";

    private static readonly IReadOnlyList<string> HumanResourcesRoles = InternalAccountRoles.HumanResourcesRoles;
    private static readonly IReadOnlyList<string> ShiftManagementRoles = InternalAccountRoles.ShiftManagementRoles;
    private static readonly IReadOnlyList<string> AttendanceAdministrationRoles = InternalAccountRoles.AttendanceAdministrationRoles;
    private static readonly IReadOnlyList<string> PayrollAdministrationRoles = InternalAccountRoles.PayrollAdministrationRoles;
    private static readonly IReadOnlyList<string> DeviceAdministrationRoles = InternalAccountRoles.DeviceAdministrationRoles;
    private static readonly IReadOnlyList<string> AdminGroupRoles = [AdminRole, SystemAdminRole, HrAdminRole, AttendanceAdminRole];

    public static readonly IReadOnlyList<VntaNavMenuNode> All =
    [
        new()
        {
            Key = "ui_demo",
            Text = "Giao diện mẫu",
            IconUrl = VntaDevExpressIcons.UiDemo,
            IconCssClass = Css(UiDemoTone),
            Children =
            [
                new()
                {
                    Key = "contacts",
                    Text = "Danh bạ",
                    IconUrl = VntaDevExpressIcons.Contacts,
                    IconCssClass = Css(ContactsTone),
                    Children =
                    [
                        new()
                        {
                            Key = "contact_list",
                            Text = "Danh sách liên hệ",
                            Route = "/ContactList",
                            IconUrl = VntaDevExpressIcons.Employee,
                            IconCssClass = Css(ContactsTone)
                        },
                        new()
                        {
                            Key = "contact_details",
                            Text = "Chi tiết liên hệ",
                            Route = "/ContactDetails",
                            IconUrl = VntaDevExpressIcons.ContactCard,
                            IconCssClass = Css(ContactsTone)
                        }
                    ]
                },
                new()
                {
                    Key = "planning",
                    Text = "Lập kế hoạch",
                    IconUrl = VntaDevExpressIcons.Planning,
                    IconCssClass = Css(PlanningTone),
                    Children =
                    [
                        new()
                        {
                            Key = "task_list",
                            Text = "Danh sách công việc",
                            Route = "/TaskList",
                            IconUrl = VntaDevExpressIcons.TaskList,
                            IconCssClass = Css(PlanningTone)
                        },
                        new()
                        {
                            Key = "scheduler",
                            Text = "Lịch công việc",
                            Route = "/Scheduler",
                            IconUrl = VntaDevExpressIcons.Scheduler,
                            IconCssClass = Css(PlanningTone)
                        }
                    ]
                },
                new()
                {
                    Key = "analytics",
                    Text = "Phân tích",
                    IconUrl = VntaDevExpressIcons.Analytics,
                    IconCssClass = Css(AnalyticsTone),
                    Children =
                    [
                        new()
                        {
                            Key = "dashboard",
                            Text = "Tổng quan",
                            Route = "/Dashboard",
                            IconUrl = VntaDevExpressIcons.Gauge,
                            IconCssClass = Css(AnalyticsTone)
                        },
                        new()
                        {
                            Key = "sales_analysis",
                            Text = "Phân tích doanh số",
                            Route = "/SalesAnalysis",
                            IconUrl = VntaDevExpressIcons.Trend,
                            IconCssClass = Css(AnalyticsTone)
                        }
                    ]
                }
            ]
        },
        MenuGroup(
            "overview",
            "Tổng quan",
            VntaDevExpressIcons.Gauge,
            OverviewTone,
            MenuItem("overview_daily_attendance", "Chấm công", "/overview/daily-attendance", VntaDevExpressIcons.Attendance, OverviewTone, isInProgress: true),
            MenuItem("overview_allowances", "Phụ cấp", "/payroll/allowance-dashboard", VntaDevExpressIcons.Allowance, OverviewTone, isInProgress: true),
            MenuItem("overview_deductions", "Khấu trừ", "/payroll/deduction-dashboard", VntaDevExpressIcons.Deduction, OverviewTone, isInProgress: true)),
        MenuGroup(
            "implementation",
            "Đang triển khai",
            VntaDevExpressIcons.InProgress,
            ImplementationTone,
            RestrictedMenuItem(
                "implementation_overtime_registrations",
                "Đăng ký tăng ca",
                "/approval/overtime-registrations",
                VntaDevExpressIcons.OvertimeRegistration,
                ImplementationTone,
                AttendanceAdministrationRoles,
                isInProgress: true),
            RestrictedMenuItem(
                "implementation_overtime_registration_approvals",
                "Phê duyệt tăng ca",
                "/approval/overtime-registration-approvals",
                VntaDevExpressIcons.OvertimeApproval,
                ImplementationTone,
                AttendanceAdministrationRoles,
                isInProgress: true)),
        RestrictedMenuGroup(
            "hr",
            "Nhân sự",
            VntaDevExpressIcons.Hr,
            HrTone,
            HumanResourcesRoles,
            MenuItem("hr_departments", "Phòng ban", "/attendance/departments", VntaDevExpressIcons.Organization, HrTone, isInProgress: true),
            MenuItem("hr_positions", "Chức vụ", "/attendance/positions", VntaDevExpressIcons.Organization, HrTone),
            MenuGroup(
                "hr_employees",
                "Nhân viên",
                VntaDevExpressIcons.Employee,
                HrTone,
                MenuItem("hr_employee_list", "Danh sách", "/attendance/employees", VntaDevExpressIcons.Employee, HrTone),
                MenuItem("hr_employee_details", "Chi tiết nhân viên", "/attendance/employees/details", VntaDevExpressIcons.ContactCard, HrTone, isInProgress: true))),
        RestrictedMenuGroup(
            "shift_management",
            "Ca kíp",
            VntaDevExpressIcons.ShiftManagement,
            ShiftTone,
            ShiftManagementRoles,
            MenuItem("attendance_work_calendar", "Lịch làm việc", WorkCalendarRoute, VntaDevExpressIcons.WorkCalendar, ShiftTone, isInProgress: true),
            MenuItem("shift_scheduling_settings", "Cài đặt xếp ca", ShiftSchedulingSettingsRoute, VntaDevExpressIcons.ShiftConfiguration, ShiftTone, isInProgress: true),
            MenuItem("shift_roster_board", "Bảng xếp ca", ShiftRosterRoute, VntaDevExpressIcons.Scheduler, ShiftTone, isInProgress: true),
            MenuItem("shift_settings", "Cài đặt ca", ShiftSettingsRoute, VntaDevExpressIcons.ShiftConfiguration, ShiftTone)),
        RestrictedMenuGroup(
            "attendance",
            "Chấm công",
            VntaDevExpressIcons.Attendance,
            AttendanceTone,
            AttendanceAdministrationRoles,
            MenuGroup(
                "attendance_overview",
                "Tổng quan",
                VntaDevExpressIcons.Gauge,
                AttendanceTone,
                MenuItem("attendance_timesheet_dashboard", "Bảng công", "/attendance/timesheet-dashboard", VntaDevExpressIcons.Gauge, AttendanceTone, isInProgress: true, isRoadmapOnly: false)),
            MenuItem("attendance_workday_summaries", "Bảng công ngày", "/attendance/workday-summaries", VntaDevExpressIcons.Attendance, AttendanceTone, isInProgress: true),
            MenuItem("attendance_monthly_work_summaries", "Bảng công tháng", "/attendance/monthly-work-summaries", VntaDevExpressIcons.AttendanceMonthly, AttendanceTone, isInProgress: true, isRoadmapOnly: false),
            MenuItem("attendance_result_codes", "Code kết quả tính công", "/attendance/result-codes", VntaDevExpressIcons.ResultCodes, AttendanceTone, isInProgress: true),
            MenuItem("attendance_raw_logs", "Dữ liệu thô", "/attendance/logs", VntaDevExpressIcons.Database, AttendanceTone)),
        RestrictedMenuGroup(
            "allowances",
            "Phụ cấp",
            VntaDevExpressIcons.Allowance,
            AllowanceTone,
            PayrollAdministrationRoles,
            MenuItem("allowance_dashboard", "Tổng quan phụ cấp", "/payroll/allowance-dashboard", VntaDevExpressIcons.AllowanceDashboard, AllowanceTone, isInProgress: true, isRoadmapOnly: false),
            MenuItem("allowance_summary", "Tổng hợp", "/payroll/allowance-summary", VntaDevExpressIcons.AllowanceData, AllowanceTone, isInProgress: true, isRoadmapOnly: false),
            MenuItem("attendance_allowance", "Chuyên cần", "/payroll/attendance-allowance", VntaDevExpressIcons.Attendance, AllowanceTone, isInProgress: true),
            MenuItem("meal_allowance", "Cơm", "/payroll/meal-allowance", VntaDevExpressIcons.AllowanceMeal, AllowanceTone),
            MenuItem("hazard_allowance", "Độc hại", "/payroll/hazard-allowance", VntaDevExpressIcons.AllowanceHazard, AllowanceTone, isInProgress: true),
            MenuItem("seniority_allowance", "Thâm niên", "/payroll/seniority-allowance", VntaDevExpressIcons.AllowanceSeniority, AllowanceTone, isInProgress: true),
            MenuItem("other_allowance", "Phụ cấp khác", "/payroll/other-allowance", VntaDevExpressIcons.Allowance, AllowanceTone),
            MenuItem("leave_holiday_allowance", "Phép - Lễ", "/payroll/leave-holiday-allowance", VntaDevExpressIcons.AllowanceLeaveHoliday, AllowanceTone, isInProgress: false, isRoadmapOnly: false),
            MenuGroup(
                "responsibility_allowances",
                "Trách nhiệm",
                VntaDevExpressIcons.AllowanceResponsibility,
                AllowanceTone,
                MenuItem("responsibility_allowances_overview", "Tổng quan", "/payroll/responsibility-allowance", VntaDevExpressIcons.Analytics, AllowanceTone, isInProgress: true),
                MenuItem("responsibility_allowance_grades", "Cấp bậc", "/payroll/responsibility-allowances/grades", VntaDevExpressIcons.Organization, AllowanceTone, isInProgress: true),
                MenuItem(
                    "responsibility_allowance_employee_assignments",
                    "DS cấp bậc nhân viên",
                    "/payroll/responsibility-allowances/employee-assignments",
                    VntaDevExpressIcons.Employee,
                    AllowanceTone,
                    isInProgress: true)),
            MenuItem(
                "other_responsibility_allowance",
                "Phụ cấp trách nhiệm khác",
                "/payroll/other-responsibility-allowance",
                VntaDevExpressIcons.Allowance,
                AllowanceTone,
                isInProgress: true)),
        RestrictedMenuGroup(
            "payroll",
            "Tính lương",
            VntaDevExpressIcons.Payroll,
            PayrollTone,
            PayrollAdministrationRoles,
            MenuItem("payroll_basic_salaries", "Lương căn bản", "/payroll/basic-salaries", VntaDevExpressIcons.PayrollBasicSalary, PayrollTone, isInProgress: true)),
        RestrictedMenuGroup(
            "deductions",
            "Khấu trừ",
            VntaDevExpressIcons.Deduction,
            DeductionTone,
            PayrollAdministrationRoles,
            MenuItem("deduction_dashboard", "Tổng quan khấu trừ", "/payroll/deduction-dashboard", VntaDevExpressIcons.Gauge, DeductionTone, isInProgress: true),
            MenuItem("deduction_summary", "Tổng kết khấu trừ", "/payroll/deduction-summary", VntaDevExpressIcons.DeductionData, DeductionTone, isInProgress: true),
            MenuGroup(
                "personal_income_tax",
                "Thuế TNCN",
                VntaDevExpressIcons.Deduction,
                DeductionTone,
                MenuItem("personal_income_tax_overview", "Tổng quan TNCN", "/payroll/personal-income-tax-overview", VntaDevExpressIcons.DeductionData, DeductionTone, isInProgress: true),
                MenuItem("personal_income_tax_deductions", "Thuế TNCN theo kỳ lương", "/payroll/personal-income-tax-deductions", VntaDevExpressIcons.Deduction, DeductionTone, isInProgress: true),
                MenuItem("personal_income_tax_family_deductions", "Giảm trừ gia cảnh", "/payroll/family-deductions", VntaDevExpressIcons.DeductionFamily, DeductionTone, isInProgress: true)),
            MenuItem("social_health_insurance_deductions", "BHXH-YT", "/payroll/social-health-insurance-deductions", VntaDevExpressIcons.DeductionInsurance, DeductionTone, isInProgress: true),
            MenuItem("union_fee_deductions", "Phí công đoàn", "/payroll/union-fee-deductions", VntaDevExpressIcons.DeductionUnion, DeductionTone, isInProgress: true),
            MenuItem("advance_deductions", "Tạm ứng", "/payroll/advance-deductions", VntaDevExpressIcons.DeductionAdvance, DeductionTone, isInProgress: true),
            MenuItem("other_deductions", "Khấu trừ khác", "/payroll/other-deductions", VntaDevExpressIcons.Deduction, DeductionTone)),
        RestrictedMenuGroup(
            "admin",
            "Quản trị",
            VntaDevExpressIcons.Settings,
            AdminTone,
            AdminGroupRoles,
            new()
            {
                Key = "employee_accounts",
                Text = "Tài khoản nhân viên",
                Route = "/admin/employee-accounts",
                IconUrl = VntaDevExpressIcons.AccountManagement,
                IconCssClass = Css(AdminTone),
                IsInProgress = true,
                AllowedCapabilities = [InternalAccountCapabilities.EmployeeAccountsOpen]
            },
            new()
            {
                Key = "account_approvals",
                Text = "Phê duyệt tài khoản",
                Route = "/admin/account-approvals",
                IconUrl = VntaDevExpressIcons.AccountApproval,
                IconCssClass = Css(AdminTone),
                IsInProgress = true,
                AllowedCapabilities = [InternalAccountCapabilities.EmployeeAccountsApprove]
            },
            new()
            {
                Key = "audit_trail",
                Text = "Nhật ký kiểm toán",
                Route = "/admin/audit-trail",
                IconUrl = VntaDevExpressIcons.AuditTrail,
                IconCssClass = Css(AdminTone),
                AllowedCapabilities = [InternalAccountCapabilities.AuditRead]
            },
            RestrictedMenuItem("attendance_biometric_data", "Sinh trắc học", "/attendance/biometric-data", VntaDevExpressIcons.Database, AdminTone, DeviceAdministrationRoles, isInProgress: true),
            RestrictedMenuItem("attendance_devices", "Máy chấm công", "/attendance/devices", VntaDevExpressIcons.Device, AdminTone, DeviceAdministrationRoles),
            RestrictedMenuItem("adms_monitor", "Giám sát ADMS", "/Adms", VntaDevExpressIcons.AdmsMonitor, AdmsTone, DeviceAdministrationRoles),
            RestrictedMenuItem("adms_device_commands", "Lệnh máy chấm công", "/adms/device-commands", VntaDevExpressIcons.Command, AdmsTone, DeviceAdministrationRoles))
    ];

    private static VntaNavMenuNode MenuGroup(
        string key,
        string text,
        string iconUrl,
        string toneClass,
        params VntaNavMenuNode[] children)
        => new()
        {
            Key = key,
            Text = text,
            IconUrl = iconUrl,
            IconCssClass = Css(toneClass),
            Children = children
        };

    private static VntaNavMenuNode RestrictedMenuGroup(
        string key,
        string text,
        string iconUrl,
        string toneClass,
        IReadOnlyList<string> allowedRoles,
        params VntaNavMenuNode[] children)
        => new()
        {
            Key = key,
            Text = text,
            IconUrl = iconUrl,
            IconCssClass = Css(toneClass),
            AllowedRoles = allowedRoles,
            Children = children
        };

    private static VntaNavMenuNode MenuItem(
        string key,
        string text,
        string? route,
        string iconUrl,
        string toneClass,
        bool isInProgress = false,
        bool isRoadmapOnly = false,
        params string[] routeAliases)
        => new()
        {
            Key = key,
            Text = text,
            Route = route,
            RouteAliases = routeAliases,
            IconUrl = iconUrl,
            IconCssClass = Css(toneClass),
            IsInProgress = isInProgress,
            IsRoadmapOnly = isRoadmapOnly
        };

    private static VntaNavMenuNode RestrictedMenuItem(
        string key,
        string text,
        string? route,
        string iconUrl,
        string toneClass,
        IReadOnlyList<string> allowedRoles,
        bool isInProgress = false,
        bool isRoadmapOnly = false,
        params string[] routeAliases)
        => new()
        {
            Key = key,
            Text = text,
            Route = route,
            RouteAliases = routeAliases,
            IconUrl = iconUrl,
            IconCssClass = Css(toneClass),
            IsInProgress = isInProgress,
            IsRoadmapOnly = isRoadmapOnly,
            AllowedRoles = allowedRoles
        };

    private static string Css(string toneClass)
        => $"{DevExpressIconCssClass} {toneClass}";
}
