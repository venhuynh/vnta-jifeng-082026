using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Web.Client.Models.Employees;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.GiamTruGiaCanh;

public partial class GiamTruGiaCanh : IDisposable
{
    #region Cấu hình và phụ thuộc

    private static readonly int[] PageSizeOptions = [50, 100, 200];
    private static readonly IReadOnlyList<string> GenderOptions = ["Nam", "Nữ", "Khác"];

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private CancellationTokenSource? activeReloadTokenSource;
    private int reloadRequestedVersion;
    private bool disposed;

    [Inject] private EmployeeTaxDependentDataProvider DataProvider { get; set; } = default!;
    [Inject] private EmployeeDataProvider EmployeeDataProvider { get; set; } = default!;
    [Inject] private IHrmToastService ToastService { get; set; } = default!;

    #endregion

    #region Trạng thái màn hình

    private IReadOnlyList<GiamTruGiaCanhRecord> Records { get; set; } = [];
    private IReadOnlyList<EmployeeRecord> Employees { get; set; } = [];
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private int TotalCount { get; set; }
    private int CurrentPageIndex { get; set; }
    private int PageSize { get; set; } = 50;
    private bool IsLoading { get; set; } = true;
    private bool IsEditPopupVisible { get; set; }
    private bool IsSavingEdit { get; set; }
    private string? EditErrorMessage { get; set; }
    private GiamTruGiaCanhEditModel EditModel { get; set; } = new();

    #endregion

    #region Trạng thái suy diễn và quyền thao tác

    private int TotalPageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    private bool CanCreate => !IsLoading && !IsSavingEdit && Employees.Count > 0;
    private bool CanReload => !IsLoading && !IsSavingEdit;
    private bool CanChangeFilters => !IsLoading && !IsSavingEdit;
    private bool CanEditRows => !IsLoading && !IsSavingEdit;
    private bool CanEmptyStateAction => string.IsNullOrWhiteSpace(SearchText) ? CanCreate : CanReload;
    private bool CanSaveEdit => !IsLoading
        && !IsSavingEdit
        && EditModel.EmployeeId != Guid.Empty
        && !string.IsNullOrWhiteSpace(EditModel.DependentFullName);
    private string EditPopupTitle => EditModel.Id == Guid.Empty ? "Thêm người phụ thuộc" : "Cập nhật người phụ thuộc";
    private string EmptyStateTitle => string.IsNullOrWhiteSpace(SearchText) ? "Chưa có người phụ thuộc" : "Không tìm thấy người phụ thuộc phù hợp";
    private string EmptyStateMessage => string.IsNullOrWhiteSpace(SearchText)
        ? "Thêm hồ sơ người phụ thuộc để quản lý đăng ký giảm trừ gia cảnh."
        : "Hãy thay đổi từ khóa tìm kiếm hoặc tải lại dữ liệu.";
    private string EmptyStateActionText => string.IsNullOrWhiteSpace(SearchText) ? "Thêm người phụ thuộc" : "Tải lại";
    private string PagerSummaryText => TotalCount == 0
        ? "Chưa có dữ liệu"
        : $"Hiển thị {CurrentPageIndex * PageSize + 1:N0}–{Math.Min((CurrentPageIndex + 1) * PageSize, TotalCount):N0} / {TotalCount:N0} hồ sơ";

    #endregion

    #region Tải và điều hướng dữ liệu

    protected override async Task OnInitializedAsync()
    {
        await LoadEmployeesAsync();
        await ReloadAsync();
    }

    private async Task LoadEmployeesAsync()
    {
        try
        {
            Employees = (await EmployeeDataProvider.GetAsync(disposalTokenSource.Token))
                .OrderBy(employee => employee.EmployeeCode)
                .ThenBy(employee => employee.FullName)
                .ToArray();
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            Employees = [];
            ToastService.ShowWarning("Không thể tải danh sách nhân viên để thêm hồ sơ người phụ thuộc.");
        }
    }

    private async Task ReloadAsync()
    {
        if (disposed)
        {
            return;
        }

        var requestVersion = Interlocked.Increment(ref reloadRequestedVersion);
        CancelActiveReload();
        await reloadGate.WaitAsync();

        try
        {
            if (disposed || requestVersion != Volatile.Read(ref reloadRequestedVersion))
            {
                return;
            }

            IsLoading = true;
            LoadErrorMessage = null;

            var searchText = NormalizeSearchText(SearchText);
            var requestedPageIndex = CurrentPageIndex;
            var requestedPageSize = PageSize;
            using var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
            activeReloadTokenSource = requestTokenSource;

            try
            {
                var result = await DataProvider.SearchAsync(
                    searchText,
                    isFamilyDeductionRegistered: null,
                    requestedPageIndex * requestedPageSize,
                    requestedPageSize,
                    requestTokenSource.Token);

                if (result.TotalCount > 0 && requestedPageIndex * requestedPageSize >= result.TotalCount)
                {
                    requestedPageIndex = Math.Max(0, (int)Math.Ceiling(result.TotalCount / (double)requestedPageSize) - 1);
                    result = await DataProvider.SearchAsync(
                        searchText,
                        isFamilyDeductionRegistered: null,
                        requestedPageIndex * requestedPageSize,
                        requestedPageSize,
                        requestTokenSource.Token);
                }

                if (requestVersion != Volatile.Read(ref reloadRequestedVersion) || disposed)
                {
                    return;
                }

                SearchText = searchText;
                CurrentPageIndex = requestedPageIndex;
                Records = result.Items
                    .Select(GiamTruGiaCanhRecord.From)
                    .ToArray();
                TotalCount = result.TotalCount;
            }
            catch (OperationCanceledException) when (requestTokenSource.IsCancellationRequested)
            {
            }
            catch (HrmApiException exception)
            {
                SetLoadError(requestVersion, exception.UserMessage);
            }
            catch (Exception)
            {
                SetLoadError(requestVersion, "Dữ liệu người phụ thuộc hiện chưa tải được. Vui lòng thử lại.");
            }
            finally
            {
                if (ReferenceEquals(activeReloadTokenSource, requestTokenSource))
                {
                    activeReloadTokenSource = null;
                }
            }
        }
        finally
        {
            if (requestVersion == Volatile.Read(ref reloadRequestedVersion))
            {
                IsLoading = false;
            }

            reloadGate.Release();
        }
    }

    private async Task OnSearchTextChanged(string? searchText)
    {
        SearchText = NormalizeSearchText(searchText);
        CurrentPageIndex = 0;
        await ReloadAsync();
    }

    private async Task OnActivePageIndexChangedAsync(int pageIndex)
    {
        if(IsLoading || IsSavingEdit)
        {
            return;
        }

        var normalizedPageIndex = Math.Clamp(pageIndex, 0, TotalPageCount - 1);
        if(CurrentPageIndex == normalizedPageIndex)
        {
            return;
        }

        CurrentPageIndex = normalizedPageIndex;
        await ReloadAsync();
    }

    private async Task OnPageSizeChangedAsync(int pageSize)
    {
        if (IsLoading || IsSavingEdit || !PageSizeOptions.Contains(pageSize) || PageSize == pageSize)
        {
            return;
        }

        var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
        PageSize = pageSize;
        CurrentPageIndex = firstVisibleRecordIndex / PageSize;
        await ReloadAsync();
    }

    #endregion

    #region Cửa sổ chỉnh sửa và lưu dữ liệu

    private void OpenCreatePopup()
    {
        if (!CanCreate)
        {
            return;
        }

        EditModel = new GiamTruGiaCanhEditModel();
        EditErrorMessage = null;
        IsEditPopupVisible = true;
    }

    private void OpenEditPopup(GiamTruGiaCanhRecord row)
    {
        if (!CanEditRows)
        {
            return;
        }

        EditModel = GiamTruGiaCanhEditModel.From(row.Dependent);
        EditErrorMessage = null;
        IsEditPopupVisible = true;
    }

    private Task SetEditPopupVisibleAsync(bool visible)
    {
        if (!IsSavingEdit)
        {
            IsEditPopupVisible = visible;
            if (!visible)
            {
                EditErrorMessage = null;
            }
        }

        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (!CanSaveEdit || disposed)
        {
            return;
        }

        var isNew = EditModel.Id == Guid.Empty;
        IsSavingEdit = true;
        EditErrorMessage = null;

        try
        {
            await DataProvider.SaveAsync(EditModel.ToRequest(), disposalTokenSource.Token);
            if (disposed)
            {
                return;
            }

            await ReloadAsync();
            if (disposed)
            {
                return;
            }

            IsEditPopupVisible = false;
            ToastService.ShowSuccess(isNew ? "Đã thêm người phụ thuộc." : "Đã cập nhật người phụ thuộc.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (HrmApiException exception)
        {
            EditErrorMessage = exception.UserMessage;
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể lưu hồ sơ người phụ thuộc. Vui lòng thử lại.";
        }
        finally
        {
            IsSavingEdit = false;
        }
    }

    #endregion

    #region Trạng thái rỗng và dọn dẹp

    private Task OnEmptyStateActionAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            OpenCreatePopup();
            return Task.CompletedTask;
        }

        return ReloadAsync();
    }

    private void SetLoadError(int requestVersion, string message)
    {
        if (requestVersion != Volatile.Read(ref reloadRequestedVersion) || disposed)
        {
            return;
        }

        Records = [];
        TotalCount = 0;
        LoadErrorMessage = message;
    }

    private void CancelActiveReload()
    {
        try
        {
            activeReloadTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static string? NormalizeSearchText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatMonth(DateOnly? value) => value?.ToString("MM/yyyy") ?? "—";

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        CancelActiveReload();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }

    #endregion
}

public sealed class GiamTruGiaCanhRecord
{
    public Guid Id { get; init; }
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public string? DependentFullName { get; init; }
    public string? RelationshipToEmployee { get; init; }
    public string? DependentTaxCode { get; init; }
    public string? DependentIdentityNumber { get; init; }
    public DateOnly? DeductionFromMonth { get; init; }
    public DateOnly? DeductionToMonth { get; init; }
    public bool IsFamilyDeductionRegistered { get; init; }
    public string? GhiChu { get; init; }
    public EmployeeTaxDependentDto Dependent { get; init; } = default!;

    public static GiamTruGiaCanhRecord From(EmployeeTaxDependentListItemDto source) => new()
    {
        Id = source.Dependent.Id,
        EmployeeCode = source.EmployeeCode,
        EmployeeName = source.EmployeeName,
        DependentFullName = source.Dependent.DependentFullName,
        RelationshipToEmployee = source.Dependent.RelationshipToEmployee,
        DependentTaxCode = source.Dependent.DependentTaxCode,
        DependentIdentityNumber = source.Dependent.DependentIdentityNumber,
        DeductionFromMonth = source.Dependent.DeductionFromMonth,
        DeductionToMonth = source.Dependent.DeductionToMonth,
        IsFamilyDeductionRegistered = source.Dependent.IsFamilyDeductionRegistered,
        GhiChu = source.Dependent.GhiChu,
        Dependent = source.Dependent
    };
}
