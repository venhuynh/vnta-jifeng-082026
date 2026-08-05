namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;

/// <summary>
/// Đại diện cho dữ liệu phụ cấp thâm niên của <b>một</b> dòng tổng hợp phụ cấp trong kỳ lương.
/// Bản ghi này lưu lại snapshot đã tính tại thời điểm làm mới để báo cáo của kỳ cũ
/// không bị thay đổi khi hồ sơ nhân sự hoặc dữ liệu chấm công phát sinh sau đó.
/// </summary>
public sealed class PayrollEmployeeSeniorityAllowanceRow
{
    /// <summary>Khóa chính, đồng thời liên kết 1-1 đến dòng tổng hợp phụ cấp của nhân viên trong kỳ.</summary>
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    /// <summary>Ngày bắt đầu dùng để tính thâm niên: ngày bắt đầu thâm niên nếu có, nếu không là ngày vào làm.</summary>
    public DateOnly? EmploymentStartDate { get; set; }

    /// <summary>Số năm thâm niên đã hoàn tất tại ngày cuối cùng của kỳ lương.</summary>
    public short? CompletedSeniorityYears { get; set; }

    /// <summary>Số tháng lẻ sau số năm thâm niên đã hoàn tất, từ 0 đến 11.</summary>
    public short? CompletedSeniorityMonths { get; set; }

    /// <summary>Tổng công hành chính đủ điều kiện phụ cấp thâm niên trong kỳ.</summary>
    public decimal? AdministrativeWorkDays { get; set; }

    /// <summary>Số công quy đổi từ tổng phút đi trễ và về sớm; 480 phút tương ứng một công.</summary>
    public decimal? LateEarlyLeaveWorkDays { get; set; }

    /// <summary>Công tính lương dùng để xét phụ cấp, bằng Công HC trừ công đi trễ/về sớm.</summary>
    public decimal? SalaryWorkDays { get; set; }

    /// <summary>Mã bậc quy tắc đã áp dụng, ví dụ <c>1-3</c>, <c>6-10</c> hoặc <c>blocked-salary-work</c>.</summary>
    public string? AppliedRuleKey { get; set; }

    /// <summary>Số tiền phụ cấp thâm niên cuối cùng của dòng, tính bằng đồng.</summary>
    public decimal AllowanceAmount { get; set; }

    /// <summary>Ghi chú do người dùng nhập khi điều chỉnh thủ công; để <c>null</c> nếu không có.</summary>
    public string? Note { get; set; }

    /// <summary><c>true</c> khi dữ liệu đã chốt và không cho phép làm mới hay sửa thủ công.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Thời điểm gần nhất hệ thống tính lại snapshot thâm niên.</summary>
    public DateTime? RefreshedAtUtc { get; set; }

    /// <summary>Tác nhân thực hiện lần tính lại gần nhất.</summary>
    public string? RefreshedBy { get; set; }

    /// <summary>Thời điểm tạo bản ghi chi tiết.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Tác nhân tạo bản ghi; mặc định là chuỗi rỗng để tránh giá trị <c>null</c>.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Thời điểm cập nhật gần nhất, gồm cả cập nhật thủ công, khóa/mở khóa và làm mới.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Tác nhân thực hiện lần cập nhật gần nhất.</summary>
    public string? UpdatedBy { get; set; }
}
