using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLe
{
    #region Data Loading And Reload Coordination

    // Dùng semaphore + version counter để gộp nhiều yêu cầu reload gần nhau; chỉ snapshot mới nhất cần đi hết pipeline tải dữ liệu.
    /// <summary>Thực hiện xử lý cho luồng <c>ReloadDataAsync</c>.</summary>
    private async Task ReloadDataAsync()
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
                await ReloadDataCoreAsync();
            }
        }
        finally
        {
            reloadGate.Release();
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ReloadDataCoreAsync</c>.</summary>
    private async Task ReloadDataCoreAsync()
    {
        LoadErrorMessage = null;
        ManualEditErrorMessage = null;
        IsLoadingData = true;
        SetLoadingPanelText(DefaultLoadingText);

        try
        {
            // Grid đang chuyển sang trạng thái loading ngay sau prepare. Chỉ reset binding tại đây để tránh
            // gọi API chọn dòng của DevExpress khi callback render trước đó chưa hoàn tất.
            SelectedGridItems = [];
            var requestVersion = reloadProcessedVersion;
            var reloadRequest = new LeaveHolidayAllowanceReloadRequest(
                AppliedMonth,
                AppliedYear,
                SearchText,
                CurrentLockFilter,
                CurrentPageIndex,
                PageSize);
            await LoadGridRecordsAsync(reloadRequest, requestVersion, disposalTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Không thể tải danh sách phụ cấp Phép - Lễ cho kỳ {PayrollMonth}/{PayrollYear}.",
                AppliedMonth,
                AppliedYear);
            AllRecords = [];
            VisibleRecords = [];
            LoadErrorMessage = "Có lỗi khi tải dữ liệu phụ cấp Phép - Lễ. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách phụ cấp Phép - Lễ.");
        }
        finally
        {
            IsLoadingData = false;
            SetLoadingPanelText(DefaultLoadingText);
        }
    }

    /// <summary>Tải cho luồng <c>LoadGridRecordsAsync</c>.</summary>
    private async Task LoadGridRecordsAsync(
        LeaveHolidayAllowanceReloadRequest reloadRequest,
        int requestVersion,
        CancellationToken cancellationToken)
    {
        var records = await ExecuteDataOperationAsync(
            token => DataProvider.SearchAsync(FilterFactory.CreateListFilter(reloadRequest), token),
            cancellationToken);

        // A delayed search may complete after a newer reload has been queued. The reload
        // loop will immediately process that newer immutable request instead of applying
        // stale records to the current render.
        if (requestVersion != Volatile.Read(ref reloadRequestedVersion))
        {
            return;
        }

        AllRecords = records;
        ApplyCurrentLockFilter();
    }

    // Sau các batch command, server có thể cập nhật nhiều dòng detail và sync summary cùng lúc nên cần reload toàn snapshot đang hiển thị.
    /// <summary>Thực hiện xử lý cho luồng <c>ReloadSnapshotAfterBatchActionAsync</c>.</summary>
    private Task ReloadSnapshotAfterBatchActionAsync() => ReloadDataAsync();

    #endregion
}
