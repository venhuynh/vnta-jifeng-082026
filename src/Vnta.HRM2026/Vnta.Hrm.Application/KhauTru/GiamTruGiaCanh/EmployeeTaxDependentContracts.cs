namespace Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;

public sealed record EmployeeTaxDependentDto(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeTaxCode,
    DateOnly? RegistrationDate,
    string DependentFullName,
    string? DependentGender,
    DateOnly? DependentBirthDate,
    string? DependentIdentityNumber,
    string? DependentTaxCode,
    string? DependentNationality,
    string? EmployeeIdentityNumber,
    string? RelationshipToEmployee,
    bool IsFamilyDeductionRegistered,
    string? RegistrationBookNumber,
    string? RegistrationPageNumber,
    string? CountryName,
    string? OldWardCode,
    string? OldWardName,
    string? OldDistrictCode,
    string? OldDistrictName,
    string? OldProvinceCode,
    string? OldProvinceName,
    string? NewWardCode,
    string? NewWardName,
    string? NewDistrictCode,
    string? NewDistrictName,
    string? NewProvinceCode,
    string? NewProvinceName,
    DateOnly? DeductionFromMonth,
    DateOnly? DeductionToMonth,
    string? GhiChu,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record SaveEmployeeTaxDependentRequest(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeTaxCode,
    DateOnly? RegistrationDate,
    string DependentFullName,
    string? DependentGender,
    DateOnly? DependentBirthDate,
    string? DependentIdentityNumber,
    string? DependentTaxCode,
    string? DependentNationality,
    string? EmployeeIdentityNumber,
    string? RelationshipToEmployee,
    bool IsFamilyDeductionRegistered,
    string? RegistrationBookNumber,
    string? RegistrationPageNumber,
    string? CountryName,
    string? OldWardCode,
    string? OldWardName,
    string? OldDistrictCode,
    string? OldDistrictName,
    string? OldProvinceCode,
    string? OldProvinceName,
    string? NewWardCode,
    string? NewWardName,
    string? NewDistrictCode,
    string? NewDistrictName,
    string? NewProvinceCode,
    string? NewProvinceName,
    DateOnly? DeductionFromMonth,
    DateOnly? DeductionToMonth,
    string? GhiChu,
    DateTime? OriginalUpdatedAtUtc,
    string? Actor);

/// <summary>
/// Bộ lọc danh sách hồ sơ người phụ thuộc dùng cho màn Giảm trừ gia cảnh.
/// </summary>
public sealed record EmployeeTaxDependentFilter(
    string? SearchText,
    bool? IsFamilyDeductionRegistered,
    int Skip = 0,
    int Take = 50);

/// <summary>
/// Dòng danh sách, bao gồm thông tin định danh nhân viên để hiển thị trên lưới.
/// </summary>
public sealed record EmployeeTaxDependentListItemDto(
    EmployeeTaxDependentDto Dependent,
    string? EmployeeCode,
    string? EmployeeName);

public sealed record EmployeeTaxDependentPageDto(
    IReadOnlyList<EmployeeTaxDependentListItemDto> Items,
    int TotalCount);

public interface IEmployeeTaxDependentService
{
    Task<IReadOnlyList<EmployeeTaxDependentDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeTaxDependentPageDto> SearchAsync(
        EmployeeTaxDependentFilter filter,
        CancellationToken cancellationToken = default);

    Task<EmployeeTaxDependentDto> SaveAsync(SaveEmployeeTaxDependentRequest request, CancellationToken cancellationToken = default);
}
