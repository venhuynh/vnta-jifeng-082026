using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("face_templates")]
public sealed class ZktecoFaceTemplate
{
    [Key]
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeviceSn { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Fid { get; set; } = string.Empty;

    public int? Size { get; set; }

    [MaxLength(20)]
    public string? Valid { get; set; }

    [Required]
    public string TemplateData { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Version { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
