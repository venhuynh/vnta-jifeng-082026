namespace Vnta.Hrm.Web.Client.Models.Security;

public sealed class EmployeeAccountRecord
{
    public Guid EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? EmployeeEmail { get; set; }

    public string? DepartmentPath { get; set; }

    public string? PositionName { get; set; }

    public bool HasAccount { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? AccountEmail { get; set; }

    public string? ApprovalStatus { get; set; }

    public bool IsActive { get; set; }

    public string? AccessLevel { get; set; }

    public IReadOnlyList<string> RoleNames { get; set; } = [];

    public string FullName =>
        string.Join(
            " ",
            new[] { LastName, FirstName }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim()));

    public string EmployeeLookupText =>
        string.IsNullOrWhiteSpace(EmployeeCode)
            ? FullName
            : $"{EmployeeCode} - {FullName}";

    public string RoleNamesText =>
        RoleNames.Count == 0 ? "Chưa gán" : string.Join(", ", RoleNames);

    public string ApprovalStatusText => ApprovalStatus switch
    {
        null => "Chưa có tài khoản",
        "Draft" => "Nháp",
        "PendingApproval" => "Chờ duyệt",
        "Approved" => "Đã duyệt",
        "Rejected" => "Từ chối",
        "Disabled" => "Ngừng dùng",
        _ => ApprovalStatus
    };

    public string AccountStateText
    {
        get
        {
            if(!HasAccount)
            {
                return "Chưa mở";
            }

            return IsActive ? "Đang kích hoạt" : "Chưa kích hoạt";
        }
    }
}
