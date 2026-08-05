using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.NhanSu.ChiTietNhanVien;

public sealed class DatabaseChiTietNhanVienService(
    IEmployeeService employeeService,
    ApplicationDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider) : IChiTietNhanVienService
{
    private const string CitizenIdentityProtectionPurpose = "Vnta.Hrm.NhanSu.ChiTietNhanVien.CitizenIdentity.v1";

    public async Task<IReadOnlyList<ChiTietNhanVienDto>> SearchAsync(ChiTietNhanVienFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await employeeService.SearchAsync(new EmployeeFilter(filter.SearchText, Take: filter.Take), cancellationToken);
        return rows.Select(MapDto).ToArray();
    }

    public async Task<ChiTietNhanVienDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await employeeService.GetByIdAsync(id, cancellationToken);
        return row is null ? null : MapDto(row);
    }

    public async Task<EmployeeContactProfileDto?> GetContactProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.EmployeeContactProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.EmployeeId == employeeId, cancellationToken);
        return row is null ? null : MapContactProfile(row);
    }

    public async Task<EmployeeContactProfileDto> UpsertContactProfileAsync(UpsertEmployeeContactProfileRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeExistsAsync(request.EmployeeId, cancellationToken);
        ValidateContactProfile(request);
        var row = await dbContext.EmployeeContactProfiles.SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId, cancellationToken);
        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        if(row is null)
        {
            row = new EmployeeContactProfileRow { EmployeeId = request.EmployeeId, CreatedAtUtc = now };
            dbContext.EmployeeContactProfiles.Add(row);
        }
        else
        {
            EnsureConcurrency(row.UpdatedAtUtc ?? row.CreatedAtUtc, request.OriginalUpdatedAtUtc);
            row.UpdatedAtUtc = now;
        }

        row.PersonalEmail = Normalize(request.PersonalEmail);
        row.PersonalPhoneNumber = Normalize(request.PersonalPhoneNumber);
        row.PermanentAddress = Normalize(request.PermanentAddress);
        row.CurrentAddress = Normalize(request.CurrentAddress);
        row.EmergencyContactName = Normalize(request.EmergencyContactName);
        row.EmergencyContactRelationship = Normalize(request.EmergencyContactRelationship);
        row.EmergencyContactPhoneNumber = Normalize(request.EmergencyContactPhoneNumber);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapContactProfile(row);
    }

    public async Task<CitizenIdentityDto?> GetCitizenIdentityAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.EmployeeCitizenIdentities.AsNoTracking().SingleOrDefaultAsync(x => x.EmployeeId == employeeId, cancellationToken);
        return row is null ? null : MapCitizenIdentity(row);
    }

    public async Task<CitizenIdentityDto> UpsertCitizenIdentityAsync(UpsertCitizenIdentityRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeExistsAsync(request.EmployeeId, cancellationToken);
        ValidateCitizenIdentityDates(request);
        var row = await dbContext.EmployeeCitizenIdentities.SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId, cancellationToken);
        var normalizedNumber = NormalizeCitizenIdentityNumber(request.CitizenIdentityNumber);
        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        if(row is null)
        {
            if(string.IsNullOrWhiteSpace(normalizedNumber)) throw new InvalidOperationException("Số căn cước công dân không được để trống.");
            row = new CitizenIdentityRow { EmployeeId = request.EmployeeId, CreatedAtUtc = now };
            dbContext.EmployeeCitizenIdentities.Add(row);
        }
        else
        {
            EnsureConcurrency(row.UpdatedAtUtc ?? row.CreatedAtUtc, request.OriginalUpdatedAtUtc);
            row.UpdatedAtUtc = now;
        }

        if(!string.IsNullOrWhiteSpace(normalizedNumber))
        {
            var numberHash = ComputeCitizenIdentityHash(normalizedNumber);
            var isDuplicated = await dbContext.EmployeeCitizenIdentities.AnyAsync(item => item.EmployeeId != request.EmployeeId && item.CitizenIdentityNumberHash == numberHash, cancellationToken);
            if(isDuplicated) throw new InvalidOperationException("Số căn cước công dân đã được sử dụng cho nhân viên khác.");
            row.CitizenIdentityNumberHash = numberHash;
            row.CitizenIdentityNumberCiphertext = dataProtectionProvider.CreateProtector(CitizenIdentityProtectionPurpose).Protect(normalizedNumber);
        }

        row.IssuedDate = request.IssuedDate;
        row.IssuedPlace = Normalize(request.IssuedPlace);
        row.ExpiryDate = request.ExpiryDate;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCitizenIdentity(row);
    }

    public async Task<ChiTietNhanVienDto> CreateAsync(CreateChiTietNhanVienRequest request, CancellationToken cancellationToken = default)
    {
        var row = await employeeService.CreateAsync(new CreateEmployeeRequest(request.EmployeeCode, request.FullName, request.DepartmentId, request.PositionId, request.Status, request.HireDate), cancellationToken);
        return MapDto(row);
    }

    public async Task<ChiTietNhanVienDto> UpdateAsync(UpdateChiTietNhanVienRequest request, CancellationToken cancellationToken = default)
    {
        var row = await employeeService.UpdateAsync(new UpdateEmployeeRequest(request.Id, request.EmployeeCode, request.FullName, request.DepartmentId, request.PositionId, request.Status, request.HireDate, request.OriginalUpdatedAtUtc, request.SeniorityStartDate, request.ResignedDate, UpdateEmploymentDates: true), cancellationToken);
        return MapDto(row);
    }

    public Task DeleteAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => employeeService.DeleteAsync(ids, cancellationToken);

    private async Task EnsureEmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if(employeeId == Guid.Empty || !await dbContext.Employees.AsNoTracking().AnyAsync(employee => employee.Id == employeeId && !employee.IsDeleted, cancellationToken)) throw new InvalidOperationException("Nhân viên không tồn tại hoặc không còn khả dụng.");
    }

    private static void ValidateContactProfile(UpsertEmployeeContactProfileRequest request)
    {
        var personalEmail = Normalize(request.PersonalEmail);
        if(!string.IsNullOrWhiteSpace(personalEmail) && !new EmailAddressAttribute().IsValid(personalEmail)) throw new InvalidOperationException("Email cá nhân không hợp lệ.");
        ValidateMaximumLength(request.PersonalEmail, 256, "Email cá nhân");
        ValidateMaximumLength(request.PersonalPhoneNumber, 30, "Điện thoại cá nhân");
        ValidateMaximumLength(request.EmergencyContactName, 150, "Họ tên liên hệ khẩn cấp");
        ValidateMaximumLength(request.EmergencyContactRelationship, 100, "Quan hệ liên hệ khẩn cấp");
        ValidateMaximumLength(request.EmergencyContactPhoneNumber, 30, "Điện thoại liên hệ khẩn cấp");
        var hasEmergencyContact = !string.IsNullOrWhiteSpace(request.EmergencyContactName) || !string.IsNullOrWhiteSpace(request.EmergencyContactRelationship) || !string.IsNullOrWhiteSpace(request.EmergencyContactPhoneNumber);
        if(hasEmergencyContact && (string.IsNullOrWhiteSpace(request.EmergencyContactName) || string.IsNullOrWhiteSpace(request.EmergencyContactPhoneNumber))) throw new InvalidOperationException("Liên hệ khẩn cấp phải có họ tên và số điện thoại.");
    }

    private static void ValidateCitizenIdentityDates(UpsertCitizenIdentityRequest request)
    {
        ValidateMaximumLength(request.IssuedPlace, 250, "Nơi cấp căn cước công dân");
        if(request.IssuedDate is { } issuedDate && issuedDate > DateOnly.FromDateTime(DateTime.Today)) throw new InvalidOperationException("Ngày cấp căn cước công dân không được sau ngày hiện tại.");
        if(request.IssuedDate is { } fromDate && request.ExpiryDate is { } expiryDate && expiryDate < fromDate) throw new InvalidOperationException("Ngày hết hạn không được trước ngày cấp.");
    }

    private static void EnsureConcurrency(DateTime currentTimestamp, DateTime? originalTimestamp)
    {
        if(originalTimestamp.HasValue && currentTimestamp != originalTimestamp.Value) throw new InvalidOperationException("Hồ sơ đã được cập nhật bởi người dùng khác. Vui lòng tải lại dữ liệu.");
    }

    private static string NormalizeCitizenIdentityNumber(string? value)
    {
        if(string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = new string(value.Where(char.IsDigit).ToArray());
        if(normalized.Length != 12) throw new InvalidOperationException("Số căn cước công dân phải có đúng 12 chữ số.");
        return normalized;
    }

    private static string ComputeCitizenIdentityHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string MaskCitizenIdentityNumber(string value) => value.Length <= 4 ? value : string.Concat(new string('*', value.Length - 4), value[^4..]);
    private static DateTime ToDatabaseTimestamp(DateTime value)
    {
        const long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1_000;
        var ticks = value.Ticks - value.Ticks % ticksPerMicrosecond;
        return new DateTime(ticks, DateTimeKind.Unspecified);
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ValidateMaximumLength(string? value, int maximumLength, string fieldName)
    {
        if(Normalize(value)?.Length > maximumLength) throw new InvalidOperationException($"{fieldName} không được vượt quá {maximumLength} ký tự.");
    }
    private static EmployeeContactProfileDto MapContactProfile(EmployeeContactProfileRow row) => new(row.EmployeeId, row.PersonalEmail, row.PersonalPhoneNumber, row.PermanentAddress, row.CurrentAddress, row.EmergencyContactName, row.EmergencyContactRelationship, row.EmergencyContactPhoneNumber, row.CreatedAtUtc, row.UpdatedAtUtc);
    private CitizenIdentityDto MapCitizenIdentity(CitizenIdentityRow row)
    {
        var number = dataProtectionProvider.CreateProtector(CitizenIdentityProtectionPurpose).Unprotect(row.CitizenIdentityNumberCiphertext);
        return new CitizenIdentityDto(row.EmployeeId, true, MaskCitizenIdentityNumber(number), row.IssuedDate, row.IssuedPlace, row.ExpiryDate, row.CreatedAtUtc, row.UpdatedAtUtc);
    }
    private static ChiTietNhanVienDto MapDto(EmployeeListItemDto source) => new(source.Id, source.EmployeeCode, source.FirstName, source.LastName, source.Email, source.PhoneNumber, source.AvatarDataUrl, source.HireDate, source.DepartmentId, source.DepartmentCode, source.DepartmentName, source.DepartmentPath, source.PositionId, source.PositionCode, source.PositionName, source.Status, source.SeniorityStartDate, source.ResignedDate, source.CreatedAtUtc, source.UpdatedAtUtc);
}
