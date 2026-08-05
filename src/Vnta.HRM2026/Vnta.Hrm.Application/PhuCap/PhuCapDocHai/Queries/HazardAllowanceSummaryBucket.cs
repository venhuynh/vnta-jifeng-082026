namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Nhóm tổng hợp được áp dụng trực tiếp tại truy vấn server của màn Phụ cấp độc hại.
/// </summary>
public enum HazardAllowanceSummaryBucket
{
    /// <summary>Không giới hạn theo badge.</summary>
    All = 0,
    /// <summary>Chỉ nhân viên thuộc bộ phận đủ điều kiện hưởng.</summary>
    Eligible = 1,
    /// <summary>Chỉ nhân viên có công hợp lệ nhưng bị loại trừ theo bộ phận.</summary>
    Exception = 2,
    /// <summary>Chỉ snapshot đã khóa tại summary payroll.</summary>
    Locked = 3,
    /// <summary>Chỉ snapshot đang mở để có thể thao tác.</summary>
    Open = 4
}
