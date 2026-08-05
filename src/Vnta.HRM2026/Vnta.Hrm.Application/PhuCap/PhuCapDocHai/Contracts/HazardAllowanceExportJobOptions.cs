namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public sealed class HazardAllowanceExportJobOptions
{
    public const string SectionName = "Payroll:HazardAllowance:ExportJobs";

    public string OutputDirectory { get; set; } = "App_Data/hazard-allowance-exports";

    public int RetentionHours { get; set; } = 24;

    public int PollIntervalSeconds { get; set; } = 5;

    public int RunningJobTimeoutMinutes { get; set; } = 30;
}
