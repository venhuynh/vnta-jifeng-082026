namespace Vnta.Hrm.Web.Client.Models.NhanSu.ChiTietNhanVien;

public enum ChiTietNhanVienEmploymentStatus
{
    Probation = 1,
    Official = 2,
    Resigned = 5
}

public sealed class ChiTietNhanVienRecord
{
    public Guid Id { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AvatarDataUrl { get; set; }

    public DateTime HireDate { get; set; }

    public Guid DepartmentId { get; set; }

    public string? DepartmentCode { get; set; }

    public string? DepartmentName { get; set; }

    public string? DepartmentPath { get; set; }

    public Guid PositionId { get; set; }

    public string? PositionCode { get; set; }

    public string? PositionName { get; set; }

    public int Status { get; set; }

    public DateTime? SeniorityStartDate { get; set; }

    public DateTime? ResignedDate { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string FullName =>
        string.Join(
            " ",
            new[] { LastName, FirstName }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim()));

    public string LookupText =>
        string.IsNullOrWhiteSpace(EmployeeCode)
            ? FullName
            : string.IsNullOrWhiteSpace(FullName)
                ? EmployeeCode
                : $"{EmployeeCode} - {FullName}";

    public ChiTietNhanVienEmploymentStatus? EmploymentStatus =>
        Enum.IsDefined(typeof(ChiTietNhanVienEmploymentStatus), Status)
            ? (ChiTietNhanVienEmploymentStatus)Status
            : null;

    public string StatusText => EmploymentStatus switch
    {
        ChiTietNhanVienEmploymentStatus.Probation => "Thử việc",
        ChiTietNhanVienEmploymentStatus.Official => "Chính thức",
        ChiTietNhanVienEmploymentStatus.Resigned => "Nghỉ việc",
        _ => $"Trạng thái {Status}"
    };
}
