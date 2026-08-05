namespace Vnta.Hrm.Infrastructure.KhauTru.GiamTruGiaCanh;

public sealed class PayrollEmployeeTaxDependentRow
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeTaxCode { get; set; }
    public DateOnly? RegistrationDate { get; set; }
    public string DependentFullName { get; set; } = string.Empty;
    public string? DependentGender { get; set; }
    public DateOnly? DependentBirthDate { get; set; }
    public string? DependentIdentityNumber { get; set; }
    public string? DependentTaxCode { get; set; }
    public string? DependentNationality { get; set; }
    public string? EmployeeIdentityNumber { get; set; }
    public string? RelationshipToEmployee { get; set; }
    public bool IsFamilyDeductionRegistered { get; set; }
    public string? RegistrationBookNumber { get; set; }
    public string? RegistrationPageNumber { get; set; }
    public string? CountryName { get; set; }
    public string? OldWardCode { get; set; }
    public string? OldWardName { get; set; }
    public string? OldDistrictCode { get; set; }
    public string? OldDistrictName { get; set; }
    public string? OldProvinceCode { get; set; }
    public string? OldProvinceName { get; set; }
    public string? NewWardCode { get; set; }
    public string? NewWardName { get; set; }
    public string? NewDistrictCode { get; set; }
    public string? NewDistrictName { get; set; }
    public string? NewProvinceCode { get; set; }
    public string? NewProvinceName { get; set; }
    public DateOnly? DeductionFromMonth { get; set; }
    public DateOnly? DeductionToMonth { get; set; }
    public string? GhiChu { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
