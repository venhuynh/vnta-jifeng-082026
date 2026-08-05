namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

#region Snapshot Models

public sealed record PayrollResponsibilityAllowanceGradeConfigDto(
    int Year,
    int Month,
    IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> Grades,
    IReadOnlyList<PayrollResponsibilityAllowanceGradePositionDto> Mappings,
    IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentDto> EmployeeAssignments);

public sealed record PayrollResponsibilityAllowanceGradeDto(
    Guid Id,
    int Year,
    int Month,
    string Code,
    string Name,
    decimal StandardResponsibilityAllowanceAmount,
    int DisplayOrder,
    bool IsActive,
    string? Note);

public sealed record PayrollResponsibilityAllowanceGradePositionDto(
    Guid Id,
    int Year,
    int Month,
    Guid GradeId,
    Guid PositionId,
    string PositionCode,
    string PositionName,
    bool IsActive,
    string? Note,
    DateTime CreatedAtUtc = default,
    DateTime? UpdatedAtUtc = null);

public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentDto(
    Guid Id,
    int Year,
    int Month,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid? PositionId,
    string PositionName,
    Guid? GradeId,
    string? GradeCode,
    string GradeName,
    decimal StandardResponsibilityAllowanceAmount,
    bool IsAssignGradeFromPosition,
    string AssignmentSource,
    string? Note,
    DateTime? UpdatedAtUtc = null);

#endregion

#region Workflow Requests And Results

public sealed record PayrollResponsibilityAllowanceAbcItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? DepartmentName,
    Guid? PositionId,
    string PositionName,
    Guid? GradeId,
    string? GradeCode,
    string GradeName,
    int Year,
    int Month,
    decimal ActualWorkDays,
    decimal StandardWorkDays,
    string AbcRating,
    decimal MonthlyPerformanceBonusAmount,
    bool IsPerformanceBonusExcluded,
    decimal StandardResponsibilityAllowanceAmount,
    decimal ActualResponsibilityAllowanceAmount,
    bool IsLocked,
    DateTime? CalculatedAtUtc,
    string? CalculatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    DateTime? LockedAtUtc,
    string? LockedBy,
    string? Note,
    DateTime CreatedAtUtc);

public sealed record PayrollResponsibilityAllowanceAbcFilter(
    int Year,
    int Month,
    bool? IsLocked = null);

/// <summary>
/// Truy vấn danh sách ABC theo kỳ. Search, nhóm trạng thái và phân trang được
/// thực hiện tại server để UI không phải tải toàn bộ snapshot về circuit.
/// </summary>
public sealed record PayrollResponsibilityAllowanceAbcQuery(
    int Year,
    int Month,
    string? SearchText,
    string? SummaryFilterKey,
    int Skip,
    int Take);

public sealed record PayrollResponsibilityAllowanceAbcSummaryDto(
    int TotalCount,
    int ActiveCount,
    int AbcACount,
    int AbcBCount,
    int AbcCCount,
    int AbcDCount,
    int OpenCount,
    int LockedCount);

public sealed record PayrollResponsibilityAllowanceAbcPageDto(
    IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto> Rows,
    int TotalCount,
    PayrollResponsibilityAllowanceAbcSummaryDto Summary);

/// <summary>Field allowlist cho tệp xuất phụ cấp trách nhiệm.</summary>
public sealed record PayrollResponsibilityAllowanceAbcExportItemDto(
    string EmployeeCode,
    string EmployeeName,
    string? DepartmentName,
    string PositionName,
    string? GradeCode,
    string GradeName,
    decimal ActualWorkDays,
    decimal StandardWorkDays,
    string AbcRating,
    decimal MonthlyPerformanceBonusAmount,
    bool IsPerformanceBonusExcluded,
    decimal StandardResponsibilityAllowanceAmount,
    decimal ActualResponsibilityAllowanceAmount,
    bool IsLocked,
    DateTime? CalculatedAtUtc);

/// <summary>Truy vấn danh sách gán cấp bậc nhân viên theo kỳ tại server.</summary>
public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentQuery(
    int Year,
    int Month,
    string? SearchText,
    string? GradePresenceKey,
    int Skip,
    int Take);

public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto(
    int TotalCount,
    int AssignedCount,
    int UnassignedCount);

public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentPageDto(
    IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentDto> Rows,
    int TotalCount,
    PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto Summary,
    IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> ActiveGrades);

/// <summary>Field allowlist cho xuất danh sách gán cấp bậc nhân viên.</summary>
public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto(
    string EmployeeCode,
    string EmployeeName,
    string PositionName,
    string? GradeCode,
    string GradeName,
    decimal StandardResponsibilityAllowanceAmount,
    string AssignmentSource);

#endregion

#region Update Context Models

public sealed record PayrollResponsibilityAllowanceUpdateContextDto(
    int Year,
    int Month,
    PayrollResponsibilityAllowanceEmployeeSnapshotContextDto EmployeeSnapshot,
    PayrollResponsibilityAllowanceCurrentAbcRecordContextDto? CurrentAbcRecord,
    PayrollResponsibilityAllowanceEmployeeAssignmentContextDto? EmployeeAssignment,
    PayrollResponsibilityAllowancePositionGradeMappingContextDto? PositionGradeMapping,
    PayrollResponsibilityAllowanceContextGradeDto? ManualGrade,
    PayrollResponsibilityAllowanceContextGradeDto? PositionDefaultGrade,
    PayrollResponsibilityAllowanceValidWorkdayContextDto ValidWorkdaySummary,
    PayrollResponsibilityAllowanceSalaryRateContextDto SalaryRate,
    PayrollResponsibilityAllowanceSelectedSourceContextDto SelectedSource,
    PayrollResponsibilityAllowanceCalculationPreviewDto CalculationPreview,
    PayrollResponsibilityAllowanceUpdateImpactDto UpdateImpact,
    IReadOnlyList<PayrollResponsibilityAllowanceContextGradeDto> AvailableGrades);

public sealed record PayrollResponsibilityAllowanceEmployeeSnapshotContextDto(
    string TableName,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? DepartmentName,
    Guid? PositionId,
    string PositionName);

public sealed record PayrollResponsibilityAllowanceCurrentAbcRecordContextDto(
    string TableName,
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid? PositionId,
    string PositionName,
    Guid? GradeId,
    string? GradeCode,
    string GradeName,
    int Year,
    int Month,
    decimal ActualWorkDays,
    decimal StandardWorkDays,
    string AbcRating,
    decimal MonthlyPerformanceBonusAmount,
    bool IsPerformanceBonusExcluded,
    decimal StandardResponsibilityAllowanceAmount,
    decimal ActualResponsibilityAllowanceAmount,
    bool IsLocked,
    DateTime? CalculatedAtUtc,
    string? CalculatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    DateTime? LockedAtUtc,
    string? LockedBy,
    string? Note);

public sealed record PayrollResponsibilityAllowanceEmployeeAssignmentContextDto(
    string TableName,
    Guid Id,
    int Year,
    int Month,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid? PositionId,
    string PositionName,
    Guid? GradeId,
    string? GradeCode,
    string GradeName,
    decimal StandardResponsibilityAllowanceAmount,
    string AssignmentSource,
    string? Note);

public sealed record PayrollResponsibilityAllowancePositionGradeMappingContextDto(
    string TableName,
    Guid Id,
    int Year,
    int Month,
    Guid GradeId,
    Guid PositionId,
    string PositionCode,
    string PositionName,
    bool IsActive,
    string? Note);

public sealed record PayrollResponsibilityAllowanceContextGradeDto(
    string TableName,
    Guid Id,
    int Year,
    int Month,
    string Code,
    string Name,
    decimal StandardResponsibilityAllowanceAmount,
    int DisplayOrder,
    bool IsActive,
    string? Note);

public sealed record PayrollResponsibilityAllowanceValidWorkdayContextDto(
    string TableName,
    string DayTypeCondition,
    IReadOnlyList<string> StatusCodes,
    decimal ValidWorkdays);

public sealed record PayrollResponsibilityAllowanceSalaryRateContextDto(
    string TableName,
    bool Exists,
    decimal StandardWorkDays);

public sealed record PayrollResponsibilityAllowanceSelectedSourceContextDto(
    string SourceKey,
    string SourceLabel,
    Guid? GradeId,
    string? GradeCode,
    string GradeName,
    decimal StandardResponsibilityAllowanceAmount);

public sealed record PayrollResponsibilityAllowanceCalculationPreviewDto(
    string AbcRating,
    decimal AbcMultiplier,
    decimal MonthlyPerformanceBonusAmount,
    bool IsPerformanceBonusExcluded,
    decimal EffectivePerformanceBonusAmount,
    decimal ValidWorkdays,
    decimal StandardWorkDays,
    decimal AbsentDays,
    decimal WorkdayRatio,
    decimal StandardResponsibilityAllowanceAmount,
    decimal ActualResponsibilityAllowanceAmount,
    string Formula);

public sealed record PayrollResponsibilityAllowanceUpdateImpactDto(
    string TargetTableName,
    bool WillInsert,
    bool WillUpdate,
    bool SkippedBecauseLocked,
    bool AmountWouldChange,
    string Message);

#endregion
