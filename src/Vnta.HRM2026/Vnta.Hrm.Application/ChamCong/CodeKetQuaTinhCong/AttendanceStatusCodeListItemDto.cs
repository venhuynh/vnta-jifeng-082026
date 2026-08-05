namespace Vnta.Hrm.Application.ChamCong.CodeKetQuaTinhCong;

public sealed record AttendanceStatusCodeListItemDto(
    Guid Id,
    string Code,
    string Name,
    string Kind,
    bool CongTangCa,
    bool CongHanhChinh,
    bool PhuCapTrachNhiemTinhNangSuat,
    bool PhuCapDocHai,
    bool PhuCapTrachNhiemKhac,
    bool PhuCapPhepLe,
    bool PhuCapTrachNhiemKhongTinhNangSuat,
    bool PhuCapChuyenCan,
    bool PhuCapThamNien,
    bool KhauTruTamUng,
    bool IsActive,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
