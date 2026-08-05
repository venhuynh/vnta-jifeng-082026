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
    #region Selection Handling

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedGridItemsChangedAsync</c>.</summary>
    private Task OnSelectedGridItemsChangedAsync(IReadOnlyList<object> items)
    {
        SelectedGridItems = items;
        return Task.CompletedTask;
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ClearGridSelectionAsync</c>.</summary>
    private async Task ClearGridSelectionAsync()
    {
        SelectedGridItems = [];

        if (GridSection is null)
        {
            return;
        }

        await GridSection.ClearSelectionAsync();
    }

    // Grid có thể còn giữ object selection cũ khi đổi chip khóa hoặc reload, nên luôn lọc lại theo danh sách đang hiển thị trước khi thao tác.
    /// <summary>Lấy cho luồng <c>GetSelectedVisibleRecords</c>.</summary>
    private List<LeaveHolidayAllowanceRecord> GetSelectedVisibleRecords() =>
        SelectedGridItems
            .OfType<LeaveHolidayAllowanceRecord>()
            .Where(IsVisibleRecord)
            .DistinctBy(row => row.Id)
            .ToList();

    /// <summary>Kiểm tra trạng thái cho luồng <c>IsVisibleRecord</c>.</summary>
    private bool IsVisibleRecord(LeaveHolidayAllowanceRecord record) =>
        VisibleRecords.Any(row => row.Id == record.Id);

    #endregion
}
