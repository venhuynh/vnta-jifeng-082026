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
    #region Busy State And State Update Helpers

    // Gom busy state của màn cho các thao tác dài để toolbar, grid và loading panel cùng phản ánh một trạng thái đang xử lý.
    /// <summary>Thực hiện xử lý cho luồng <c>RunScreenActionAsync</c>.</summary>
    private async Task RunScreenActionAsync(string loadingText, Func<Task> action)
    {
        IsProcessingScreenAction = true;
        SetLoadingPanelText(loadingText);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            await action();
        }
        finally
        {
            IsProcessingScreenAction = false;
            SetLoadingPanelText(DefaultLoadingText);
        }
    }

    private async Task ExecuteDataOperationAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await dataOperationGate.WaitAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
        }
        finally
        {
            dataOperationGate.Release();
        }
    }

    private async Task<T> ExecuteDataOperationAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await dataOperationGate.WaitAsync(cancellationToken);
        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            dataOperationGate.Release();
        }
    }

    // Chip khóa hiện chỉ lọc trên client-side để không phát sinh thêm request và để selection/badge tổng bám đúng tập dữ liệu đang hiển thị.
    /// <summary>Áp dụng cho luồng <c>ApplyCurrentLockFilter</c>.</summary>
    private void ApplyCurrentLockFilter()
    {
        IEnumerable<LeaveHolidayAllowanceRecord> rows = AllRecords;

        if (CurrentLockFilter == LeaveHolidayAllowanceLockFilter.OpenOnly)
        {
            rows = rows.Where(row => !row.IsLocked);
        }
        else if (CurrentLockFilter == LeaveHolidayAllowanceLockFilter.LockedOnly)
        {
            rows = rows.Where(row => row.IsLocked);
        }

        VisibleRecords = rows.ToArray();
        ClampCurrentPageIndex();
    }

    /// <summary>Đưa chỉ số trang về phạm vi hợp lệ sau khi tập bản ghi thay đổi.</summary>
    private void ClampCurrentPageIndex()
    {
        currentPageIndex = Math.Clamp(currentPageIndex, 0, Math.Max(0, TotalPageCount - 1));
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ReplaceUpdatedRecordInState</c>.</summary>
    private void ReplaceUpdatedRecordInState(LeaveHolidayAllowanceRecord updatedRecord)
    {
        AllRecords = AllRecords
            .Select(record => record.Id == updatedRecord.Id ? updatedRecord : record)
            .ToArray();
        ApplyCurrentLockFilter();

        SelectedGridItems = SelectedGridItems
            .Select(item => item is LeaveHolidayAllowanceRecord record && record.Id == updatedRecord.Id
                ? updatedRecord
                : item)
            .Where(item => item is not LeaveHolidayAllowanceRecord record || VisibleRecords.Any(row => row.Id == record.Id))
            .ToArray();

        if (updatedRecord.IsLocked
            && IsManualEditPopupVisible
            && ManualEditModel?.Id == updatedRecord.Id)
        {
            ResetManualEditPopupState();
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>SetLoadingPanelText</c>.</summary>
    private void SetLoadingPanelText(string value)
    {
        LoadingPanelText = value;
    }

    /// <summary>Áp dụng cho luồng <c>ApplyToolbarPeriod</c>.</summary>
    private void ApplyToolbarPeriod()
    {
        (ToolbarMonth, ToolbarYear) = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
    }

    #endregion
}
