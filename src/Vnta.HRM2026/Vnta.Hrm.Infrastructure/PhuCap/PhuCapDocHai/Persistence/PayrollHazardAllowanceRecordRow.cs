namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

public sealed class PayrollHazardAllowanceRecordRow
{
    #region Định danh aggregate

    // Khóa một-một tới dòng tổng hợp payroll, đồng thời là phạm vi lock và đồng bộ tiền phụ cấp downstream.
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    #endregion

    #region Snapshot số liệu phụ cấp độc hại

    // Số dòng ngày thường có mã trạng thái được cấu hình hưởng phụ cấp độc hại trong kỳ lương.
    public decimal QualifiedWorkdayCount { get; set; }

    // Số ngày khấu trừ được quy đổi từ tổng phút đi trễ và về sớm theo định mức 480 phút/ngày.
    public decimal LateEarlyDeductionDays { get; set; }

    // CTL: Công HC - số ngày đi trễ/về sớm quy đổi.
    public decimal PayableWorkdayCount { get; set; }

    // Quy tắc A/B/C không dùng đơn giá theo ngày; cột legacy này được ghi 0 để không biểu diễn sai công thức hiện hành.
    public decimal HazardAllowancePerDay { get; set; }

    // Số tiền snapshot = Công tính lương × 7.700 VND; bằng 0 khi không đủ điều kiện hưởng.
    public decimal HazardAllowanceAmount { get; set; }

    // Kết quả kiểm tra đường dẫn tổ chức với danh sách bộ phận bị loại trừ tại thời điểm tạo snapshot.
    public bool IsEligibleDepartment { get; set; }

    // Trạng thái hưởng phụ cấp do nghiệp vụ xác lập. Tính lại vẫn cập nhật công và mức tiền,
    // nhưng luôn giữ trạng thái này để ngoại lệ do người dùng chọn không bị ghi đè.
    public bool IsEligibleForAllowance { get; set; }

    // Lý do nghiệp vụ khi IsEligibleDepartment là false; null khi nhân viên đủ điều kiện.
    public string? ExclusionReason { get; set; }

    // Trạng thái khóa riêng của snapshot phụ cấp độc hại; không dùng cờ khóa của summary dùng chung.
    public bool IsLocked { get; set; }

    #endregion

    #region Dấu vết tạo và cập nhật

    // Thời điểm tạo snapshot ban đầu, lưu theo timestamp không timezone của dữ liệu nghiệp vụ HRM.
    public DateTime CreatedAtUtc { get; set; }

    // Principal hoặc tác nhân hệ thống đã tạo snapshot; giá trị mặc định tránh cột audit không khởi tạo.
    public string CreatedBy { get; set; } = string.Empty;

    // Thời điểm thay đổi snapshot gần nhất; null khi bản ghi chưa từng được cập nhật sau khi tạo.
    public DateTime? UpdatedAtUtc { get; set; }

    // Principal hoặc tác nhân hệ thống thực hiện lần cập nhật gần nhất.
    public string? UpdatedBy { get; set; }

    #endregion
}
