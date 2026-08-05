using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("biodata")]
public sealed class ZktecoBioData
{
    [Key]
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeviceSn { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Pin { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? BioNo { get; set; }

    [MaxLength(20)]
    public string? BioIndex { get; set; }

    [MaxLength(20)]
    public string? Valid { get; set; }

    [MaxLength(20)]
    public string? Duress { get; set; }

    [MaxLength(20)]
    public string? BioType { get; set; }

    [MaxLength(20)]
    public string? MajorVersion { get; set; }

    [MaxLength(20)]
    public string? MinorVersion { get; set; }

    [MaxLength(20)]
    public string? Format { get; set; }

    [Required]
    public string TemplateData { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
