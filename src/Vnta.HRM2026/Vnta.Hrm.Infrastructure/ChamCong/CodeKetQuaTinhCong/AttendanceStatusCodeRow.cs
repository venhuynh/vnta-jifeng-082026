namespace Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;

public sealed class AttendanceStatusCodeRow
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public bool CongTangCa { get; set; }

    /// <summary>
    /// Xác định mã kết quả công được phép đóng góp một công hành chính.
    /// </summary>
    public bool CongHanhChinh { get; set; }

    public bool PhuCapTrachNhiemTinhNangSuat { get; set; }

    public bool PhuCapDocHai { get; set; }

    public bool PhuCapTrachNhiemKhac { get; set; }

    public bool PhuCapPhepLe { get; set; }

    public bool PhuCapTrachNhiemKhongTinhNangSuat { get; set; }

    /// <summary>
    /// Xác định mã kết quả công có được tính vào công chuyên cần (CTL) hay không.
    /// </summary>
    public bool PhuCapChuyenCan { get; set; }

    public bool PhuCapThamNien { get; set; }

    public bool KhauTruTamUng { get; set; }

    public bool IsActive { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
