using System.ComponentModel.DataAnnotations;
using Vnta.Hrm.Application.Common;

namespace Vnta.Hrm.Web.Client.Models.Attendance;

public sealed class AttendanceBiometricDataRecord
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    [StringLength(200)]
    public string? EmployeeName { get; set; }

    public string? Avatar { get; set; }

    [StringLength(200)]
    public string? DepartmentName { get; set; }

    [StringLength(200)]
    public string? PositionName { get; set; }

    public int FpQty { get; set; }

    public bool HasFaceData { get; set; }

    public DateTime LastUpdated { get; set; }

    [StringLength(255)]
    public string? CardNumber { get; set; }

    public bool IsAdmin { get; set; }

    public bool HasPassword { get; set; }

    public string? AvatarImageSrc => AvatarImageSourceHelper.NormalizeSource(Avatar);

    public string EmployeeDisplay
    {
        get
        {
            var parts = new[] { EmployeeCode, EmployeeName }
                .Where(static part => !string.IsNullOrWhiteSpace(part))
                .Select(static part => part!.Trim())
                .ToArray();

            return parts.Length == 0 ? "--" : string.Join(" - ", parts);
        }
    }

    public string FingerprintStatusText => FpQty switch
    {
        <= 0 => "Không có vân tay",
        1 => "1 vân tay",
        _ => $"{FpQty} vân tay"
    };

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
