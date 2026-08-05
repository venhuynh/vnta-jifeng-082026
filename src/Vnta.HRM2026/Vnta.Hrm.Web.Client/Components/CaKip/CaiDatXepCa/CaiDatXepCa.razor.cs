using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Text;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.CaKip.CaiDatXepCa;

public partial class CaiDatXepCa : IDisposable
{
    #region Dependencies

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private ShiftSchedulingSettingDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    #endregion

    #region State

    private IReadOnlyList<ShiftSchedulingSettingRecord> AllSettings { get; set; } = [];
    private IReadOnlyList<ShiftSchedulingSettingRecord> VisibleSettings { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string? AppliedSearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private string CurrentLoadingText { get; set; } = HrmUiDefaults.LoadingText;
    private bool IsLoading { get; set; } = true;
    private bool IsSavingSetting { get; set; }
    private bool IsDeletingSettings { get; set; }
    private bool IsCreatingNewSetting { get; set; }
    private ShiftSchedulingClassificationType NewSettingClassificationType { get; set; } =
        ShiftSchedulingClassificationType.TheoPhongBan;
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;

    #endregion

    #region Derived State

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool HasSearchText => !string.IsNullOrWhiteSpace(AppliedSearchText);
    private bool CanInteract => !IsLoading && !IsSavingSetting && !IsDeletingSettings && !HasLoadError;
    private bool CanCreate => CanInteract;
    private bool CanEditSelected => CanInteract && GetSelectedSettingCount() == 1;
    private bool CanDeleteSelected => CanInteract && GetSelectedSettingCount() > 0;
    private bool CanExport => !IsLoading && !IsSavingSetting && !IsDeletingSettings && VisibleSettings.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedSettingCount() > 0;
    private bool ShowLoadingPanel => IsLoading || IsSavingSetting || IsDeletingSettings;
    private string LoadingText => CurrentLoadingText;
    private string EmptyStateTitle => HasSearchText
        ? "Không tìm thấy cấu hình xếp ca phù hợp"
        : "Chưa có cấu hình xếp ca";
    private string EmptyStateMessage => HasSearchText
        ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem thêm dữ liệu."
        : "Hãy bắt đầu bằng cách tạo cấu hình xếp ca đầu tiên cho màn này.";

    #endregion

    #region UI Entry Points

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadAsync();
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    #region Data Loading

    private async Task ReloadAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        if (!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
                   && reloadProcessedVersion < Volatile.Read(ref reloadRequestedVersion))
            {
                reloadProcessedVersion = Volatile.Read(ref reloadRequestedVersion);
                await ReloadCoreAsync();
            }
        }
        finally
        {
            reloadGate.Release();
        }
    }

    private async Task ReloadCoreAsync()
    {
        LoadErrorMessage = null;
        EditErrorMessage = null;
        IsLoading = true;
        CurrentLoadingText = "Đang tải dữ liệu cài đặt xếp ca...";

        try
        {
            await ClearSelectionAsync();
            var settings = await DataProvider.GetAsync(disposalTokenSource.Token);
            UpdateLoadedSettings(settings);
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            AllSettings = [];
            VisibleSettings = [];
            LoadErrorMessage = "Có lỗi khi tải dữ liệu cài đặt xếp ca. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách cài đặt xếp ca.");
        }
        finally
        {
            IsLoading = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    #endregion

    #region Screen Actions

    private Task OnAddDepartmentSettingClick() =>
        StartAddSettingAsync(ShiftSchedulingClassificationType.TheoPhongBan);

    private Task OnAddEmployeeSettingClick() =>
        StartAddSettingAsync(ShiftSchedulingClassificationType.TheoNhanVien);

    private async Task StartAddSettingAsync(ShiftSchedulingClassificationType classificationType)
    {
        if (!CanCreate || Grid is null)
        {
            return;
        }

        EditErrorMessage = null;
        NewSettingClassificationType = classificationType;
        await ClearSelectionAsync();
        await Grid.StartEditNewRowAsync();
    }

    private async Task OnEditSettingClick()
    {
        var setting = GetSingleSelectedSetting();
        if (setting is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một cấu hình xếp ca để điều chỉnh.");
            return;
        }

        await OpenEditAsync(setting);
    }

    private Task OnEditSettingRowClickAsync(ShiftSchedulingSettingRecord setting) =>
        OpenEditAsync(setting);

    private async Task OpenEditAsync(ShiftSchedulingSettingRecord? setting)
    {
        if (!CanInteract || Grid is null || setting is null)
        {
            return;
        }

        EditErrorMessage = null;
        await Grid.StartEditDataItemAsync(setting, nameof(ShiftSchedulingSettingRecord.ShiftId));
    }

    private async Task OnDeleteSettingsClick()
    {
        var selectedSettings = GetSelectedSettings();
        if (selectedSettings.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một cấu hình xếp ca để xóa.");
            return;
        }

        await DeleteSettingsAsync(selectedSettings);
    }

    private Task OnDeleteSettingRowClickAsync(ShiftSchedulingSettingRecord setting) =>
        DeleteSettingsAsync([setting]);

    private async Task DeleteSettingsAsync(IReadOnlyCollection<ShiftSchedulingSettingRecord> settingsToDelete)
    {
        if (!CanInteract || settingsToDelete.Count == 0)
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            settingsToDelete.Count == 1
                ? "Bạn có chắc muốn xóa cấu hình xếp ca này?"
                : $"Bạn có chắc muốn xóa {settingsToDelete.Count} cấu hình xếp ca?",
            title: "Xác nhận xóa",
            okText: "Xóa",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Danger);

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsDeletingSettings = true;
            CurrentLoadingText = settingsToDelete.Count == 1
                ? "Đang xóa cấu hình xếp ca..."
                : "Đang xóa danh sách cấu hình xếp ca...";

            var settings = await DataProvider.DeleteAsync(
                settingsToDelete
                    .Select(setting => setting.Id)
                    .Distinct(),
                disposalTokenSource.Token);
            UpdateLoadedSettings(settings);
            await ClearSelectionAsync();
            ToastService.ShowSuccess(
                settingsToDelete.Count == 1
                    ? "Đã xóa cấu hình xếp ca."
                    : $"Đã xóa {settingsToDelete.Count} cấu hình xếp ca.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            ToastService.ShowError(
                settingsToDelete.Count == 1
                    ? "Không thể xóa cấu hình xếp ca."
                    : "Không thể xóa danh sách cấu hình xếp ca.");
        }
        finally
        {
            IsDeletingSettings = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeNullable(value);
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal)
            && string.Equals(AppliedSearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        AppliedSearchText = normalizedValue;
        await ClearSelectionAsync();
        ApplySearchFilter();
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private async Task OnCancelSettingEditClick()
    {
        if (Grid is not null)
        {
            await Grid.CancelEditAsync();
        }
    }

    private async Task OnClearSearchClick()
    {
        if (!HasSearchText)
        {
            return;
        }

        SearchText = null;
        AppliedSearchText = null;
        await ClearSelectionAsync();
        ApplySearchFilter();
    }

    private void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
    {
        EditErrorMessage = null;
        IsCreatingNewSetting = e.IsNew;

        var model = (ShiftSchedulingSettingRecord)e.EditModel;
        if (e.IsNew)
        {
            InitializeNewSettingDefaults(model);
        }
    }

    private async Task OnEditModelSaving(GridEditModelSavingEventArgs e)
    {
        EditErrorMessage = null;

        try
        {
            var editModel = (ShiftSchedulingSettingRecord)e.EditModel;
            if (e.IsNew)
            {
                editModel.ClassificationType = NewSettingClassificationType;
            }

            NormalizeEditModel(editModel);

            if (ShouldSaveEmployeeBatch(e, editModel))
            {
                await SaveEmployeeBatchAsync(e, editModel);
                return;
            }

            if (editModel.Id == Guid.Empty)
            {
                editModel.Id = Guid.NewGuid();
            }

            var now = DateTime.UtcNow;
            if (editModel.CreatedAtUtc == default)
            {
                editModel.CreatedAtUtc = now;
            }

            editModel.UpdatedAtUtc = now;

            var validationMessage = await DataProvider.ValidateAsync(editModel, disposalTokenSource.Token);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                EditErrorMessage = validationMessage;
                e.Cancel = true;
                return;
            }

            IsSavingSetting = true;
            CurrentLoadingText = e.IsNew
                ? "Đang tạo cấu hình xếp ca..."
                : "Đang cập nhật cấu hình xếp ca...";

            var settings = await DataProvider.SaveAsync(editModel, e.IsNew, disposalTokenSource.Token);
            UpdateLoadedSettings(settings);
            e.Reload = false;
            await ClearSelectionAsync();
            ToastService.ShowSuccess(e.IsNew ? "Đã thêm cấu hình xếp ca." : "Đã cập nhật cấu hình xếp ca.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }

            e.Cancel = true;
        }
        catch (InvalidOperationException ex)
        {
            EditErrorMessage = ex.Message;
            e.Cancel = true;
            ToastService.ShowError("Không thể lưu cài đặt xếp ca.");
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể lưu dữ liệu cài đặt xếp ca. Vui lòng kiểm tra lại thông tin.";
            e.Cancel = true;
            ToastService.ShowError("Không thể lưu cài đặt xếp ca.");
        }
        finally
        {
            IsSavingSetting = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private static bool ShouldSaveEmployeeBatch(
        GridEditModelSavingEventArgs e,
        ShiftSchedulingSettingRecord editModel) =>
        e.IsNew
        && editModel.ClassificationType == ShiftSchedulingClassificationType.TheoNhanVien
        && editModel.EmployeeTargets.Count > 0;

    private async Task SaveEmployeeBatchAsync(
        GridEditModelSavingEventArgs e,
        ShiftSchedulingSettingRecord editModel)
    {
        var employeeTargets = editModel.EmployeeTargets
            .GroupBy(target => target.Id)
            .Select(group => group.First())
            .ToArray();

        if (employeeTargets.Length == 0)
        {
            EditErrorMessage = "Hãy chọn ít nhất một nhân viên.";
            e.Cancel = true;
            return;
        }

        var now = DateTime.UtcNow;
        var settingsToSave = employeeTargets
            .Select(target => CreateEmployeeSetting(editModel, target, now))
            .ToArray();

        foreach (var setting in settingsToSave)
        {
            var validationMessage = await DataProvider.ValidateAsync(setting, disposalTokenSource.Token);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                EditErrorMessage = validationMessage;
                e.Cancel = true;
                return;
            }
        }

        IsSavingSetting = true;
        CurrentLoadingText = "Đang tạo cấu hình xếp ca theo nhân viên...";

        IReadOnlyList<ShiftSchedulingSettingRecord> latestSettings = AllSettings;
        foreach (var setting in settingsToSave)
        {
            latestSettings = await DataProvider.SaveAsync(setting, isNew: true, disposalTokenSource.Token);
        }

        UpdateLoadedSettings(latestSettings);
        e.Reload = false;
        await ClearSelectionAsync();
        ToastService.ShowSuccess($"Đã thêm {settingsToSave.Length} cấu hình xếp ca theo nhân viên.");
    }

    private static ShiftSchedulingSettingRecord CreateEmployeeSetting(
        ShiftSchedulingSettingRecord source,
        ShiftSchedulingEmployeeTargetRecord employeeTarget,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            ShiftId = source.ShiftId,
            ShiftCode = source.ShiftCode,
            ShiftName = source.ShiftName,
            ShiftStartTime = source.ShiftStartTime,
            ShiftEndTime = source.ShiftEndTime,
            ClassificationType = ShiftSchedulingClassificationType.TheoNhanVien,
            Value = employeeTarget.Value,
            AssignmentScopeMode = source.AssignmentScopeMode,
            EffectiveFromDate = source.EffectiveFromDate,
            EffectiveToDate = source.EffectiveToDate,
            IsActive = source.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = null
        };

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("shift-scheduling-settings"),
        "Đã bắt đầu xuất Excel cài đặt xếp ca.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "shift-scheduling-settings-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("shift-scheduling-settings"),
        "Đã bắt đầu xuất PDF cài đặt xếp ca.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "shift-scheduling-settings-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu cài đặt xếp ca.");
        }
    }

    #endregion

    #region Helpers

    private static string GetActivityStatusCssClass(bool isActive) => isActive
        ? "shift-scheduling-status shift-scheduling-status-active"
        : "shift-scheduling-status shift-scheduling-status-inactive";

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private void UpdateLoadedSettings(IReadOnlyList<ShiftSchedulingSettingRecord> settings)
    {
        AllSettings = settings.ToArray();
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        VisibleSettings = BuildSearchScopedSettings();
    }

    private IReadOnlyList<ShiftSchedulingSettingRecord> BuildSearchScopedSettings()
    {
        if (!HasSearchText)
        {
            return AllSettings;
        }

        var searchText = AppliedSearchText!;
        return AllSettings
            .Where(setting => MatchesSearch(setting, searchText))
            .ToArray();
    }

    private static bool MatchesSearch(ShiftSchedulingSettingRecord setting, string searchText) =>
        ContainsSearchText(setting.ClassificationTypeText, searchText)
        || ContainsSearchText(setting.ShiftDisplayText, searchText)
        || ContainsSearchText(setting.Value, searchText)
        || ContainsSearchText(setting.AssignmentScopeModeText, searchText)
        || ContainsSearchText(setting.ActivityStatusText, searchText)
        || ContainsSearchText(setting.EffectiveDateRangeText, searchText);

    private static bool ContainsSearchText(string? source, string searchText) =>
        !string.IsNullOrWhiteSpace(source)
        && source.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = value ?? string.Empty;
        if (!HasSearchText || displayText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = AppliedSearchText!;
        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);

        while (true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"shift-scheduling-settings-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if (builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    private List<ShiftSchedulingSettingRecord> GetSelectedSettings() =>
        SelectedDataItems.OfType<ShiftSchedulingSettingRecord>().ToList();

    private ShiftSchedulingSettingRecord? GetSingleSelectedSetting()
    {
        var selectedSettings = GetSelectedSettings();
        return selectedSettings.Count == 1 ? selectedSettings[0] : null;
    }

    private int GetSelectedSettingCount() => GetSelectedSettings().Count;

    private static void NormalizeEditModel(ShiftSchedulingSettingRecord model)
    {
        model.Value = NormalizeNullable(model.Value);
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void InitializeNewSettingDefaults(ShiftSchedulingSettingRecord model)
    {
        model.Id = Guid.NewGuid();
        model.ShiftId = null;
        model.ClassificationType = NewSettingClassificationType;
        model.Value = string.Empty;
        model.EffectiveFromDate = null;
        model.EffectiveToDate = null;
        model.EmployeeTargets.Clear();
        model.AssignmentScopeMode = ShiftSchedulingAssignmentScopeMode.CoDinh;
        model.IsActive = true;
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = null;
    }

    #endregion

    #region Disposal

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    #endregion
}
