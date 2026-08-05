using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Vnta.AttendanceGateway.Domain;

[Table("departments")]
public sealed class ZktecoDepartment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CenterName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DepartmentOrWorkshopName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TeamName { get; set; }

    [MaxLength(200)]
    public string? GroupName { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [NotMapped]
    public string Name => GroupName ?? TeamName ?? DepartmentOrWorkshopName;

    [NotMapped]
    public string FullPath => string.Join(" / ", new[] { CenterName, DepartmentOrWorkshopName, TeamName, GroupName }
        .Where(x => !string.IsNullOrWhiteSpace(x)));

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
