using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

#region Grade Configuration Rows

/// <summary>
/// Bản ghi persistence của một bậc phụ cấp trách nhiệm áp dụng cho một kỳ lương.
/// Mức tiền chuẩn tại đây là dữ liệu nguồn để tạo hoặc làm mới snapshot ABC.
/// </summary>
public sealed class PayrollResponsibilityAllowanceGradeRow
{
    /// <summary>Khóa chính ổn định của bậc trong kho dữ liệu.</summary>
    public Guid Id { get; set; }

    /// <summary>Năm của kỳ lương mà cấu hình bậc có hiệu lực.</summary>
    public int Year { get; set; }

    /// <summary>Tháng của kỳ lương mà cấu hình bậc có hiệu lực.</summary>
    public int Month { get; set; }

    /// <summary>Mã bậc được chuẩn hóa, dùng nhận diện cấu hình trong cùng kỳ.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên hiển thị của bậc trách nhiệm.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Số tiền phụ cấp chuẩn trước khi áp dụng ABC và thưởng hiệu suất.</summary>
    public decimal StandardResponsibilityAllowanceAmount { get; set; }

    /// <summary>Thứ tự trình bày bậc trên giao diện cấu hình.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Cho biết bậc còn được phép làm nguồn áp dụng hay không.</summary>
    public bool IsActive { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Ánh xạ một chức vụ vào một bậc trách nhiệm trong kỳ lương. Đây là nguồn mặc
/// định, được dùng khi nhân viên chưa có điều chỉnh/gán riêng.
/// </summary>
public sealed class PayrollResponsibilityAllowanceGradePositionRow
{
    /// <summary>Khóa chính của mapping trong kỳ.</summary>
    public Guid Id { get; set; }

    /// <summary>Năm hiệu lực của mapping.</summary>
    public int Year { get; set; }

    /// <summary>Tháng hiệu lực của mapping.</summary>
    public int Month { get; set; }

    /// <summary>Bậc trách nhiệm được gán làm mặc định.</summary>
    public Guid GradeId { get; set; }

    /// <summary>Chức vụ nhận bậc mặc định này.</summary>
    public Guid PositionId { get; set; }

    /// <summary>Cho biết mapping còn được dùng khi resolve nguồn mặc định.</summary>
    public bool IsActive { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}

#endregion

#region Employee Assignment Rows

/// <summary>
/// Cấu hình phụ cấp trách nhiệm cho đúng một dòng Phụ cấp tổng hợp. Kỳ lương và
/// nhân viên được xác định qua Summary; mức tiền luôn được lấy từ bậc được gán.
/// </summary>
public sealed class PayrollResponsibilityAllowanceEmployeeAssignmentRow
{
    /// <summary>Khóa chính của assignment.</summary>
    public Guid Id { get; set; }

    /// <summary>Khóa ngoại một-một tới dòng Phụ cấp tổng hợp.</summary>
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    /// <summary>Bậc trách nhiệm được chọn; rỗng nghĩa là chưa gán bậc.</summary>
    public Guid? GradeId { get; set; }

    /// <summary><c>true</c> khi bậc hiện tại được nhận từ quy tắc theo chức vụ.</summary>
    public bool IsAssignGradeFromPosition { get; set; } = true;

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}

#endregion

#region Monthly ABC Rows

/// <summary>
/// Snapshot phụ cấp trách nhiệm/ABC của một nhân viên trong một kỳ lương. Đây là
/// dòng vận hành được khóa, điều chỉnh, tính lại và đồng bộ sang bảng tổng hợp.
/// </summary>
public sealed class PayrollResponsibilityAllowanceAbcRow
{
    /// <summary>Khóa chính của snapshot ABC.</summary>
    public Guid Id { get; set; }

    /// <summary>Khóa ngoại tới summary phụ cấp nhận tiền thực tế sau khi đồng bộ.</summary>
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    /// <summary>Nhân viên sở hữu snapshot trong kỳ.</summary>
    public Guid EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }

    public Guid? PositionId { get; set; }

    public string PositionName { get; set; } = string.Empty;

    public Guid? GradeId { get; set; }

    public string? GradeCode { get; set; }

    public string GradeName { get; set; } = string.Empty;

    /// <summary>Năm kỳ lương của snapshot.</summary>
    public int Year { get; set; }

    /// <summary>Tháng kỳ lương của snapshot.</summary>
    public int Month { get; set; }

    /// <summary>Công hợp lệ lấy từ chấm công, sau khi trừ quy đổi đi muộn/về sớm.</summary>
    public decimal ActualWorkDays { get; set; }

    /// <summary>Công chuẩn của kỳ lấy từ hồ sơ lương cơ bản.</summary>
    public decimal StandardWorkDays { get; set; }

    /// <summary>Kết quả xếp loại A/B/C/D hoặc NA được suy ra từ công chuẩn và công thực tế.</summary>
    public string AbcRating { get; set; } = string.Empty;

    /// <summary>Hệ số/thưởng hiệu suất tháng do nghiệp vụ nhập để tính tiền phụ cấp.</summary>
    public decimal MonthlyPerformanceBonusAmount { get; set; }

    /// <summary>Nếu đúng, tiền thực tế bằng tiền chuẩn và không nhân thưởng hiệu suất.</summary>
    public bool IsPerformanceBonusExcluded { get; set; }

    /// <summary>Mức phụ cấp chuẩn snapshot từ bậc hoặc điều chỉnh của nhân viên.</summary>
    public decimal StandardResponsibilityAllowanceAmount { get; set; }

    /// <summary>Số tiền sau khi áp dụng công thức ABC; được đồng bộ sang summary khi mở khóa.</summary>
    public decimal ActualResponsibilityAllowanceAmount { get; set; }

    /// <summary>Khóa nghiệp vụ: các luồng cập nhật/tính lại phải giữ nguyên snapshot này.</summary>
    public bool IsLocked { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Thời điểm gần nhất hệ thống áp công thức ABC và tiền thực tế.</summary>
    public DateTime? CalculatedAtUtc { get; set; }

    public string? CalculatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    /// <summary>Thời điểm khóa nghiệp vụ, null khi dòng đang mở.</summary>
    public DateTime? LockedAtUtc { get; set; }

    public string? LockedBy { get; set; }

    public string? Note { get; set; }
}

#endregion
