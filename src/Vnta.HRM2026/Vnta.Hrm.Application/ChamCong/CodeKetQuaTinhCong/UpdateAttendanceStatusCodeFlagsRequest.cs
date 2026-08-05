namespace Vnta.Hrm.Application.ChamCong.CodeKetQuaTinhCong;

public sealed record UpdateAttendanceStatusCodeFlagsRequest(
    Guid Id,
    DateTime OriginalUpdatedAtUtc,
    bool CongHanhChinh,
    bool PhuCapTrachNhiemTinhNangSuat,
    bool PhuCapDocHai,
    bool PhuCapTrachNhiemKhac,
    bool PhuCapPhepLe,
    bool PhuCapTrachNhiemKhongTinhNangSuat,
    bool PhuCapChuyenCan,
    bool PhuCapThamNien,
    bool KhauTruTamUng);
