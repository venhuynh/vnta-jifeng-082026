using DevExpress.Blazor;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Export;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Sections;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Mutable UI state owned by the hazard allowance screen.</summary>
public partial class PhuCapDocHai
{
    private IReadOnlyList<HazardAllowanceListItemDto> LoadedRecords { get; set; } = [];
    private int TotalCount { get; set; }
    private IReadOnlyList<HazardAllowanceListItemDto> ExportRecords { get; set; } = [];
    private HazardAllowanceSummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0);
    private IReadOnlyList<object> SelectedGridItems { get; set; } = [];
    private IGrid? Grid => ResultsGrid?.Grid;
    private PhuCapDocHaiGrid? ResultsGrid { get; set; }
    private PhuCapDocHaiExportGrid? ExportSource { get; set; }
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    private bool HasLoadError { get; set; }
    private string LoadingText { get; set; } = DefaultLoadingText;
    private int ToolbarMonth { get; set; } = MinimumSupportedMonth;
    private int ToolbarYear { get; set; } = MinimumSupportedYear;
    private int AppliedMonth { get; set; } = MinimumSupportedMonth;
    private int AppliedYear { get; set; } = MinimumSupportedYear;
    private int PageSize { get; set; } = PageSizeOptions[0].Value;
    private int PageIndex { get; set; }
    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
    private string? SearchText { get; set; }
    private bool HasRequestedData { get; set; }
    private bool IsLoading { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsExporting { get; set; }
    private bool IsRulesPopupVisible { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool IsLockActionProcessing { get; set; }
    private bool PendingLockActionState { get; set; }
    private int PendingLockActionMonth { get; set; }
    private int PendingLockActionYear { get; set; }
    private string SelectedLockActionScope { get; set; } = LockScopeWholePeriod;
    private bool IsSavingEdit { get; set; }
    private bool IsRecalculating { get; set; }
    private bool IsRefreshingRow { get; set; }
    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private string MonthlyWorkPopupTitle { get; set; } = "Bảng công tháng";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private HazardAllowanceListItemDto? MonthlyWorkPopupRecord { get; set; }
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    private PhuCapDocHaiEditModel EditModel { get; set; } = new();
    private decimal VisibleAllowanceTotal { get; set; }
    private bool IsAllowanceTotalSyncPending { get; set; }
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;
}
