namespace Vnta.PostgresSync.Console.Services;

public enum CommandMode
{
    InteractiveMenu,
    SyncAll,
    SyncMasterData,
    SyncBiometricSourceData,
    SyncAttendanceDaily,
    SyncAttendanceLogs,
    SyncAttendanceWorkdaySummaries,
    SyncPayrollBasicSalary,
    SyncPayrollOtherAllowance,
    SyncPayrollInsuranceDeduction,
    SyncPayrollResponsibilityAllowance,
    SyncFamilyDeduction,
    Inspect
}
