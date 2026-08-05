using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Identity;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Integrations.Payroll;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Infrastructure.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
namespace Vnta.Hrm.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AuditEventRow> AuditEvents => Set<AuditEventRow>();

    public DbSet<AuditPropertyChangeRow> AuditPropertyChanges => Set<AuditPropertyChangeRow>();

    public DbSet<AdmsDeviceCommandRow> DeviceCommands => Set<AdmsDeviceCommandRow>();

    public DbSet<AttendanceLogRow> AttendanceLogs => Set<AttendanceLogRow>();

    public DbSet<AttendanceDailySummaryRow> AttendanceDailySummaries => Set<AttendanceDailySummaryRow>();

    public DbSet<AttendanceWorkdaySummaryRow> AttendanceWorkdaySummaries => Set<AttendanceWorkdaySummaryRow>();

    public DbSet<AttendanceOvertimeRegistrationRequestRow> AttendanceOvertimeRegistrationRequests => Set<AttendanceOvertimeRegistrationRequestRow>();

    public DbSet<AttendanceOvertimeRegistrationDetailRow> AttendanceOvertimeRegistrationDetails => Set<AttendanceOvertimeRegistrationDetailRow>();

    public DbSet<AttendanceOvertimeRegistrationHistoryRow> AttendanceOvertimeRegistrationHistories => Set<AttendanceOvertimeRegistrationHistoryRow>();

    public DbSet<AttendanceBiometricDataRow> BiometricData => Set<AttendanceBiometricDataRow>();

    public DbSet<AttendanceDeviceUserProfileRow> DeviceUserProfiles => Set<AttendanceDeviceUserProfileRow>();

    public DbSet<AttendanceFingerprintTemplateRow> FingerprintTemplates => Set<AttendanceFingerprintTemplateRow>();

    public DbSet<AttendanceBioPhotoRow> BioPhotos => Set<AttendanceBioPhotoRow>();

    public DbSet<AttendanceUserPictureRow> UserPictures => Set<AttendanceUserPictureRow>();

    public DbSet<AttendanceStatusCodeRow> AttendanceStatusCodes => Set<AttendanceStatusCodeRow>();

    public DbSet<AttendanceWorkCalendarDayRow> AttendanceWorkCalendarDays => Set<AttendanceWorkCalendarDayRow>();

    public DbSet<AttendanceGatewayEmployeeRow> Employees => Set<AttendanceGatewayEmployeeRow>();
    public DbSet<EmployeeContactProfileRow> EmployeeContactProfiles => Set<EmployeeContactProfileRow>();
    public DbSet<CitizenIdentityRow> EmployeeCitizenIdentities => Set<CitizenIdentityRow>();

    public DbSet<AttendanceDepartmentRow> Departments => Set<AttendanceDepartmentRow>();

    public DbSet<AttendanceDeviceRow> Devices => Set<AttendanceDeviceRow>();

    public DbSet<AttendanceGatewayPositionRow> Positions => Set<AttendanceGatewayPositionRow>();

    public DbSet<AttendanceShiftRow> Shifts => Set<AttendanceShiftRow>();

    public DbSet<AttendanceShiftAssignmentRow> ShiftAssignments => Set<AttendanceShiftAssignmentRow>();

    public DbSet<ShiftSchedulingSettingRow> ShiftSchedulingSettings => Set<ShiftSchedulingSettingRow>();

    public DbSet<BasicSalaryRecordRow> BasicSalaryRecords => Set<BasicSalaryRecordRow>();

    public DbSet<PayrollMonthlyWorkInputRow> PayrollMonthlyWorkInputs => Set<PayrollMonthlyWorkInputRow>();

    public DbSet<PayrollAttendanceAllowanceRecordRow> PayrollAttendanceAllowanceRecords => Set<PayrollAttendanceAllowanceRecordRow>();

    public DbSet<PayrollMealAllowanceRecordRow> PayrollMealAllowanceRecords => Set<PayrollMealAllowanceRecordRow>();

    // Snapshot trung tâm, được các bảng phụ cấp chi tiết tham chiếu theo từng nhân viên/kỳ lương.
    public DbSet<PayrollAllowanceSummaryRecordRow> PayrollAllowanceSummaryRecords => Set<PayrollAllowanceSummaryRecordRow>();

    public DbSet<PayrollDeductionSummaryRecordRow> PayrollDeductionSummaryRecords => Set<PayrollDeductionSummaryRecordRow>();

    public DbSet<PayrollAllowanceSummaryLeaveHolidayRecordRow> PayrollAllowanceSummaryLeaveHolidayRecords => Set<PayrollAllowanceSummaryLeaveHolidayRecordRow>();

    public DbSet<PayrollAllowanceOtherResponsibilityRecordRow> PayrollAllowanceOtherResponsibilityRecords => Set<PayrollAllowanceOtherResponsibilityRecordRow>();

    public DbSet<PayrollHazardAllowanceRecordRow> PayrollHazardAllowanceRecords => Set<PayrollHazardAllowanceRecordRow>();
    public DbSet<HazardAllowanceExportJobRow> HazardAllowanceExportJobs => Set<HazardAllowanceExportJobRow>();
    public DbSet<PayrollOtherAllowanceRecordRow> PayrollOtherAllowanceRecords => Set<PayrollOtherAllowanceRecordRow>();
    public DbSet<PayrollEmployeeSeniorityAllowanceRow> PayrollEmployeeSeniorityAllowances => Set<PayrollEmployeeSeniorityAllowanceRow>();

    public DbSet<PayrollDeductionInsuranceRecordRow> PayrollDeductionInsuranceRecords => Set<PayrollDeductionInsuranceRecordRow>();

    public DbSet<PayrollDeductionTaxRecordRow> PayrollDeductionTaxRecords => Set<PayrollDeductionTaxRecordRow>();

    public DbSet<PayrollDeductionUnionFeeRecordRow> PayrollDeductionUnionFeeRecords => Set<PayrollDeductionUnionFeeRecordRow>();

    public DbSet<PayrollDeductionAdvanceRecordRow> PayrollDeductionAdvanceRecords => Set<PayrollDeductionAdvanceRecordRow>();

    public DbSet<PayrollDeductionOtherRecordRow> PayrollDeductionOtherRecords => Set<PayrollDeductionOtherRecordRow>();

    public DbSet<PayrollEmployeeTaxDependentRow> PayrollEmployeeTaxDependents => Set<PayrollEmployeeTaxDependentRow>();

    public DbSet<PayrollResponsibilityAllowanceGradeRow> PayrollResponsibilityAllowanceGrades => Set<PayrollResponsibilityAllowanceGradeRow>();

    public DbSet<PayrollResponsibilityAllowanceGradePositionRow> PayrollResponsibilityAllowanceGradePositions => Set<PayrollResponsibilityAllowanceGradePositionRow>();

    public DbSet<PayrollResponsibilityAllowanceEmployeeAssignmentRow> PayrollResponsibilityAllowanceEmployeeAssignments => Set<PayrollResponsibilityAllowanceEmployeeAssignmentRow>();

    public DbSet<PayrollResponsibilityAllowanceAbcRow> PayrollResponsibilityAllowanceAbcRows => Set<PayrollResponsibilityAllowanceAbcRow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
