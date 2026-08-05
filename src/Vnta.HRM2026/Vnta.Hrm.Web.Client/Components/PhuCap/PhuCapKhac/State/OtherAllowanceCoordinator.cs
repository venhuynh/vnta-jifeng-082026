using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapKhac;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

/// <summary>Điều phối workflow và state của màn hình Phụ cấp khác.</summary>
public sealed partial class OtherAllowanceCoordinator(
    IOtherAllowanceReadDataProvider ReadDataProvider,
    IOtherAllowanceCreateDataProvider CreateDataProvider,
    IOtherAllowancePreviousMonthSyncDataProvider PreviousMonthSyncDataProvider,
    IOtherAllowanceUpdateDataProvider UpdateDataProvider,
    IOtherAllowanceLockDataProvider LockDataProvider,
    IOtherAllowanceMonthlyWorkDataProvider MonthlyWorkDataProvider,
    ILogger<OtherAllowanceCoordinator> Logger,
    IHrmDialogService DialogService,
    IHrmToastService ToastService) : IOtherAllowanceScreenController
{
    private static readonly int[] MonthOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const int MaximumSearchResultTake = 5000;
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";
    private static readonly int[] PageSizeOptions = [50, 100, 200, MaximumSearchResultTake];
    private const string DefaultLoadingText = "Đang tải dữ liệu phụ cấp khác...";

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim commandGate = new(1, 1);
    private CancellationTokenSource? loadCancellationTokenSource;
    private int loadRequestVersion;
    private int disposed;

    private int ToolbarMonth { get; set; }
    private int ToolbarYear { get; set; }
    private int AppliedMonth { get; set; }
    private int AppliedYear { get; set; }
    private string? SearchText { get; set; }
    private IReadOnlyList<OtherAllowanceListItemDto> Rows { get; set; } = [];
    private IReadOnlyList<object> selectedItems = [];
    private int ServerTotalRecordCount { get; set; }
    private decimal TotalAllowanceAmount { get; set; }
    private int pageSize = PageSizeOptions[0];
    private int currentPageIndex;
    private bool IsLoading { get; set; }
    private bool IsSyncingFromPreviousMonth { get; set; }
    private bool HasRequestedData { get; set; }
    private string? ErrorMessage { get; set; }
    private string LoadingText { get; set; } = DefaultLoadingText;
    private bool IsChangingLockState { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool PendingLockActionState { get; set; }
    private int PendingLockActionMonth { get; set; }
    private int PendingLockActionYear { get; set; }
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    private bool IsEditPopupVisible { get; set; }
    private bool IsSavingEdit { get; set; }
    private bool IsCreateMode { get; set; }
    private PhuCapKhacEditModel EditModel { get; set; } = new();
    private IReadOnlyList<PhuCapKhacEmployeeOption> CreateEmployeeOptions { get; set; } = [];
    private string EditPopupTitle { get; set; } = "Sửa phụ cấp khác";
    private string? EditErrorMessage { get; set; }
    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private string MonthlyWorkPopupTitle { get; set; } = "Đối chiếu bảng công tháng";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    private OtherAllowanceListItemDto? MonthlyWorkPopupRecord { get; set; }
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    private bool IsRulesPopupVisible { get; set; }
}
