namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public sealed record ChiTietNhanVienFilter(
    string? SearchText,
    int Take = 100);
