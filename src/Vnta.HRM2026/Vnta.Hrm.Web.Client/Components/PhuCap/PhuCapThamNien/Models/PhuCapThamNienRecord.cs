namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>
/// Đại diện một dòng dữ liệu phụ cấp thâm niên của nhân viên trong một kỳ lương.
/// </summary>
public sealed class PhuCapThamNienRecord
{
    #region Định danh và thông tin nhân viên

    /// <summary>Định danh duy nhất của dòng dữ liệu hiển thị.</summary>
    public Guid Id { get; set; }

    /// <summary>Định danh bản ghi tổng hợp phụ cấp lương dùng cho các thao tác cập nhật.</summary>
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    /// <summary>Định danh nhân viên sở hữu dòng phụ cấp.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Mã nhân viên tại thời điểm lập dữ liệu kỳ lương.</summary>
    public string? EmployeeCode { get; set; }

    /// <summary>Họ và tên nhân viên tại thời điểm lập dữ liệu kỳ lương.</summary>
    public string? EmployeeName { get; set; }

    /// <summary>Tên phòng ban của nhân viên.</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Tên chức vụ của nhân viên.</summary>
    public string? PositionName { get; set; }

    #endregion

    #region Dữ liệu kỳ lương và phụ cấp

    /// <summary>Tháng của kỳ lương áp dụng cho dòng phụ cấp.</summary>
    public short PayrollMonth { get; set; }

    /// <summary>Năm của kỳ lương áp dụng cho dòng phụ cấp.</summary>
    public short PayrollYear { get; set; }

    /// <summary>Ngày bắt đầu làm việc dùng để xác định thâm niên.</summary>
    public DateTime? EmploymentStartDate { get; set; }

    /// <summary>Số năm thâm niên hoàn thành tại thời điểm tính phụ cấp.</summary>
    public short? CompletedSeniorityYears { get; set; }

    /// <summary>Số tháng thâm niên lẻ sau số năm hoàn thành.</summary>
    public short? CompletedSeniorityMonths { get; set; }

    /// <summary>Số Công HC được xác định từ các mã kết quả công có cờ Phụ cấp thâm niên.</summary>
    public decimal? AdministrativeWorkDays { get; set; }

    /// <summary>Số ngày quy đổi từ tổng phút đi trễ và về sớm trong kỳ.</summary>
    public decimal? LateEarlyLeaveWorkDays { get; set; }

    /// <summary>Số ngày công tính lương của nhân viên trong kỳ.</summary>
    public decimal? SalaryWorkDays { get; set; }

    /// <summary>Khóa quy tắc được áp dụng để xác định mức phụ cấp.</summary>
    public string? AppliedRuleKey { get; set; }

    /// <summary>Số tiền phụ cấp thâm niên của nhân viên trong kỳ.</summary>
    public decimal AllowanceAmount { get; set; }

    /// <summary>Ghi chú nghiệp vụ đi kèm bản ghi phụ cấp.</summary>
    public string? Note { get; set; }

    #endregion

    #region Trạng thái và lịch sử làm mới

    /// <summary>Cho biết bản ghi đã bị khóa, không được chỉnh sửa hoặc tính lại trực tiếp.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Cho biết dòng phụ cấp tổng hợp cha đã khóa và bảo vệ mọi thao tác khóa/mở khóa của dòng con.</summary>
    public bool IsSummaryLocked { get; set; }

    /// <summary>Thời điểm UTC gần nhất bản ghi được làm mới.</summary>
    public DateTime? RefreshedAtUtc { get; set; }

    /// <summary>Tài khoản hoặc người dùng thực hiện lần làm mới gần nhất.</summary>
    public string? RefreshedBy { get; set; }

    /// <summary>Version token returned by the server for optimistic concurrency.</summary>
    public DateTime UpdatedAtUtc { get; set; }

    #endregion

    #region Thuộc tính hiển thị tính toán

    /// <summary>Chuỗi hiển thị kết hợp mã và tên nhân viên.</summary>
    public string EmployeeDisplay
    {
        get
        {
            var parts = new[]
            {
                string.IsNullOrWhiteSpace(EmployeeCode) ? null : EmployeeCode.Trim(),
                string.IsNullOrWhiteSpace(EmployeeName) ? null : EmployeeName.Trim()
            };

            return string.Join(" - ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }

    /// <summary>Tên phòng ban đã chuẩn hóa để hiển thị, hoặc giá trị thay thế khi trống.</summary>
    public string DepartmentDisplay => string.IsNullOrWhiteSpace(DepartmentName) ? "--" : DepartmentName.Trim();

    /// <summary>Tên chức vụ đã chuẩn hóa để hiển thị, hoặc giá trị thay thế khi trống.</summary>
    public string PositionDisplay => string.IsNullOrWhiteSpace(PositionName) ? "--" : PositionName.Trim();

    /// <summary>Chuỗi thâm niên theo định dạng số năm và số tháng.</summary>
    public string SeniorityDisplay
    {
        get
        {
            if(!CompletedSeniorityYears.HasValue)
            {
                return "--";
            }

            var years = CompletedSeniorityYears.Value;
            var months = CompletedSeniorityMonths ?? 0;
            return $"{years} năm {months} tháng";
        }
    }

    /// <summary>Nhãn tiếng Việt tương ứng với khóa quy tắc phụ cấp đã áp dụng.</summary>
    public string AppliedRuleDisplay => AppliedRuleKey switch
    {
        "blocked-salary-work" => "Công tính lương <= 5",
        "13-plus" => "Từ 13 năm trở lên",
        "10-13" => "Từ 10 đến dưới 13 năm",
        "6-10" => "Từ 6 đến dưới 10 năm",
        "3-6" => "Từ 3 đến dưới 6 năm",
        "1-3" => "Từ 1 đến dưới 3 năm",
        _ => "Chưa đủ điều kiện"
    };

    /// <summary>Nhãn trạng thái khóa dùng để hiển thị trên lưới.</summary>
    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";

    /// <summary>Nhãn hành động đổi trạng thái khóa phù hợp với trạng thái hiện tại.</summary>
    public string LockActionText => IsLocked ? "Mở khóa" : "Khóa";

    #endregion
}


