using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("oplog")]
public sealed class ZktecoOpLog
{
    [Key]
    public int Id { get; set; }

    [MaxLength(500)]
    public string? Operator { get; set; }

    public DateTime? OpTime { get; set; }

    [MaxLength(500)]
    public string? OpType { get; set; }

    [MaxLength(50)]
    public string? User { get; set; }

    [MaxLength(500)]
    public string? Obj1 { get; set; }

    [MaxLength(500)]
    public string? Obj2 { get; set; }

    [MaxLength(500)]
    public string? Obj3 { get; set; }

    [MaxLength(500)]
    public string? Obj4 { get; set; }

    [MaxLength(500)]
    public string? DeviceId { get; set; }
}
