using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Common;
using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.ChiTietNhanVien;

public partial class ChiTietNhanVien : IDisposable
{
    private const int EmployeeSearchTake = 100;

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim detailLoadGate = new(1, 1);
    private readonly SemaphoreSlim employeeSearchGate = new(1, 1);

    [Inject] private ChiTietNhanVienDataProvider DataProvider { get; set; } = default!;
    [Inject] private AttendanceDepartmentDataProvider DepartmentDataProvider { get; set; } = default!;
    [Inject] private AttendancePositionDataProvider PositionDataProvider { get; set; } = default!;
    [Inject] private IHrmDialogService DialogService { get; set; } = default!;
    [Inject] private IHrmToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public Guid? EmployeeId { get; set; }
    [SupplyParameterFromQuery(Name = "id")] private Guid? EmployeeIdFromQuery { get; set; }

    private IReadOnlyList<AttendanceDepartmentRecord> DepartmentOptions { get; set; } = [];
    private IReadOnlyList<AttendancePositionRecord> PositionOptions { get; set; } = [];
    private IReadOnlyList<ChiTietNhanVienRecord> EmployeeSearchItems { get; set; } = [];
    private ChiTietNhanVienRecord? CurrentEmployee { get; set; }
    private ChiTietNhanVienEditModel? EditModel { get; set; }
    private ChiTietNhanVienProfileForm? ProfileForm { get; set; }
    private string? DetailLoadErrorMessage { get; set; }
    private string? LookupLoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private string? EmployeeSearchText { get; set; }
    private string? EmployeeSearchErrorMessage { get; set; }
    private bool IsInitialLoadCompleted { get; set; }
    private bool IsLoadingEditorLookups { get; set; }
    private bool IsLoadingDetail { get; set; }
    private bool IsRefreshing { get; set; }
    private bool IsSavingEmployee { get; set; }
    private bool IsDeletingEmployee { get; set; }
    private bool IsEditorVisible { get; set; }
    private bool IsProfileEditMode { get; set; }
    private bool IsCreateMode { get; set; }
    private bool IsSmallScreen { get; set; }
    private bool IsEmployeeSearchVisible { get; set; }
    private bool IsSearchingEmployee { get; set; }
    private Guid? LoadedEmployeeId { get; set; }
    private long ProfileSavedVersion { get; set; }

    private Guid? RequestedEmployeeId => EmployeeId ?? EmployeeIdFromQuery;
    private bool HasDetailLoadError => !string.IsNullOrWhiteSpace(DetailLoadErrorMessage);
    private bool ShowLoadingPanel => IsLoadingEditorLookups || IsLoadingDetail || IsRefreshing || IsSavingEmployee || IsDeletingEmployee;
    private bool CanInteract => !ShowLoadingPanel && !HasDetailLoadError;
    private bool CanCreate => !ShowLoadingPanel && !IsEditorVisible && !IsProfileEditMode;
    private bool CanEdit => CanInteract && CurrentEmployee is not null && !IsEditorVisible && !IsProfileEditMode;
    private bool CanDelete => CanEdit;
    private bool CanRefresh => !ShowLoadingPanel && !IsEditorVisible && !IsProfileEditMode;
    private bool CanSearchEmployees => !ShowLoadingPanel && !IsEditorVisible && !IsProfileEditMode;
    private string LoadingText => IsSavingEmployee ? "Đang lưu hồ sơ nhân viên..." : IsDeletingEmployee ? "Đang xóa hồ sơ nhân viên..." : IsRefreshing ? "Đang tải lại hồ sơ nhân viên..." : IsLoadingEditorLookups ? "Đang tải danh mục hồ sơ..." : "Đang tải hồ sơ nhân viên...";
    private string? CurrentEmployeeAvatarSource => AvatarImageSourceHelper.NormalizeSource(CurrentEmployee?.AvatarDataUrl);
    private string CurrentEmployeeInitials => BuildInitials(CurrentEmployee?.FullName);

    protected override async Task OnParametersSetAsync()
    {
        if(!IsInitialLoadCompleted)
        {
            await LoadInitialDataAsync();
            IsInitialLoadCompleted = true;
            return;
        }

        if(LoadedEmployeeId == RequestedEmployeeId) return;
        await LoadDetailAsync(RequestedEmployeeId);
    }

    private async Task LoadInitialDataAsync()
    {
        await LoadEditorLookupsAsync();
        await LoadDetailAsync(RequestedEmployeeId);
    }

    private async Task LoadEditorLookupsAsync()
    {
        IsLoadingEditorLookups = true;
        try
        {
            LookupLoadErrorMessage = null;
            DepartmentOptions = await DepartmentDataProvider.GetAsync(disposalTokenSource.Token);
            PositionOptions = await PositionDataProvider.GetAsync(disposalTokenSource.Token);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(Exception)
        {
            DepartmentOptions = [];
            PositionOptions = [];
            LookupLoadErrorMessage = "Không thể tải danh mục phòng ban hoặc chức vụ. Vui lòng thử lại.";
        }
        finally
        {
            IsLoadingEditorLookups = false;
        }
    }

    private async Task LoadDetailAsync(Guid? employeeId)
    {
        if(disposalTokenSource.IsCancellationRequested) return;
        var lockAcquired = false;
        try
        {
            await detailLoadGate.WaitAsync(disposalTokenSource.Token);
            lockAcquired = true;
            LoadedEmployeeId = employeeId;
            DetailLoadErrorMessage = null;
            if(employeeId is null)
            {
                CurrentEmployee = null;
                return;
            }

            IsLoadingDetail = true;
            var employee = await DataProvider.GetByIdAsync(employeeId.Value, disposalTokenSource.Token);
            if(employeeId != RequestedEmployeeId) return;
            if(employee is null)
            {
                CurrentEmployee = null;
                DetailLoadErrorMessage = "Không tìm thấy hồ sơ nhân viên. Hồ sơ có thể đã bị xóa hoặc không còn khả dụng.";
                return;
            }
            CurrentEmployee = employee;
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(Exception)
        {
            CurrentEmployee = null;
            DetailLoadErrorMessage = "Không thể tải hồ sơ nhân viên. Vui lòng thử lại.";
        }
        finally
        {
            IsLoadingDetail = false;
            if(lockAcquired) detailLoadGate.Release();
        }
    }

    private async Task RetryDetailLoadAsync() => await LoadDetailAsync(RequestedEmployeeId);

    private async Task RefreshAsync()
    {
        if(!CanRefresh) return;
        IsRefreshing = true;
        try
        {
            await LoadEditorLookupsAsync();
            await LoadDetailAsync(RequestedEmployeeId);
        }
        finally { IsRefreshing = false; }
    }

    private async Task OpenEmployeeSearchAsync()
    {
        if(!CanSearchEmployees) return;

        IsEmployeeSearchVisible = true;
        EmployeeSearchText = null;
        await SearchEmployeesAsync(null);
    }

    private async Task OnEmployeeSearchTextChangedAsync(string? value)
    {
        var normalizedSearchText = NormalizeOptional(value);
        if(string.Equals(EmployeeSearchText, normalizedSearchText, StringComparison.Ordinal)) return;

        EmployeeSearchText = normalizedSearchText;
        await SearchEmployeesAsync(normalizedSearchText);
    }

    private Task OnEmployeeSearchVisibleChangedAsync(bool visible)
    {
        if(IsSearchingEmployee && !visible) return Task.CompletedTask;

        IsEmployeeSearchVisible = visible;
        return Task.CompletedTask;
    }

    private async Task SearchEmployeesAsync(string? searchText)
    {
        if(disposalTokenSource.IsCancellationRequested) return;

        var lockAcquired = false;
        try
        {
            await employeeSearchGate.WaitAsync(disposalTokenSource.Token);
            lockAcquired = true;
            IsSearchingEmployee = true;
            EmployeeSearchErrorMessage = null;
            EmployeeSearchItems = await DataProvider.SearchAsync(
                new ChiTietNhanVienFilter(NormalizeOptional(searchText), EmployeeSearchTake),
                disposalTokenSource.Token);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(Exception)
        {
            EmployeeSearchItems = [];
            EmployeeSearchErrorMessage = "Không thể tìm nhân viên. Vui lòng thử lại.";
        }
        finally
        {
            IsSearchingEmployee = false;
            if(lockAcquired) employeeSearchGate.Release();
        }
    }

    private Task OnEmployeeSelectedAsync(ChiTietNhanVienRecord employee)
    {
        IsEmployeeSearchVisible = false;
        NavigateToEmployee(employee.Id);
        return Task.CompletedTask;
    }

    private void OpenCreateEditor()
    {
        if(!CanCreate) return;
        EditErrorMessage = null;
        EditModel = new ChiTietNhanVienEditModel();
        IsCreateMode = true;
        IsEditorVisible = true;
    }

    private async Task BeginInlineEditAsync()
    {
        if(!CanEdit || ProfileForm is null) return;

        EditErrorMessage = null;
        await ProfileForm.BeginEditAsync();
    }

    private Task OnProfileEditModeChangedAsync(bool isEditMode)
    {
        IsProfileEditMode = isEditMode;
        return Task.CompletedTask;
    }

    private async Task OnEditorVisibleChangedAsync(bool visible)
    {
        if(!visible && IsSavingEmployee) return;
        IsEditorVisible = visible;
        if(!visible) ResetEditorState();
        await Task.CompletedTask;
    }

    private async Task SaveEmployeeAsync(ChiTietNhanVienEditModel model)
    {
        if(IsSavingEmployee || !string.IsNullOrWhiteSpace(LookupLoadErrorMessage)) return;
        if(!IsCreateMode && CurrentEmployee is null)
        {
            EditErrorMessage = "Không xác định được hồ sơ nhân viên cần điều chỉnh.";
            return;
        }

        IsSavingEmployee = true;
        EditErrorMessage = null;
        var isCreatingEmployee = IsCreateMode;
        try
        {
            var savedEmployee = isCreatingEmployee
                ? await DataProvider.CreateAsync(model, disposalTokenSource.Token)
                : await DataProvider.UpdateAsync(CurrentEmployee!.Id, model, disposalTokenSource.Token);
            IsEditorVisible = false;
            ResetEditorState();
            if(isCreatingEmployee)
            {
                NavigateToEmployee(savedEmployee.Id);
            }
            else
            {
                CurrentEmployee = savedEmployee;
            }
            ToastService.ShowSuccess(isCreatingEmployee ? "Đã thêm mới nhân viên." : "Đã cập nhật hồ sơ nhân viên.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(InvalidOperationException ex) { EditErrorMessage = ex.Message; }
        catch(Exception)
        {
            EditErrorMessage = isCreatingEmployee ? "Không thể thêm mới nhân viên. Vui lòng thử lại." : "Không thể cập nhật hồ sơ nhân viên. Vui lòng thử lại.";
        }
        finally { IsSavingEmployee = false; }
    }

    private async Task SaveInlineProfileAsync(ChiTietNhanVienEditModel model)
    {
        await SaveEmployeeAsync(model);
        if(string.IsNullOrWhiteSpace(EditErrorMessage) && CurrentEmployee is not null)
        {
            ProfileSavedVersion++;
        }
    }

    private async Task DeleteCurrentEmployeeAsync()
    {
        if(!CanDelete || CurrentEmployee is null) return;
        var employee = CurrentEmployee;
        var confirmed = await DialogService.ConfirmAsync($"Bạn có chắc muốn xóa nhân viên `{employee.LookupText}`?", title: "Xác nhận xóa", okText: "Xóa", cancelText: "Hủy", renderStyle: MessageBoxRenderStyle.Danger);
        if(!confirmed) return;
        IsDeletingEmployee = true;
        try
        {
            await DataProvider.DeleteAsync([employee.Id], disposalTokenSource.Token);
            ToastService.ShowSuccess("Đã xóa nhân viên.");
            NavigateToEmployee(null);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(Exception) { ToastService.ShowError("Không thể xóa nhân viên. Vui lòng thử lại."); }
        finally { IsDeletingEmployee = false; }
    }

    private void NavigateToEmployee(Guid? employeeId) => NavigationManager.NavigateTo(employeeId.HasValue ? $"/attendance/employees/details/{employeeId.Value}" : "/attendance/employees/details", replace: true);

    private void ResetEditorState()
    {
        EditModel = null;
        EditErrorMessage = null;
        IsCreateMode = false;
    }

    private static ChiTietNhanVienEditModel MapToEditModel(ChiTietNhanVienRecord employee) => new()
    {
        EmployeeCode = employee.EmployeeCode,
        FullName = employee.FullName,
        DepartmentId = employee.DepartmentId,
        PositionId = employee.PositionId,
        Status = employee.EmploymentStatus ?? ChiTietNhanVienEmploymentStatus.Probation,
        HireDate = employee.HireDate,
        SeniorityStartDate = employee.SeniorityStartDate,
        ResignedDate = employee.ResignedDate,
        OriginalUpdatedAtUtc = employee.UpdatedAtUtc ?? employee.CreatedAtUtc
    };

    private static string DisplayOptional(string? value) => string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildInitials(string? fullName)
    {
        var words = fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        return words.Length switch
        {
            0 => "NV",
            1 => words[0][..1].ToUpperInvariant(),
            _ => string.Concat(words[0][..1], words[^1][..1]).ToUpperInvariant()
        };
    }

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        detailLoadGate.Dispose();
        employeeSearchGate.Dispose();
    }
}
