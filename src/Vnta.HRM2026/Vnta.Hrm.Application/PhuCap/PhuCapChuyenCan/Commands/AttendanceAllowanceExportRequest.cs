using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>Requests export of an entire authorized period; client filters and selections are intentionally not accepted.</summary>
public sealed record AttendanceAllowanceExportRequest(int PayrollYear, int PayrollMonth, AttendanceAllowanceExportFormat Format);
