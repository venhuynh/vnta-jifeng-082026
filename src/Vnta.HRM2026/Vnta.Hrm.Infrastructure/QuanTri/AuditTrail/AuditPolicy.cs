using System.Collections.ObjectModel;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.CaKip.BangXepCa;
using Vnta.Hrm.Infrastructure.CaKip.CaiDatCa;
using Vnta.Hrm.Infrastructure.CaKip.LichLamViec;
using Vnta.Hrm.Infrastructure.DangKyPheDuyet.DangKyTangCa;
using Vnta.Hrm.Infrastructure.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// The persistence-facing allow-list. A CLR type that is absent from this policy is never
/// collected, which keeps technical, identity, biometric, and high-volume rows out of audit.
/// </summary>
public interface IAuditPolicy
{
    AuditEntityPolicy? GetPolicy(Type entityType);
}

public sealed class AuditEntityPolicy
{
    public AuditEntityPolicy(
        string logicalEntityType,
        bool allowHardDelete,
        IReadOnlyDictionary<string, AuditPropertyPolicy> properties,
        Func<object, string?>? displayNameSelector = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalEntityType);
        ArgumentNullException.ThrowIfNull(properties);

        LogicalEntityType = logicalEntityType;
        AllowHardDelete = allowHardDelete;
        Properties = properties;
        DisplayNameSelector = displayNameSelector;
    }

    public string LogicalEntityType { get; }

    public bool AllowHardDelete { get; }

    public IReadOnlyDictionary<string, AuditPropertyPolicy> Properties { get; }

    public Func<object, string?>? DisplayNameSelector { get; }

    public bool TryGetProperty(string propertyName, out AuditPropertyPolicy property) =>
        Properties.TryGetValue(propertyName, out property!);
}

public sealed record AuditPropertyPolicy(string Label, bool IsSensitive = false);

/// <summary>
/// Pilot policy. Expand it only through an explicit reviewed allow-list change.
/// </summary>
public sealed class AuditPolicy : IAuditPolicy
{
    private static readonly IReadOnlyDictionary<Type, AuditEntityPolicy> Policies =
        new ReadOnlyDictionary<Type, AuditEntityPolicy>(new Dictionary<Type, AuditEntityPolicy>
        {
            [typeof(AttendanceShiftRow)] = new(
                AuditEntityTypes.Shift,
                allowHardDelete: false,
                Properties(
                    (nameof(AttendanceShiftRow.Code), "Mã ca"),
                    (nameof(AttendanceShiftRow.Name), "Tên ca"),
                    (nameof(AttendanceShiftRow.StartTime), "Giờ bắt đầu"),
                    (nameof(AttendanceShiftRow.EndTime), "Giờ kết thúc"),
                    (nameof(AttendanceShiftRow.BreakStartTime), "Bắt đầu nghỉ"),
                    (nameof(AttendanceShiftRow.BreakEndTime), "Kết thúc nghỉ"),
                    (nameof(AttendanceShiftRow.IsOvernight), "Ca qua đêm"),
                    (nameof(AttendanceShiftRow.Status), "Trạng thái"),
                    (nameof(AttendanceShiftRow.ColorHex), "Màu hiển thị"),
                    (nameof(AttendanceShiftRow.WorkingDays), "Ngày làm việc")),
                entity => ((AttendanceShiftRow)entity).Name),

            [typeof(AttendanceWorkCalendarDayRow)] = new(
                AuditEntityTypes.WorkCalendarDay,
                allowHardDelete: true,
                Properties(
                    (nameof(AttendanceWorkCalendarDayRow.WorkDate), "Ngày làm việc"),
                    (nameof(AttendanceWorkCalendarDayRow.DayType), "Loại ngày"),
                    (nameof(AttendanceWorkCalendarDayRow.Name), "Tên ngày"),
                    (nameof(AttendanceWorkCalendarDayRow.Note), "Ghi chú")),
                entity => DescribeCalendarDay((AttendanceWorkCalendarDayRow)entity)),

            [typeof(AttendanceOvertimeRegistrationRequestRow)] = new(
                AuditEntityTypes.OvertimeRegistration,
                allowHardDelete: false,
                Properties(
                    (nameof(AttendanceOvertimeRegistrationRequestRow.WorkDate), "Ngày làm việc"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.DayType), "Loại ngày"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.WorkshopCode), "Mã xưởng"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.WorkshopName), "Tên xưởng"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.RequestedByEmployeeId), "Mã người đề nghị"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.RequestedBy), "Người đề nghị"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.ApprovedByEmployeeId), "Mã người phê duyệt"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.ApprovedBy), "Người phê duyệt"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.Status), "Trạng thái"),
                    (nameof(AttendanceOvertimeRegistrationRequestRow.Note), "Ghi chú")),
                entity => DescribeOvertimeRegistration((AttendanceOvertimeRegistrationRequestRow)entity)),

            [typeof(AttendanceShiftAssignmentRow)] = new(
                AuditEntityTypes.ShiftAssignment,
                allowHardDelete: false,
                Properties(
                    (nameof(AttendanceShiftAssignmentRow.EmployeeId), "Nhân viên"),
                    (nameof(AttendanceShiftAssignmentRow.WorkDate), "Ngày làm việc"),
                    (nameof(AttendanceShiftAssignmentRow.ShiftId), "Ca làm việc"),
                    (nameof(AttendanceShiftAssignmentRow.CreationType), "Loại tạo"),
                    (nameof(AttendanceShiftAssignmentRow.SourceBatchId), "Lô nguồn"),
                    (nameof(AttendanceShiftAssignmentRow.Notes), "Ghi chú")),
                entity => DescribeShiftAssignment((AttendanceShiftAssignmentRow)entity)),

            [typeof(PayrollEmployeeSeniorityAllowanceRow)] = new(
                AuditEntityTypes.SeniorityAllowance,
                allowHardDelete: false,
                SeniorityAllowanceProperties(),
                entity => ((PayrollEmployeeSeniorityAllowanceRow)entity)
                    .PayrollAllowanceSummaryRecordId.ToString("D")),

            [typeof(PayrollHazardAllowanceRecordRow)] = new(
                AuditEntityTypes.HazardAllowance,
                allowHardDelete: false,
                HazardAllowanceProperties(),
                entity => ((PayrollHazardAllowanceRecordRow)entity)
                    .PayrollAllowanceSummaryRecordId.ToString("D")),

            [typeof(PayrollOtherAllowanceRecordRow)] = new(
                AuditEntityTypes.OtherAllowance,
                allowHardDelete: true,
                OtherAllowanceProperties(),
                entity => ((PayrollOtherAllowanceRecordRow)entity).AllowanceName),

            [typeof(PayrollAllowanceSummaryRecordRow)] = new(
                AuditEntityTypes.AllowanceSummary,
                allowHardDelete: false,
                HazardAllowanceSummaryProperties(),
                entity => ((PayrollAllowanceSummaryRecordRow)entity).Id.ToString("D")),

            [typeof(PayrollDeductionInsuranceRecordRow)] = new(
                AuditEntityTypes.PayrollInsuranceDeduction,
                allowHardDelete: false,
                InsuranceDeductionProperties(),
                entity => ((PayrollDeductionInsuranceRecordRow)entity)
                    .PayrollDeductionSummaryRecordId.ToString("D")),

            [typeof(PayrollEmployeeTaxDependentRow)] = new(
                AuditEntityTypes.EmployeeTaxDependent,
                allowHardDelete: false,
                EmployeeTaxDependentProperties(),
                entity => ((PayrollEmployeeTaxDependentRow)entity).DependentFullName)
        });

    public AuditEntityPolicy? GetPolicy(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        for (var candidate = entityType; candidate is not null; candidate = candidate.BaseType)
        {
            if (Policies.TryGetValue(candidate, out var policy))
            {
                return policy;
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, AuditPropertyPolicy> Properties(
        params (string Name, string Label)[] properties)
    {
        var values = new Dictionary<string, AuditPropertyPolicy>(StringComparer.Ordinal);

        foreach (var (name, label) in properties)
        {
            values.Add(name, new AuditPropertyPolicy(label));
        }

        return new ReadOnlyDictionary<string, AuditPropertyPolicy>(values);
    }

    private static IReadOnlyDictionary<string, AuditPropertyPolicy> SeniorityAllowanceProperties() =>
        new ReadOnlyDictionary<string, AuditPropertyPolicy>(
            new Dictionary<string, AuditPropertyPolicy>(StringComparer.Ordinal)
            {
                [nameof(PayrollEmployeeSeniorityAllowanceRow.EmploymentStartDate)] = new("Ngày bắt đầu tính thâm niên"),
                [nameof(PayrollEmployeeSeniorityAllowanceRow.AdministrativeWorkDays)] = new("Công HC"),
                [nameof(PayrollEmployeeSeniorityAllowanceRow.LateEarlyLeaveWorkDays)] = new("Số ngày ĐTVS"),
                [nameof(PayrollEmployeeSeniorityAllowanceRow.SalaryWorkDays)] = new("Công tính lương"),
                [nameof(PayrollEmployeeSeniorityAllowanceRow.AppliedRuleKey)] = new("Quy tắc áp dụng"),
                [nameof(PayrollEmployeeSeniorityAllowanceRow.AllowanceAmount)] = new("Phụ cấp thâm niên", IsSensitive: true),
                [nameof(PayrollEmployeeSeniorityAllowanceRow.Note)] = new("Ghi chú"),
                [nameof(PayrollEmployeeSeniorityAllowanceRow.IsLocked)] = new("Trạng thái khóa")
            });

    private static IReadOnlyDictionary<string, AuditPropertyPolicy> InsuranceDeductionProperties() =>
        new ReadOnlyDictionary<string, AuditPropertyPolicy>(
            new Dictionary<string, AuditPropertyPolicy>(StringComparer.Ordinal)
            {
                [nameof(PayrollDeductionInsuranceRecordRow.IsLocked)] = new("Trạng thái khóa"),
                [nameof(PayrollDeductionInsuranceRecordRow.TotalInsuranceRate)] = new("Tổng tỷ lệ đóng bảo hiểm"),
                [nameof(PayrollDeductionInsuranceRecordRow.SocialInsuranceAmount)] = new("Khấu trừ BHXH", IsSensitive: true),
                [nameof(PayrollDeductionInsuranceRecordRow.HealthInsuranceAmount)] = new("Khấu trừ BHYT", IsSensitive: true),
                [nameof(PayrollDeductionInsuranceRecordRow.UnemploymentInsuranceAmount)] = new("Khấu trừ BHTN", IsSensitive: true),
                [nameof(PayrollDeductionInsuranceRecordRow.TotalDeductionAmount)] = new("Tổng khấu trừ bảo hiểm", IsSensitive: true)
            });

    private static IReadOnlyDictionary<string, AuditPropertyPolicy> HazardAllowanceProperties() =>
        new ReadOnlyDictionary<string, AuditPropertyPolicy>(
            new Dictionary<string, AuditPropertyPolicy>(StringComparer.Ordinal)
            {
                [nameof(PayrollHazardAllowanceRecordRow.QualifiedWorkdayCount)] = new("Ngày công đủ điều kiện"),
                [nameof(PayrollHazardAllowanceRecordRow.LateEarlyDeductionDays)] = new("Ngày trừ đi muộn/về sớm"),
                [nameof(PayrollHazardAllowanceRecordRow.PayableWorkdayCount)] = new("Ngày công hưởng phụ cấp"),
                [nameof(PayrollHazardAllowanceRecordRow.HazardAllowancePerDay)] = new("Phụ cấp độc hại theo ngày", IsSensitive: true),
                [nameof(PayrollHazardAllowanceRecordRow.HazardAllowanceAmount)] = new("Tiền phụ cấp độc hại", IsSensitive: true),
                [nameof(PayrollHazardAllowanceRecordRow.IsEligibleDepartment)] = new("Điều kiện hưởng"),
                [nameof(PayrollHazardAllowanceRecordRow.IsEligibleForAllowance)] = new("Trạng thái hưởng phụ cấp"),
                [nameof(PayrollHazardAllowanceRecordRow.ExclusionReason)] = new("Lý do không hưởng")
            });

    private static IReadOnlyDictionary<string, AuditPropertyPolicy> OtherAllowanceProperties() =>
        new ReadOnlyDictionary<string, AuditPropertyPolicy>(
            new Dictionary<string, AuditPropertyPolicy>(StringComparer.Ordinal)
            {
                [nameof(PayrollOtherAllowanceRecordRow.PayrollAllowanceSummaryRecordId)] = new("Bản ghi tổng hợp phụ cấp"),
                [nameof(PayrollOtherAllowanceRecordRow.AllowanceName)] = new("Tên phụ cấp"),
                [nameof(PayrollOtherAllowanceRecordRow.IsFixedAmount)] = new("Loại số tiền"),
                [nameof(PayrollOtherAllowanceRecordRow.AllowanceAmount)] = new("Số tiền phụ cấp", IsSensitive: true),
                [nameof(PayrollOtherAllowanceRecordRow.Note)] = new("Ghi chú"),
                [nameof(PayrollOtherAllowanceRecordRow.IsLocked)] = new("Trạng thái khóa")
            });

    private static IReadOnlyDictionary<string, AuditPropertyPolicy> HazardAllowanceSummaryProperties() =>
        new ReadOnlyDictionary<string, AuditPropertyPolicy>(
            new Dictionary<string, AuditPropertyPolicy>(StringComparer.Ordinal)
            {
                [nameof(PayrollAllowanceSummaryRecordRow.HazardAllowanceAmount)] = new("Tiền phụ cấp độc hại", IsSensitive: true),
                [nameof(PayrollAllowanceSummaryRecordRow.OtherAllowanceAmount)] = new("Tiền phụ cấp khác", IsSensitive: true),
                [nameof(PayrollAllowanceSummaryRecordRow.IsLocked)] = new("Trạng thái khóa"),
                [nameof(PayrollAllowanceSummaryRecordRow.UpdatedAtUtc)] = new("Thời điểm làm mới/cập nhật")
            });

    private static IReadOnlyDictionary<string, AuditPropertyPolicy> EmployeeTaxDependentProperties() =>
        new ReadOnlyDictionary<string, AuditPropertyPolicy>(
            new Dictionary<string, AuditPropertyPolicy>(StringComparer.Ordinal)
            {
                [nameof(PayrollEmployeeTaxDependentRow.DependentFullName)] = new("Họ và tên người phụ thuộc"),
                [nameof(PayrollEmployeeTaxDependentRow.DependentGender)] = new("Giới tính"),
                [nameof(PayrollEmployeeTaxDependentRow.DependentBirthDate)] = new("Ngày sinh"),
                [nameof(PayrollEmployeeTaxDependentRow.DependentIdentityNumber)] = new("CCCD/CMND người phụ thuộc", IsSensitive: true),
                [nameof(PayrollEmployeeTaxDependentRow.DependentTaxCode)] = new("Mã số thuế người phụ thuộc", IsSensitive: true),
                [nameof(PayrollEmployeeTaxDependentRow.DependentNationality)] = new("Quốc tịch"),
                [nameof(PayrollEmployeeTaxDependentRow.EmployeeTaxCode)] = new("Mã số thuế nhân viên", IsSensitive: true),
                [nameof(PayrollEmployeeTaxDependentRow.EmployeeIdentityNumber)] = new("CCCD/CMND nhân viên", IsSensitive: true),
                [nameof(PayrollEmployeeTaxDependentRow.RelationshipToEmployee)] = new("Quan hệ với nhân viên"),
                [nameof(PayrollEmployeeTaxDependentRow.RegistrationDate)] = new("Ngày đăng ký"),
                [nameof(PayrollEmployeeTaxDependentRow.RegistrationBookNumber)] = new("Sổ đăng ký"),
                [nameof(PayrollEmployeeTaxDependentRow.RegistrationPageNumber)] = new("Trang đăng ký"),
                [nameof(PayrollEmployeeTaxDependentRow.IsFamilyDeductionRegistered)] = new("Trạng thái đăng ký giảm trừ"),
                [nameof(PayrollEmployeeTaxDependentRow.DeductionFromMonth)] = new("Hiệu lực từ tháng"),
                [nameof(PayrollEmployeeTaxDependentRow.DeductionToMonth)] = new("Hiệu lực đến tháng"),
                [nameof(PayrollEmployeeTaxDependentRow.GhiChu)] = new("Ghi chú")
            });

    private static string DescribeCalendarDay(AttendanceWorkCalendarDayRow row) =>
        string.IsNullOrWhiteSpace(row.Name)
            ? row.WorkDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            : $"{row.WorkDate:yyyy-MM-dd} - {row.Name}";

    private static string DescribeOvertimeRegistration(AttendanceOvertimeRegistrationRequestRow row) =>
        $"{row.WorkshopName} - {row.WorkDate:yyyy-MM-dd}";

    private static string DescribeShiftAssignment(AttendanceShiftAssignmentRow row) =>
        $"{row.EmployeeId:D} - {row.WorkDate:yyyy-MM-dd}";
}
