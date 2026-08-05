namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public sealed record AttendanceBiometricDataFilter(
    string? SearchText,
    bool? HasFaceData,
    int? FingerprintQuantity,
    int Take = 1000);
