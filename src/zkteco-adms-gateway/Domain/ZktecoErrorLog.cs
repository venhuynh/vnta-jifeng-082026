using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("errorlog")]
public sealed class ZktecoErrorLog
{
    [Key]
    public int Id { get; set; }

    public string? ErrCode { get; set; }

    public string? ErrMsg { get; set; }

    public string? DataOrigin { get; set; }

    [MaxLength(100)]
    public string? CmdId { get; set; }

    public string? Additional { get; set; }

    [MaxLength(50)]
    public string? DeviceId { get; set; }
}
