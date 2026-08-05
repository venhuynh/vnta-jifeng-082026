using System.ComponentModel.DataAnnotations;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models.Security;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.TaiKhoanNhanVien;

public partial class TaiKhoanNhanVien : IDisposable
{
    private readonly CancellationTokenSource disposalTokenSource = new();

    private static readonly IReadOnlyList<RoleOption> RoleOptions =
    [
        new(InternalAccountRoles.Employee, "Nhân viên"),
        new(InternalAccountRoles.Manager, "Quản lý"),
        new(InternalAccountRoles.PayrollAdmin, "Quản trị lương"),
        new(InternalAccountRoles.AttendanceAdmin, "Quản trị chấm công"),
        new(InternalAccountRoles.HrAdmin, "Quản trị nhân sự"),
        new(InternalAccountRoles.SystemAdmin, "Quản trị hệ thống")
    ];

    [Inject]
    private EmployeeAccountDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IReadOnlyList<EmployeeAccountRecord> Records { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; }
    private bool IsOpenAccountPopupVisible { get; set; }
    private bool IsResetPasswordPopupVisible { get; set; }
    private OpenEmployeeAccountFormModel? OpenAccountModel { get; set; }
    private EditContext? OpenAccountEditContext { get; set; }
    private EmployeeAccountRecord? ResetPasswordTarget { get; set; }
    private ResetPasswordFormModel ResetPasswordModel { get; set; } = new();
    private EditContext ResetPasswordEditContext { get; set; } = default!;

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanReload => !IsLoading && !IsSaving;
    private bool CanOpenAccount => !IsLoading && !IsSaving && CanOpenSelectedEmployee();
    private bool CanResetPassword => !IsLoading && !IsSaving && GetSingleSelectedEmployee() is { HasAccount: true };
    private bool CanDeactivate => !IsLoading && !IsSaving && GetSingleSelectedEmployee() is { HasAccount: true, IsActive: true };
    private bool CanActivate => !IsLoading && !IsSaving && CanActivateSelectedEmployee();
    private bool CanSubmitOpenAccount => !IsSaving && OpenAccountModel is not null;
    private bool CanSubmitResetPassword => !IsSaving && ResetPasswordTarget is not null;
    private string LoadingText => "Đang tải dữ liệu tài khoản nhân viên...";
    private string ResetPasswordEmployeeDisplay => ResetPasswordTarget is null ? string.Empty : $"{ResetPasswordTarget.EmployeeCode} - {ResetPasswordTarget.FullName}";

    protected override async Task OnInitializedAsync()
    {
        ResetPasswordEditContext = new EditContext(ResetPasswordModel);
        await ReloadAsync();
        await base.OnInitializedAsync();
    }

    private async Task ReloadAsync()
    {
        if(disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        LoadErrorMessage = null;
        IsLoading = true;

        try
        {
            Records = await DataProvider.GetAsync(disposalTokenSource.Token);
            await ClearSelectionAsync();
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception ex)
        {
            Records = [];
            LoadErrorMessage = ex.Message;
            ToastService.ShowError("Không thể tải danh sách tài khoản nhân viên.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private Task OnOpenAccountClick()
    {
        var employee = GetSingleSelectedEmployee();
        if(employee is null || employee.HasAccount)
        {
            ToastService.ShowWarning("Hãy chọn đúng một nhân viên chưa có tài khoản để mở tài khoản.");
            return Task.CompletedTask;
        }

        EditErrorMessage = null;
        OpenAccountModel = new OpenEmployeeAccountFormModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.FullName,
            TemporaryPassword = $"Vnta@{employee.EmployeeCode}",
            RoleName = InternalAccountRoles.Employee,
            AccessLevel = InternalAccountRoles.Employee
        };
        OpenAccountEditContext = new EditContext(OpenAccountModel);
        IsOpenAccountPopupVisible = true;
        return Task.CompletedTask;
    }

    private async Task OnSaveOpenAccountClickAsync()
    {
        if(OpenAccountEditContext is null)
        {
            return;
        }

        if(!OpenAccountEditContext.Validate())
        {
            return;
        }

        await OnOpenAccountRequested();
    }

    private async Task OnOpenAccountRequested()
    {
        if(OpenAccountModel is null || IsSaving)
        {
            return;
        }

        IsSaving = true;
        EditErrorMessage = null;

        try
        {
            await DataProvider.OpenAsync(OpenAccountModel, disposalTokenSource.Token);
            await ReloadAsync();
            IsOpenAccountPopupVisible = false;
            OpenAccountModel = null;
            OpenAccountEditContext = null;
            ToastService.ShowSuccess("Đã mở tài khoản nhân viên ở trạng thái chờ phê duyệt.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private Task OnResetPasswordClick()
    {
        var employee = GetSingleSelectedEmployee();
        if(employee is not { HasAccount: true })
        {
            ToastService.ShowWarning("Hãy chọn đúng một nhân viên đã có tài khoản để đặt lại mật khẩu.");
            return Task.CompletedTask;
        }

        EditErrorMessage = null;
        ResetPasswordTarget = employee;
        ResetPasswordModel = new ResetPasswordFormModel
        {
            TemporaryPassword = $"Vnta@{employee.EmployeeCode}"
        };
        ResetPasswordEditContext = new EditContext(ResetPasswordModel);
        IsResetPasswordPopupVisible = true;
        return Task.CompletedTask;
    }

    private async Task OnConfirmResetPasswordClickAsync()
    {
        if(ResetPasswordTarget is null || !ResetPasswordEditContext.Validate())
        {
            return;
        }

        IsSaving = true;
        EditErrorMessage = null;

        try
        {
            await DataProvider.ResetPasswordAsync(
                ResetPasswordTarget.EmployeeId,
                ResetPasswordModel.TemporaryPassword,
                disposalTokenSource.Token);
            await ReloadAsync();
            IsResetPasswordPopupVisible = false;
            ResetPasswordTarget = null;
            ToastService.ShowSuccess("Đã đặt lại mật khẩu tạm cho tài khoản nhân viên.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task OnDeactivateClickAsync()
    {
        var employee = GetSingleSelectedEmployee();
        if(employee is not { HasAccount: true, IsActive: true })
        {
            ToastService.ShowWarning("Hãy chọn đúng một tài khoản đang kích hoạt để ngưng sử dụng.");
            return;
        }

        IsSaving = true;
        EditErrorMessage = null;

        try
        {
            await DataProvider.DeactivateAsync(employee.EmployeeId, disposalTokenSource.Token);
            await ReloadAsync();
            ToastService.ShowSuccess("Đã ngưng kích hoạt tài khoản nhân viên.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task OnActivateClickAsync()
    {
        var employee = GetSingleSelectedEmployee();
        if(employee is null || !CanActivateEmployee(employee))
        {
            ToastService.ShowWarning("Hãy chọn đúng một tài khoản đã bị ngưng kích hoạt để bật lại.");
            return;
        }

        IsSaving = true;
        EditErrorMessage = null;

        try
        {
            await DataProvider.ActivateAsync(employee.EmployeeId, disposalTokenSource.Token);
            await ReloadAsync();
            ToastService.ShowSuccess("Đã kích hoạt lại tài khoản nhân viên.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private Task OnOpenAccountPopupVisibleChanged(bool visible)
    {
        if(!visible && !IsSaving)
        {
            OpenAccountModel = null;
            OpenAccountEditContext = null;
            EditErrorMessage = null;
        }

        IsOpenAccountPopupVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnResetPasswordPopupVisibleChanged(bool visible)
    {
        if(!visible && !IsSaving)
        {
            ResetPasswordTarget = null;
            ResetPasswordModel = new ResetPasswordFormModel();
            ResetPasswordEditContext = new EditContext(ResetPasswordModel);
            EditErrorMessage = null;
        }

        IsResetPasswordPopupVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnCancelOpenAccountClickAsync() => OnOpenAccountPopupVisibleChanged(false);

    private Task OnCancelResetPasswordClickAsync() => OnResetPasswordPopupVisibleChanged(false);

    private EmployeeAccountRecord? GetSingleSelectedEmployee()
    {
        var selectedEmployees = SelectedDataItems
            .OfType<EmployeeAccountRecord>()
            .Where(IsVisibleEmployee)
            .DistinctBy(employee => employee.EmployeeId)
            .ToList();

        return selectedEmployees.Count == 1 ? selectedEmployees[0] : null;
    }

    private bool CanOpenSelectedEmployee() => GetSingleSelectedEmployee() is { HasAccount: false };

    private bool CanActivateSelectedEmployee() => GetSingleSelectedEmployee() is { } employee && CanActivateEmployee(employee);

    private static bool CanActivateEmployee(EmployeeAccountRecord employee) =>
        employee.HasAccount
        && !employee.IsActive
        && (string.Equals(employee.ApprovalStatus, EmployeeAccountApprovalStatusText.Disabled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(employee.ApprovalStatus, EmployeeAccountApprovalStatusText.Approved, StringComparison.OrdinalIgnoreCase));

    private int GetSelectedEmployeeCount() => SelectedDataItems
        .OfType<EmployeeAccountRecord>()
        .Where(IsVisibleEmployee)
        .DistinctBy(employee => employee.EmployeeId)
        .Count();

    private bool IsVisibleEmployee(EmployeeAccountRecord employee) =>
        Records.Any(row => row.EmployeeId == employee.EmployeeId);

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if(Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }

    private static class EmployeeAccountApprovalStatusText
    {
        public const string Approved = "Approved";
        public const string Disabled = "Disabled";
    }

    private sealed class ResetPasswordFormModel
    {
        [Required(ErrorMessage = "Mật khẩu tạm không được để trống.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tạm phải có ít nhất 6 ký tự.")]
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    private sealed record RoleOption(string Value, string Text);
}
