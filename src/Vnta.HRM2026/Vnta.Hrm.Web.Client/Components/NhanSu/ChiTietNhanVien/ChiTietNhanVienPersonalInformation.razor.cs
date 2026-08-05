using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.ChiTietNhanVien;

public partial class ChiTietNhanVienPersonalInformation : IDisposable
{
    private readonly CancellationTokenSource disposalTokenSource = new();
    [Inject] private ChiTietNhanVienDataProvider DataProvider { get; set; } = default!;
    [Inject] private IHrmToastService ToastService { get; set; } = default!;
    [Parameter] public Guid EmployeeId { get; set; }
    private Guid loadedEmployeeId;
    private EmployeeContactProfileDto? ContactProfile { get; set; }
    private CitizenIdentityDto? CitizenIdentity { get; set; }
    private ContactEditModel? ContactEdit { get; set; }
    private CitizenIdentityEditModel? CitizenIdentityEdit { get; set; }
    private EditContext? ContactEditContext { get; set; }
    private EditContext? CitizenIdentityEditContext { get; set; }
    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }
    private bool IsEditingContact { get; set; }
    private bool IsEditingCitizenIdentity { get; set; }
    private string? ErrorMessage { get; set; }
    private bool CanBeginContactEdit => !IsLoading && !IsSaving && !IsEditingCitizenIdentity;
    private bool CanBeginCitizenIdentityEdit => !IsLoading && !IsSaving && !IsEditingContact;
    private bool CanSaveContact => !IsSaving && ContactEdit is not null && ContactEditContext is not null;
    private bool CanSaveCitizenIdentity => !IsSaving && CitizenIdentityEdit is not null && CitizenIdentityEditContext is not null;

    protected override async Task OnParametersSetAsync()
    {
        if(EmployeeId == Guid.Empty || loadedEmployeeId == EmployeeId) return;
        loadedEmployeeId = EmployeeId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            ContactProfile = await DataProvider.GetContactProfileAsync(EmployeeId, disposalTokenSource.Token);
            CitizenIdentity = await DataProvider.GetCitizenIdentityAsync(EmployeeId, disposalTokenSource.Token);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(Exception) { ErrorMessage = "Không thể tải thông tin cá nhân. Vui lòng thử lại."; }
        finally { IsLoading = false; }
    }

    private Task BeginContactEditAsync()
    {
        if(!CanBeginContactEdit) return Task.CompletedTask;
        ContactEdit = ContactEditModel.From(ContactProfile, EmployeeId);
        ContactEditContext = new EditContext(ContactEdit);
        IsEditingContact = true;
        return Task.CompletedTask;
    }

    private Task CancelContactEditAsync()
    {
        if(IsSaving) return Task.CompletedTask;
        IsEditingContact = false;
        ContactEdit = null;
        ContactEditContext = null;
        return Task.CompletedTask;
    }

    private Task BeginCitizenIdentityEditAsync()
    {
        if(!CanBeginCitizenIdentityEdit) return Task.CompletedTask;
        CitizenIdentityEdit = CitizenIdentityEditModel.From(CitizenIdentity, EmployeeId);
        CitizenIdentityEditContext = new EditContext(CitizenIdentityEdit);
        IsEditingCitizenIdentity = true;
        return Task.CompletedTask;
    }

    private Task CancelCitizenIdentityEditAsync()
    {
        if(IsSaving) return Task.CompletedTask;
        IsEditingCitizenIdentity = false;
        CitizenIdentityEdit = null;
        CitizenIdentityEditContext = null;
        return Task.CompletedTask;
    }

    private async Task SaveContactAsync()
    {
        var model = ContactEdit;
        var editContext = ContactEditContext;
        if(IsSaving || model is null || editContext is null || !editContext.Validate()) return;
        IsSaving = true; ErrorMessage = null;
        try { ContactProfile = await DataProvider.UpsertContactProfileAsync(model.ToRequest(), disposalTokenSource.Token); IsEditingContact = false; ToastService.ShowSuccess("Đã cập nhật thông tin liên hệ."); }
        catch(InvalidOperationException ex) { ErrorMessage = ex.Message; }
        catch(Exception) { ErrorMessage = "Không thể cập nhật thông tin liên hệ. Vui lòng thử lại."; }
        finally { IsSaving = false; }
    }

    private async Task SaveCitizenIdentityAsync()
    {
        var model = CitizenIdentityEdit;
        var editContext = CitizenIdentityEditContext;
        if(IsSaving || model is null || editContext is null || !editContext.Validate()) return;
        IsSaving = true; ErrorMessage = null;
        try { CitizenIdentity = await DataProvider.UpsertCitizenIdentityAsync(model.ToRequest(), disposalTokenSource.Token); IsEditingCitizenIdentity = false; ToastService.ShowSuccess("Đã cập nhật thông tin CCCD."); }
        catch(InvalidOperationException ex) { ErrorMessage = ex.Message; }
        catch(Exception) { ErrorMessage = "Không thể cập nhật thông tin CCCD. Vui lòng thử lại."; }
        finally { IsSaving = false; }
    }

    private static string DisplayOptional(string? value) => string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();
    private static string DisplayDate(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? "Chưa có";
    private static string FormatEmergencyContact(EmployeeContactProfileDto profile) => string.IsNullOrWhiteSpace(profile.EmergencyContactName) ? "Chưa có" : $"{profile.EmergencyContactName} · {profile.EmergencyContactPhoneNumber}";
    public void Dispose() { disposalTokenSource.Cancel(); disposalTokenSource.Dispose(); }

    private sealed class ContactEditModel : IValidatableObject
    {
        public Guid EmployeeId { get; set; }
        [EmailAddress(ErrorMessage = "Email cá nhân không hợp lệ.")]
        [StringLength(256, ErrorMessage = "Email cá nhân không được vượt quá 256 ký tự.")]
        public string? PersonalEmail { get; set; }
        [StringLength(30, ErrorMessage = "Điện thoại cá nhân không được vượt quá 30 ký tự.")] public string? PersonalPhoneNumber { get; set; }
        public string? PermanentAddress { get; set; }
        public string? CurrentAddress { get; set; }
        [StringLength(150, ErrorMessage = "Họ tên liên hệ khẩn cấp không được vượt quá 150 ký tự.")] public string? EmergencyContactName { get; set; }
        [StringLength(100, ErrorMessage = "Quan hệ liên hệ khẩn cấp không được vượt quá 100 ký tự.")] public string? EmergencyContactRelationship { get; set; }
        [StringLength(30, ErrorMessage = "Điện thoại liên hệ khẩn cấp không được vượt quá 30 ký tự.")] public string? EmergencyContactPhoneNumber { get; set; }
        public DateTime? OriginalUpdatedAtUtc { get; set; }
        public static ContactEditModel From(EmployeeContactProfileDto? source, Guid employeeId) => new() { EmployeeId = employeeId, PersonalEmail = source?.PersonalEmail, PersonalPhoneNumber = source?.PersonalPhoneNumber, PermanentAddress = source?.PermanentAddress, CurrentAddress = source?.CurrentAddress, EmergencyContactName = source?.EmergencyContactName, EmergencyContactRelationship = source?.EmergencyContactRelationship, EmergencyContactPhoneNumber = source?.EmergencyContactPhoneNumber, OriginalUpdatedAtUtc = source?.UpdatedAtUtc ?? source?.CreatedAtUtc };
        public UpsertEmployeeContactProfileRequest ToRequest() => new(EmployeeId, PersonalEmail, PersonalPhoneNumber, PermanentAddress, CurrentAddress, EmergencyContactName, EmergencyContactRelationship, EmergencyContactPhoneNumber, OriginalUpdatedAtUtc);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasEmergencyContact = !string.IsNullOrWhiteSpace(EmergencyContactName)
                || !string.IsNullOrWhiteSpace(EmergencyContactRelationship)
                || !string.IsNullOrWhiteSpace(EmergencyContactPhoneNumber);
            if(hasEmergencyContact && (string.IsNullOrWhiteSpace(EmergencyContactName) || string.IsNullOrWhiteSpace(EmergencyContactPhoneNumber)))
            {
                yield return new ValidationResult(
                    "Liên hệ khẩn cấp phải có họ tên và số điện thoại.",
                    [nameof(EmergencyContactName), nameof(EmergencyContactPhoneNumber)]);
            }
        }
    }

    private sealed class CitizenIdentityEditModel : IValidatableObject
    {
        public Guid EmployeeId { get; set; }
        [RegularExpression("^$|^\\d{12}$", ErrorMessage = "Số CCCD phải có đúng 12 chữ số.")] public string? CitizenIdentityNumber { get; set; }
        public DateOnly? IssuedDate { get; set; }
        [StringLength(250, ErrorMessage = "Nơi cấp không được vượt quá 250 ký tự.")] public string? IssuedPlace { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public DateTime? OriginalUpdatedAtUtc { get; set; }
        public static CitizenIdentityEditModel From(CitizenIdentityDto? source, Guid employeeId) => new() { EmployeeId = employeeId, IssuedDate = source?.IssuedDate, IssuedPlace = source?.IssuedPlace, ExpiryDate = source?.ExpiryDate, OriginalUpdatedAtUtc = source?.UpdatedAtUtc ?? source?.CreatedAtUtc };
        public UpsertCitizenIdentityRequest ToRequest() => new(EmployeeId, CitizenIdentityNumber, IssuedDate, IssuedPlace, ExpiryDate, OriginalUpdatedAtUtc);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(IssuedDate is { } issuedDate && issuedDate > DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult("Ngày cấp căn cước công dân không được sau ngày hiện tại.", [nameof(IssuedDate)]);
            }
            if(IssuedDate is { } fromDate && ExpiryDate is { } expiryDate && expiryDate < fromDate)
            {
                yield return new ValidationResult("Ngày hết hạn không được trước ngày cấp.", [nameof(ExpiryDate)]);
            }
        }
    }
}
