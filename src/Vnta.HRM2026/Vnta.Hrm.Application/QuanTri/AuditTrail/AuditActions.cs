namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Stable, server-defined action names for the audit-trail pilot.
/// Browser input must never supply these values.
/// </summary>
public static class AuditActions
{
    public static class Shift
    {
        public const string Save = "Shift.Save";
        public const string Created = "Shift.Created";
        public const string Updated = "Shift.Updated";
    }

    public static class WorkCalendarDay
    {
        public const string Save = "WorkCalendarDay.Save";
        public const string Delete = "WorkCalendarDay.Delete";
        public const string EnsureSundayDaysOff = "WorkCalendarDay.EnsureSundayDaysOff";
        public const string Created = "WorkCalendarDay.Created";
        public const string Updated = "WorkCalendarDay.Updated";
        public const string Deleted = "WorkCalendarDay.Deleted";
    }

    public static class OvertimeRegistration
    {
        public const string Save = "OvertimeRegistration.Save";
        public const string ChangeStatus = "OvertimeRegistration.ChangeStatus";
        public const string DraftCreated = "OvertimeRegistration.DraftCreated";
        public const string Created = "OvertimeRegistration.Created";
        public const string Updated = "OvertimeRegistration.Updated";
        public const string Submitted = "OvertimeRegistration.Submitted";
        public const string Approved = "OvertimeRegistration.Approved";
        public const string Returned = "OvertimeRegistration.Returned";
        public const string Rejected = "OvertimeRegistration.Rejected";
    }

    public static class ShiftAssignment
    {
        public const string ManualSave = "ShiftAssignment.ManualSave";
        public const string BatchGenerate = "ShiftAssignment.BatchGenerate";
        public const string ManualCreated = "ShiftAssignment.ManualCreated";
        public const string ManualUpdated = "ShiftAssignment.ManualUpdated";
        public const string BatchGenerated = "ShiftAssignment.BatchGenerated";
    }

    public static class NhanVien
    {
        public const string Create = "NhanVien.Create";
        public const string Update = "NhanVien.Update";
        public const string Delete = "NhanVien.Delete";
        public const string ChangeStatus = "NhanVien.ChangeStatus";
        public const string RefreshFromAttendance = "NhanVien.RefreshFromAttendance";
    }

    public static class ResponsibilityPositionAssignment
    {
        public const string Create = "ResponsibilityPositionAssignment.Create";
        public const string Update = "ResponsibilityPositionAssignment.Update";
        public const string Deactivate = "ResponsibilityPositionAssignment.Deactivate";
        public const string CopyFromPreviousPeriod = "ResponsibilityPositionAssignment.CopyFromPreviousPeriod";
    }

    public static class SeniorityAllowance
    {
        public const string PreparePeriod = "SeniorityAllowance.PreparePeriod";
        public const string Refresh = "SeniorityAllowance.Refresh";
        public const string ManualValueUpdated = "SeniorityAllowance.ManualValueUpdated";
        public const string LockStateChanged = "SeniorityAllowance.LockStateChanged";
        public const string BatchLockStateChanged = "SeniorityAllowance.BatchLockStateChanged";
    }

    public static class HazardAllowance
    {
        public const string Refreshed = "HazardAllowance.Refreshed";
        public const string ManualValuesUpdated = "HazardAllowance.ManualValuesUpdated";
        public const string EntitlementBatchUpdated = "HazardAllowance.EntitlementBatchUpdated";
        public const string LockStateChanged = "HazardAllowance.LockStateChanged";
        public const string BatchLockStateChanged = "HazardAllowance.BatchLockStateChanged";
    }

    public static class MealAllowance
    {
        public const string Refreshed = "MealAllowance.Refreshed";
        public const string ManualValuesUpdated = "MealAllowance.ManualValuesUpdated";
        public const string BatchLockStateChanged = "MealAllowance.BatchLockStateChanged";
    }

    public static class OtherAllowance
    {
        public const string Created = "OtherAllowance.Created";
        public const string SyncedFromPreviousMonth = "OtherAllowance.SyncedFromPreviousMonth";
        public const string Updated = "OtherAllowance.Updated";
        public const string LockStateChanged = "OtherAllowance.LockStateChanged";
        public const string Deleted = "OtherAllowance.Deleted";
    }

    public static class EmployeeTaxDependent
    {
        public const string Saved = "EmployeeTaxDependent.Saved";
    }

    public static class OtherDeduction
    {
        public const string ManualValueUpdated = "OtherDeduction.ManualValueUpdated";
    }

    public static class PersonalIncomeTaxDeduction
    {
        public const string ManualValueUpdated = "PersonalIncomeTaxDeduction.ManualValueUpdated";
        public const string Refreshed = "PersonalIncomeTaxDeduction.Refreshed";
        public const string BatchLockStateChanged = "PersonalIncomeTaxDeduction.BatchLockStateChanged";
    }

    public static class PayrollInsuranceDeduction
    {
        public const string ManualValuesUpdated = "PayrollInsuranceDeduction.ManualValuesUpdated";
        public const string Refresh = "PayrollInsuranceDeduction.Refresh";
        public const string SyncedFromPreviousMonth = "PayrollInsuranceDeduction.SyncedFromPreviousMonth";
        public const string Created = "PayrollInsuranceDeduction.Created";
        public const string Deleted = "PayrollInsuranceDeduction.Deleted";
        public const string LockStateChanged = "PayrollInsuranceDeduction.LockStateChanged";
        public const string BatchLockStateChanged = "PayrollInsuranceDeduction.BatchLockStateChanged";
    }

    public static class ResponsibilityAllowance
    {
        public const string Mutation = "ResponsibilityAllowance.Mutation";
        public const string BatchLockStateChanged = "ResponsibilityAllowance.BatchLockStateChanged";
        public const string Exported = "ResponsibilityAllowance.Exported";
    }

    public static class OtherResponsibilityAllowance
    {
        public const string PeriodPrepared = "OtherResponsibilityAllowance.PeriodPrepared";
        public const string Recalculated = "OtherResponsibilityAllowance.Recalculated";
        public const string BatchLockStateChanged = "OtherResponsibilityAllowance.BatchLockStateChanged";
    }

    public static class AllowanceSummary
    {
        public const string SyncFromPreviousMonth = "AllowanceSummary.SyncFromPreviousMonth";
        public const string Refreshed = "AllowanceSummary.Refreshed";
        public const string Deleted = "AllowanceSummary.Deleted";
        public const string ManualValuesUpdated = "AllowanceSummary.ManualValuesUpdated";
        public const string LockStateChanged = "AllowanceSummary.LockStateChanged";
        public const string BatchLockStateChanged = "AllowanceSummary.BatchLockStateChanged";
        public const string Exported = "AllowanceSummary.Exported";
    }

    public static class AttendanceAllowance
    {
        public const string Exported = "AttendanceAllowance.Exported";
        public const string Save = "AttendanceAllowance.Save";
        public const string Delete = "AttendanceAllowance.Delete";
        public const string Refresh = "AttendanceAllowance.Refresh";
        public const string SyncFromPreviousMonth = "AttendanceAllowance.SyncFromPreviousMonth";
        public const string SetLockState = "AttendanceAllowance.SetLockState";
        public const string SetLockStateBatch = "AttendanceAllowance.SetLockStateBatch";
    }

    public static class UnionFeeDeduction
    {
        public const string PeriodPrepared = "UnionFeeDeduction.PeriodPrepared";
        public const string Refreshed = "UnionFeeDeduction.Refreshed";
        public const string ManualValueUpdated = "UnionFeeDeduction.ManualValueUpdated";
        public const string SetLockState = "UnionFeeDeduction.SetLockState";
        public const string SetLockStateBatch = "UnionFeeDeduction.SetLockStateBatch";
    }

    public static class LeaveHolidayAllowance
    {
        public const string PreparePeriod = "LeaveHolidayAllowance.PreparePeriod";
        public const string ClearManualValues = "LeaveHolidayAllowance.ClearManualValues";
        public const string SyncFromPreviousMonth = "LeaveHolidayAllowance.SyncFromPreviousMonth";
        public const string Recalculate = "LeaveHolidayAllowance.Recalculate";
        public const string ManualValuesUpdated = "LeaveHolidayAllowance.ManualValuesUpdated";
        public const string LockStateChanged = "LeaveHolidayAllowance.LockStateChanged";
        public const string BatchLockStateChanged = "LeaveHolidayAllowance.BatchLockStateChanged";
    }

    public static class DeductionSummary
    {
        public const string SyncFromPreviousMonth = "DeductionSummary.SyncFromPreviousMonth";
        public const string ManualOtherDeductionUpdated = "DeductionSummary.ManualOtherDeductionUpdated";
        public const string Refreshed = "DeductionSummary.Refreshed";
        public const string PeriodRecalculated = "DeductionSummary.PeriodRecalculated";
        public const string Exported = "DeductionSummary.Exported";
        public const string LockStateChanged = "DeductionSummary.LockStateChanged";
        public const string BatchLockStateChanged = "DeductionSummary.BatchLockStateChanged";
    }
}
