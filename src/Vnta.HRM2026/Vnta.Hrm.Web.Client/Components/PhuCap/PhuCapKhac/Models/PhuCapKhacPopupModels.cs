namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;

/// <summary>Draft độc lập cho biểu mẫu điều chỉnh phụ cấp khác.</summary>
public sealed class PhuCapKhacEditModel
{
    public Guid Id { get; set; }
    public Guid PayrollAllowanceSummaryRecordId { get; set; }
    public string EmployeeDisplay { get; set; } = string.Empty;
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public string PayrollPeriodDisplay { get; set; } = string.Empty;
    public string AllowanceName { get; set; } = string.Empty;
    public bool IsFixedAmount { get; set; }
    public decimal AllowanceAmount { get; set; }
    public string? Note { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? OriginalUpdatedAtUtc { get; set; }
}

/// <summary>Nhân viên có bản ghi tổng hợp phụ cấp hợp lệ để thêm phụ cấp khác.</summary>
public sealed record PhuCapKhacEmployeeOption(
    Guid PayrollAllowanceSummaryRecordId,
    string EmployeeDisplay);
