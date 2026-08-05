using System.ComponentModel.DataAnnotations;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models.Security;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.PheDuyetTaiKhoan;

public partial class PheDuyetTaiKhoan : IDisposable
{
    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject]
    private EmployeeAccountDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IReadOnlyList<EmployeeAccountRecord> Records { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string StatusFilter { get; set; } = "PendingApproval";
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; }
    private bool IsRejectPopupVisible { get; set; }
    private EmployeeAccountRecord? RejectTarget { get; set; }
    private RejectAccountFormModel RejectFormModel { get; set; } = new();
    private EditContext RejectEditContext { get; set; } = default!;

    private static readonly IReadOnlyList<StatusFilterOption> StatusFilterOptions =
    [
        new("PendingApproval", "Chờ duyệt"),
        new("Approved", "Đã duyệt"),
        new("Rejected", "Từ chối"),
        new("Disabled", "Ngừng dùng"),
        new("All", "Tất cả")
    ];

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanReload => !IsLoading && !IsSaving;
    private bool CanApprove => !IsLoading && !IsSaving && CanReviewSelectedEmployee(EmployeeAccountApprovalStatusText.PendingApproval);
    private bool CanReject => !IsLoading && !IsSaving && CanReviewSelectedEmployee(EmployeeAccountApprovalStatusText.PendingApproval);
    private bool CanSubmitReject => !IsSaving && RejectTarget is not null;
    private string LoadingText => "Đang tải danh sách tài khoản cần phê duyệt...";
    private string RejectEmployeeDisplay => RejectTarget is null ? string.Empty : $"{RejectTarget.EmployeeCode} - {RejectTarget.FullName}";
    private string RejectReason
    {
        get => RejectFormModel.RejectionReason;
        set => RejectFormModel.RejectionReason = value;
    }

    private IReadOnlyList<EmployeeAccountRecord> FilteredRecords =>
        StatusFilter == "All"
            ? Records
            : Records.Where(x => string.Equals(x.ApprovalStatus, StatusFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

    protected override async Task OnInitializedAsync()
    {
        RejectEditContext = new EditContext(RejectFormModel);
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
            ToastService.ShowError("Không thể tải danh sách phê duyệt tài khoản.");
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

    private async Task OnApproveClick()
    {
        var employee = GetSingleSelectedEmployeeForPendingApproval();
        if(employee is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một tài khoản đang chờ duyệt.");
            return;
        }

        IsSaving = true;
        EditErrorMessage = null;

        try
        {
            await DataProvider.ApproveAsync(employee.EmployeeId, disposalTokenSource.Token);
            await ReloadAsync();
            ToastService.ShowSuccess("Đã phê duyệt tài khoản nhân viên.");
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

    private Task OnRejectClick()
    {
        var employee = GetSingleSelectedEmployeeForPendingApproval();
        if(employee is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một tài khoản đang chờ duyệt.");
            return Task.CompletedTask;
        }

        EditErrorMessage = null;
        RejectTarget = employee;
        RejectFormModel = new RejectAccountFormModel();
        RejectEditContext = new EditContext(RejectFormModel);
        IsRejectPopupVisible = true;
        return Task.CompletedTask;
    }

    private async Task OnConfirmRejectClickAsync()
    {
        if(RejectTarget is null || !RejectEditContext.Validate())
        {
            return;
        }

        IsSaving = true;
        EditErrorMessage = null;

        try
        {
            await DataProvider.RejectAsync(
                RejectTarget.EmployeeId,
                RejectFormModel.RejectionReason,
                disposalTokenSource.Token);
            await ReloadAsync();
            IsRejectPopupVisible = false;
            RejectTarget = null;
            ToastService.ShowSuccess("Đã từ chối tài khoản nhân viên.");
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

    private Task OnRejectPopupVisibleChanged(bool visible)
    {
        if(!visible && !IsSaving)
        {
            RejectTarget = null;
            RejectFormModel = new RejectAccountFormModel();
            RejectEditContext = new EditContext(RejectFormModel);
            EditErrorMessage = null;
        }

        IsRejectPopupVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnCancelRejectClickAsync() => OnRejectPopupVisibleChanged(false);

    private bool CanReviewSelectedEmployee(string requiredStatus)
    {
        var employee = GetSingleSelectedEmployee();
        return employee is not null
            && employee.HasAccount
            && string.Equals(employee.ApprovalStatus, requiredStatus, StringComparison.OrdinalIgnoreCase);
    }

    private EmployeeAccountRecord? GetSingleSelectedEmployeeForPendingApproval() => GetSingleSelectedEmployee() is { } employee
        && string.Equals(employee.ApprovalStatus, EmployeeAccountApprovalStatusText.PendingApproval, StringComparison.OrdinalIgnoreCase)
            ? employee
            : null;

    private EmployeeAccountRecord? GetSingleSelectedEmployee()
    {
        var selectedEmployees = SelectedDataItems
            .OfType<EmployeeAccountRecord>()
            .Where(IsVisibleEmployee)
            .DistinctBy(employee => employee.EmployeeId)
            .ToList();

        return selectedEmployees.Count == 1 ? selectedEmployees[0] : null;
    }

    private bool IsVisibleEmployee(EmployeeAccountRecord employee) =>
        FilteredRecords.Any(row => row.EmployeeId == employee.EmployeeId);

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
        public const string PendingApproval = "PendingApproval";
    }

    private sealed class RejectAccountFormModel
    {
        [Required(ErrorMessage = "Lý do từ chối không được để trống.")]
        public string RejectionReason { get; set; } = string.Empty;
    }

    private sealed record StatusFilterOption(string Value, string Text);
}
