using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    private async Task ReloadAsync()
    {
        if (!HasRequestedData || IsDisposalRequested) return;
        Interlocked.Increment(ref reloadState.RequestedVersion);
        if (!await reloadState.Gate.WaitAsync(0, disposalTokenSource.Token)) return;

        try
        {
            while (!IsDisposalRequested && reloadState.ProcessedVersion < Volatile.Read(ref reloadState.RequestedVersion))
            {
                reloadState.ProcessedVersion = Volatile.Read(ref reloadState.RequestedVersion);
                await LoadRecordsAsync();
            }
        }
        finally
        {
            reloadState.Gate.Release();
        }
    }

    private async Task LoadRecordsAsync()
    {
        BeginDataLoad();
        try
        {
            await ClearGridSelectionAsync();
            ApplyLoadedRecords(await DataProvider.SearchAsync(BuildSearchFilter(), disposalTokenSource.Token));
        }
        catch (OperationCanceledException) when (IsDisposalRequested)
        {
        }
        catch (Exception)
        {
            ClearLoadedRecords();
            DataLoadErrorMessage = "Có lỗi khi tải dữ liệu phụ cấp trách nhiệm khác. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải dữ liệu phụ cấp trách nhiệm khác.");
        }
        finally
        {
            EndDataLoad();
        }
    }

    private OtherResponsibilityAllowanceFilter BuildSearchFilter() => new(AppliedMonth, AppliedYear, SearchText);
    private void BeginDataLoad() { DataLoadErrorMessage = null; IsLoading = true; ResetLoadingText(); }
    private void EndDataLoad() { IsLoading = false; ResetLoadingText(); }
    private void ApplyLoadedRecords(IReadOnlyList<OtherResponsibilityAllowanceRecord> records) { AllRecords = records; VisibleRecords = records; }
    private void ClearLoadedRecords() { AllRecords = []; VisibleRecords = []; }
}
