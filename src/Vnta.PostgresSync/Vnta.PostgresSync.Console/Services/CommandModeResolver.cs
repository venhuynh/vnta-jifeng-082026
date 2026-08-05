namespace Vnta.PostgresSync.Console.Services;

public static class CommandModeResolver
{
    public static CommandMode Resolve(string[] args)
    {
        if (args.Length == 0)
        {
            return CommandMode.InteractiveMenu;
        }

        var rawCommand = args[0].Trim().ToLowerInvariant();
        return rawCommand switch
        {
            "inspect" or "--inspect" => CommandMode.Inspect,
            "sync-master" or "--sync-master" => CommandMode.SyncMasterData,
            "sync-biometric" or "--sync-biometric" => CommandMode.SyncBiometricSourceData,
            "sync-attendance" or "--sync-attendance" => CommandMode.SyncAttendanceDaily,
            "sync-attendance-logs" or "--sync-attendance-logs" => CommandMode.SyncAttendanceLogs,
            "sync-attendance-workday-summaries" or "--sync-attendance-workday-summaries" => CommandMode.SyncAttendanceWorkdaySummaries,
            "sync-basic-salary" or "--sync-basic-salary" => CommandMode.SyncPayrollBasicSalary,
            "sync-other-allowance" or "--sync-other-allowance" => CommandMode.SyncPayrollOtherAllowance,
            "sync-insurance-deduction" or "--sync-insurance-deduction" => CommandMode.SyncPayrollInsuranceDeduction,
            "sync-responsibility-allowance" or "--sync-responsibility-allowance" => CommandMode.SyncPayrollResponsibilityAllowance,
            "sync-family-deduction" or "--sync-family-deduction" => CommandMode.SyncFamilyDeduction,
            "sync-all" or "--sync-all" => CommandMode.SyncAll,
            _ => CommandMode.SyncAll
        };
    }
}
