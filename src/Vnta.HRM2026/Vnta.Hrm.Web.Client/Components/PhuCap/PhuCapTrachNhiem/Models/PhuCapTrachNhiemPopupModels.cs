using Vnta.Hrm.Web.Client.Models.Employees;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Đại diện kiểu <c>GradeFormModel</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed class GradeFormModel
{
    /// <summary>Giá trị <c>Code</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Giá trị <c>Name</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Giá trị <c>StandardResponsibilityAllowanceAmount</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public decimal StandardResponsibilityAllowanceAmount { get; set; }
    /// <summary>Giá trị <c>DisplayOrder</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Giá trị <c>IsActive</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Giá trị <c>Note</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Thực hiện xử lý cho luồng <c>CreateDefault</c>.</summary>
    public static GradeFormModel CreateDefault() => new();
}

/// <summary>Đại diện kiểu <c>MappingFormModel</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed class MappingFormModel
{
    /// <summary>Giá trị <c>PositionIdText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string PositionIdText { get; set; } = string.Empty;
    /// <summary>Giá trị <c>GradeIdText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string GradeIdText { get; set; } = string.Empty;
    /// <summary>Giá trị <c>IsActive</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Giá trị <c>Note</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Thực hiện xử lý cho luồng <c>CreateDefault</c>.</summary>
    public static MappingFormModel CreateDefault() => new();
}

/// <summary>Đại diện kiểu <c>EmployeeAssignmentEditorModel</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed class EmployeeAssignmentEditorModel
{
    /// <summary>Giá trị <c>GradeIdText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string GradeIdText { get; set; } = string.Empty;
    /// <summary>Giá trị <c>Note</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string Note { get; set; } = string.Empty;
    /// <summary>Giá trị <c>AssignmentSource</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string AssignmentSource { get; set; } = string.Empty;
}

/// <summary>Đại diện kiểu <c>EmployeeAssignmentEditorRow</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed record EmployeeAssignmentEditorRow(
    EmployeeRecord Employee,
    EmployeeAssignmentEditorModel Editor,
    bool IsLocked);

/// <summary>Đại diện kiểu <c>AdjustmentFormModel</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed class AdjustmentFormModel
{
    /// <summary>Giá trị <c>GradeIdText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string GradeIdText { get; set; } = string.Empty;
    /// <summary>Giá trị <c>IsActive</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public bool IsActive { get; set; }
    /// <summary>Giá trị <c>MonthlyPerformanceBonusAmount</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public decimal MonthlyPerformanceBonusAmount { get; set; }
    /// <summary>Giá trị <c>IsPerformanceBonusExcluded</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public bool IsPerformanceBonusExcluded { get; set; }
    /// <summary>Giá trị <c>Note</c> được sử dụng bởi màn hình phụ cấp trách nhiệm.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Thực hiện xử lý cho luồng <c>CreateDefault</c>.</summary>
    public static AdjustmentFormModel CreateDefault() => new();
}

/// <summary>Đại diện kiểu <c>CalculationDetailRow</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed record CalculationDetailRow(
    string Label,
    string Value,
    string Description);
