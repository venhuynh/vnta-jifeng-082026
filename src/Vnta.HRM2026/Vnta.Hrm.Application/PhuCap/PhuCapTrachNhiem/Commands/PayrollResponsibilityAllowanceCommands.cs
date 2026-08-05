namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

// CQRS command contracts. Derived allowance amounts are deliberately absent; handlers calculate them from policy/source data.
public sealed record SavePayrollResponsibilityAllowanceGradeRequest(Guid? Id, int Year, int Month, string Code, string Name, decimal StandardResponsibilityAllowanceAmount, int DisplayOrder, bool IsActive, string? Note);
public sealed record SavePayrollResponsibilityAllowanceGradePositionRequest(Guid? Id, int Year, int Month, Guid GradeId, Guid PositionId, bool IsActive, string? Note);
public sealed record SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest(Guid? Id, int Year, int Month, Guid EmployeeId, Guid? GradeId, string? Note);
public sealed record UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest(Guid Id, int Year, int Month, Guid EmployeeId, Guid GradeId, string? Note, DateTime? OriginalUpdatedAtUtc);
public sealed record RefreshPayrollResponsibilityAllowanceAbcRequest(int Year, int Month, Guid? EmployeeId, DateTime? OriginalUpdatedAtUtc = null);
public sealed record SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest(int Year, int Month, bool IsLocked, IReadOnlyList<Guid>? EmployeeIds = null, IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? ConcurrencyTokens = null);
public sealed record PayrollResponsibilityAllowanceAbcConcurrencyToken(Guid EmployeeId, DateTime OriginalUpdatedAtUtc);
public sealed record SavePayrollResponsibilityAllowanceAdjustmentRequest(Guid? EmployeeAssignmentId, int Year, int Month, Guid EmployeeId, Guid? GradeId, bool IsActive, string? Note, decimal MonthlyPerformanceBonusAmount, bool IsPerformanceBonusExcluded, DateTime? OriginalUpdatedAtUtc = null);
public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest(int Year, int Month, string Format);
public sealed record PayrollResponsibilityAllowanceAbcExportRequest(int Year, int Month, string Format);

public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult(int Year, int Month, int TotalEmployees, int Updated);
public sealed record RefreshPayrollResponsibilityAllowanceAbcResult(int Year, int Month, int TotalEmployees, int Inserted, int Updated, int SkippedLocked, int SkippedMissingSource = 0);
public sealed record CalculatePayrollResponsibilityAllowanceAbcResult(int Year, int Month, int TotalRows, int Updated, int SkippedLocked, int RatedA, int RatedB, int RatedC, int RatedD);
public sealed record RecalculatePayrollResponsibilityAllowanceAbcResult(int Year, int Month, RefreshPayrollResponsibilityAllowanceAbcResult Refresh, CalculatePayrollResponsibilityAllowanceAbcResult Calculate);
public sealed record SetPayrollResponsibilityAllowanceAbcBatchLockStateResult(int Year, int Month, int TargetRowCount, int UpdatedCount);
public sealed record PayrollResponsibilityAllowanceConfigCopyResult(int Year, int Month, bool CopyMappings, int CreatedCount, int SkippedCount);
public sealed record CopyPayrollResponsibilityAllowanceAbcFromPreviousResult(int Year, int Month, int PreviousYear, int PreviousMonth, int CurrentWorkInputRows, int CopiedFromPreviousRows, int Inserted, int Updated, int InitializedWithoutPrevious, int SkippedLocked);
public sealed record UpdatePayrollResponsibilityPerformanceBonusForPeriodResult(int Year, int Month, int TotalRows, int Updated, int SkippedLocked, int PerformanceBonusExcludedRows);
public sealed record UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult(PayrollResponsibilityAllowanceEmployeeAssignmentDto Assignment, RefreshPayrollResponsibilityAllowanceAbcResult Refresh);
