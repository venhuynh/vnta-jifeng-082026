namespace Vnta.Hrm.Infrastructure.NhanSu.NhanVien;

public sealed class AttendanceGatewayEmployeeRow
{
    public Guid Id { get; set; }

    public Guid PositionId { get; set; }

    public int Status { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Avatar { get; set; }

    public DateTime HireDate { get; set; }

    /// <summary>Ngày bắt đầu được tính thâm niên sau khi nhân viên chính thức.</summary>
    public DateTime? SeniorityStartDate { get; set; }

    /// <summary>Ngày nghỉ việc, chỉ có giá trị khi tình trạng là nghỉ việc.</summary>
    public DateTime? ResignedDate { get; set; }

    public Guid DepartmentId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<AttendanceBiometricDataRow> BiometricDataRows { get; set; } = [];

    public ICollection<AttendanceDeviceUserProfileRow> DeviceUserProfiles { get; set; } = [];

    public ICollection<AttendanceFingerprintTemplateRow> FingerprintTemplates { get; set; } = [];

    public ICollection<AttendanceBioPhotoRow> BioPhotos { get; set; } = [];

    public ICollection<AttendanceUserPictureRow> UserPictures { get; set; } = [];
}
