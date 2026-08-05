using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models;

public sealed class AttendanceStatusCodeRecord
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Kind { get; set; } = string.Empty;

    public bool CongTangCa { get; set; }

    public bool CongHanhChinh { get; set; }

    public bool PhuCapTrachNhiemTinhNangSuat { get; set; }

    public bool PhuCapDocHai { get; set; }

    public bool PhuCapTrachNhiemKhac { get; set; }

    public bool PhuCapPhepLe { get; set; }

    public bool PhuCapTrachNhiemKhongTinhNangSuat { get; set; }

    public bool PhuCapChuyenCan { get; set; }

    public bool PhuCapThamNien { get; set; }

    public bool KhauTruTamUng { get; set; }

    public bool IsActive { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
