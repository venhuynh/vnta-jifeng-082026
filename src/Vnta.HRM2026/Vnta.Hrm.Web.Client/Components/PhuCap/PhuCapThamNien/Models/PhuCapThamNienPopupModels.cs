namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Đại diện kiểu <c>PhuCapThamNienEditModel</c> phục vụ giao diện phụ cấp thâm niên.</summary>
public sealed class PhuCapThamNienEditModel
{
    /// <summary>Giá trị <c>PayrollAllowanceSummaryRecordId</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    /// <summary>Giá trị <c>EmployeeDisplay</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    public string EmployeeDisplay { get; set; } = string.Empty;

    /// <summary>Giá trị <c>AllowanceAmount</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    public decimal AllowanceAmount { get; set; }

    /// <summary>Giá trị <c>Note</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    public string? Note { get; set; }

    /// <summary>Giá trị <c>IsLocked</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    public bool IsLocked { get; set; }

    public DateTime OriginalUpdatedAtUtc { get; set; }
}
