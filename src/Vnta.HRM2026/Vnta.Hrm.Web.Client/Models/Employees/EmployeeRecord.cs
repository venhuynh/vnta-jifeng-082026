namespace Vnta.Hrm.Web.Client.Models.Employees;

public enum EmployeeEmploymentStatus
{
    Probation = 1,
    Official = 2,
    Resigned = 5
}

public sealed class EmployeeRecord
{
    public Guid Id { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AvatarDataUrl { get; set; }

    public DateTime HireDate { get; set; }

    public DateTime? SeniorityStartDate { get; set; }

    public DateTime? ResignedDate { get; set; }

    public Guid DepartmentId { get; set; }

    public string? DepartmentCode { get; set; }

    public string? DepartmentName { get; set; }

    public string? DepartmentPath { get; set; }

    public Guid PositionId { get; set; }

    public string? PositionCode { get; set; }

    public string? PositionName { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string FullName =>
        string.Join(
            " ",
            new[] { LastName, FirstName }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim()));

    public string EmployeeLookupText
    {
        get
        {
            var fullName = string.IsNullOrWhiteSpace(FullName)
                ? EmployeeCode
                : FullName;

            return string.IsNullOrWhiteSpace(EmployeeCode)
                ? fullName
                : $"{EmployeeCode} - {fullName}";
        }
    }

    public EmployeeEmploymentStatus? EmploymentStatus => Enum.IsDefined(typeof(EmployeeEmploymentStatus), Status)
        ? (EmployeeEmploymentStatus)Status
        : null;

    public string StatusText => EmploymentStatus switch
    {
        EmployeeEmploymentStatus.Probation => "Thử việc",
        EmployeeEmploymentStatus.Official => "Chính thức",
        EmployeeEmploymentStatus.Resigned => "Nghỉ việc",
        _ => $"Trạng thái {Status}"
    };
}
