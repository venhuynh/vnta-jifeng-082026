using System.ComponentModel.DataAnnotations;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.GiamTruGiaCanh;

public sealed class GiamTruGiaCanhEditModel : IValidatableObject
{
    #region Dữ liệu biểu mẫu

    public Guid Id { get; init; }

    [Required(ErrorMessage = "Vui lòng chọn nhân viên.")]
    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "Tên người phụ thuộc là bắt buộc.")]
    [StringLength(4000)]
    public string DependentFullName { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? EmployeeTaxCode { get; set; }
    public DateOnly? RegistrationDate { get; set; }
    [StringLength(4000)]
    public string? DependentGender { get; set; }
    public DateOnly? DependentBirthDate { get; set; }
    [StringLength(4000)] public string? DependentIdentityNumber { get; set; }
    [StringLength(4000)] public string? DependentTaxCode { get; set; }
    [StringLength(4000)] public string? DependentNationality { get; set; }
    [StringLength(4000)] public string? EmployeeIdentityNumber { get; set; }
    [StringLength(4000)] public string? RelationshipToEmployee { get; set; }
    public bool IsFamilyDeductionRegistered { get; set; } = true;
    [StringLength(128)] public string? RegistrationBookNumber { get; set; }
    [StringLength(4000)] public string? RegistrationPageNumber { get; set; }
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
    [StringLength(4000)]
    public string? GhiChu { get; set; }
    public DateTime? OriginalUpdatedAtUtc { get; init; }

    #endregion

    #region Chuyển đổi và validation

    public static GiamTruGiaCanhEditModel From(EmployeeTaxDependentDto source) => new()
    {
        Id = source.Id, EmployeeId = source.EmployeeId, EmployeeTaxCode = source.EmployeeTaxCode,
        RegistrationDate = source.RegistrationDate, DependentFullName = source.DependentFullName,
        DependentGender = source.DependentGender, DependentBirthDate = source.DependentBirthDate,
        DependentIdentityNumber = source.DependentIdentityNumber, DependentTaxCode = source.DependentTaxCode,
        DependentNationality = source.DependentNationality, EmployeeIdentityNumber = source.EmployeeIdentityNumber,
        RelationshipToEmployee = source.RelationshipToEmployee,
        IsFamilyDeductionRegistered = source.IsFamilyDeductionRegistered,
        RegistrationBookNumber = source.RegistrationBookNumber, RegistrationPageNumber = source.RegistrationPageNumber,
        CountryName = source.CountryName, OldWardCode = source.OldWardCode, OldWardName = source.OldWardName,
        OldDistrictCode = source.OldDistrictCode, OldDistrictName = source.OldDistrictName,
        OldProvinceCode = source.OldProvinceCode, OldProvinceName = source.OldProvinceName,
        NewWardCode = source.NewWardCode, NewWardName = source.NewWardName,
        NewDistrictCode = source.NewDistrictCode, NewDistrictName = source.NewDistrictName,
        NewProvinceCode = source.NewProvinceCode, NewProvinceName = source.NewProvinceName,
        DeductionFromMonth = source.DeductionFromMonth, DeductionToMonth = source.DeductionToMonth,
        GhiChu = source.GhiChu, OriginalUpdatedAtUtc = source.UpdatedAtUtc ?? source.CreatedAtUtc
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeductionFromMonth.HasValue
            && DeductionToMonth.HasValue
            && DeductionToMonth.Value < DeductionFromMonth.Value)
        {
            yield return new ValidationResult(
                "Tháng kết thúc giảm trừ không được trước tháng bắt đầu.",
                [nameof(DeductionFromMonth), nameof(DeductionToMonth)]);
        }
    }

    public SaveEmployeeTaxDependentRequest ToRequest() => new(
        Id, EmployeeId, EmployeeTaxCode, RegistrationDate, DependentFullName, DependentGender,
        DependentBirthDate, DependentIdentityNumber, DependentTaxCode, DependentNationality,
        EmployeeIdentityNumber, RelationshipToEmployee, IsFamilyDeductionRegistered,
        RegistrationBookNumber, RegistrationPageNumber, CountryName, OldWardCode, OldWardName,
        OldDistrictCode, OldDistrictName, OldProvinceCode, OldProvinceName, NewWardCode, NewWardName,
        NewDistrictCode, NewDistrictName, NewProvinceCode, NewProvinceName, DeductionFromMonth,
        DeductionToMonth, GhiChu, OriginalUpdatedAtUtc, Actor: null);

    #endregion
}
