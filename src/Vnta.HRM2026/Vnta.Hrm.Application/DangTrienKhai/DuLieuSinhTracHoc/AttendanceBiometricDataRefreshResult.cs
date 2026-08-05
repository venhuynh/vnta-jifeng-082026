namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public sealed record AttendanceBiometricDataRefreshResult(
    int TotalEmployees,
    int Inserted,
    int Updated,
    int ProfilesInserted,
    int ProfilesUpdated,
    int ProfilesDeleted,
    int EmployeesWithFingerprints,
    int EmployeesWithFaceData,
    DateTime RefreshedAtUtc,
    string FingerprintSource,
    string FaceSource);
