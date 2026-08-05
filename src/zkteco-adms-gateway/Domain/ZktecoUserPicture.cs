using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("user_pictures")]
public sealed class ZktecoUserPicture
{
    [Key]
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeviceSn { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    public int? Size { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
