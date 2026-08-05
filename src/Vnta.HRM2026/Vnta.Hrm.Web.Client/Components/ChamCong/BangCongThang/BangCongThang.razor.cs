using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Attendance;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.BangCongThang;

// Component chỉ điều phối trạng thái giao diện; mọi dữ liệu bảng công phải đi qua provider và contract Application.
public partial class BangCongThang : IDisposable
{
    // Dữ liệu bảng công chỉ có từ tháng 06/2026; UI và chuẩn hóa kỳ cùng bảo vệ mốc này.
    internal const int MinimumSupportedYear = 2026;
    internal const int MinimumSupportedMonth = 6;
    internal const int MaximumSupportedYear = 2100;
    private static readonly IReadOnlyList<MonthOption> MonthOptions = BuildMonthOptions();
    private static readonly int[] PageSizeOptions = [50, 100, 200];

    // Hủy toàn bộ thao tác nền khi component biến mất; không để callback muộn cập nhật trạng thái đã dispose.
    private readonly CancellationTokenSource disposalTokenSource = new();
    // Chỉ một vòng reload được chạy; phiên bản request vẫn cho phép vòng đang giữ gate xử lý trạng thái mới nhất.
    private readonly SemaphoreSlim summaryReloadGate = new(1, 1);
    // Bộ nhớ đệm calendar và task đang tải dùng chung giữa các event toolbar có thể đến gần như đồng thời.
    private readonly object calendarLoadSync = new();
    private CancellationTokenSource? activeSummaryLoadTokenSource;
    private CancellationTokenSource? activeDetailLoadTokenSource;

    // Phụ thuộc UI
    [Inject]
    private AttendanceWorkCalendarDataProvider AttendanceWorkCalendarDataProvider { get; set; } = default!;

    [Inject]
    private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    private IAttendanceWorkdaySummaryReadService AttendanceWorkdaySummaryReadService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    // Trạng thái giao diện và bộ nhớ đệm
    private IReadOnlyList<MonthlyWorkSummaryGridRowRecord> GridRows { get; set; } = [];
    private IReadOnlyList<MonthlyCalendarColumn> DateColumns { get; set; } = [];
    private IReadOnlyDictionary<string, MonthlyCalendarColumn> DateColumnsByFieldName { get; set; } =
        new Dictionary<string, MonthlyCalendarColumn>();
    private IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType> SpecialDaysByDate { get; set; } =
        new Dictionary<DateOnly, AttendanceWorkCalendarDayType>();
    private Dictionary<int, IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType>> CachedSpecialDaysByYear { get; } = [];
    private Dictionary<int, Task<IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType>>> CalendarLoadsByYear { get; } = [];
    // Kỳ đang nhập chỉ để dựng UI; chỉ appliedPeriod mới được phép gắn với dữ liệu đã query.
    private MonthlyWorkPeriod requestedPeriod = GetDefaultPeriod();
    private MonthlyWorkPeriod? appliedPeriod;
    private string? SearchText { get; set; }
    private int pageSize = 50;
    private int summaryReloadRequestedVersion;
    private int summaryReloadProcessedVersion;
    private int currentPageIndex;
    private int totalEmployeeCount;
    private int GridRenderVersion { get; set; }
    private bool HasInteractedWithToolbar { get; set; }
    private bool IsCalendarLoading { get; set; }
    private bool IsSummaryLoading { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool HasRequestedSummaryLoad { get; set; }
    private string? CalendarLoadErrorMessage { get; set; }
    private string? SummaryLoadErrorMessage { get; set; }
    private AttendanceWorkdaySummaryRecord? DetailSummary { get; set; }
    private bool IsDetailPopupVisible { get; set; }
    private bool IsDetailSummaryLoading { get; set; }
    private int detailLoadVersion;

    private int ToolbarMonth => requestedPeriod.Month;
    private int ToolbarYear => requestedPeriod.Year;
    private IReadOnlyList<MonthOption> AvailableMonthOptions => ToolbarYear == MinimumSupportedYear
        ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
        : MonthOptions;

    private bool IsBusy => IsCalendarLoading || IsSummaryLoading || IsChangingPageSize;
    private bool CanChangeFilters => !IsBusy;
    private bool CanChangePageSize => !IsBusy;
    private bool HasSummaryLoadError => !string.IsNullOrWhiteSpace(SummaryLoadErrorMessage);
    private bool CanBrowsePages => !IsBusy && HasRequestedSummaryLoad && !HasSummaryLoadError && TotalEmployeeCount > 0;
    private int PageSize => pageSize;
    private int CurrentPageIndex => currentPageIndex;
    private int TotalEmployeeCount => totalEmployeeCount;
    private int TotalPageCount => TotalEmployeeCount <= 0 ? 1 : (int)Math.Ceiling(TotalEmployeeCount / (double)PageSize);
    private int CurrentPageNumber => TotalEmployeeCount == 0 ? 0 : CurrentPageIndex + 1;
    private int CurrentPageStartRecord => TotalEmployeeCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalEmployeeCount == 0 ? 0 : Math.Min(TotalEmployeeCount, CurrentPageIndex * PageSize + GridRows.Count);
    private string LoadingText => IsChangingPageSize
        ? "Đang cập nhật số dòng hiển thị..."
        : IsSummaryLoading
        ? $"Đang tải thông tin nhân viên tháng {ToolbarMonth:00}/{ToolbarYear}, trang {Math.Max(1, CurrentPageIndex + 1)}..."
        : $"Đang dựng lại khung cột tháng {ToolbarMonth:00}/{ToolbarYear}...";
    private string WorkingDaysBandCaption => $"Ngày công tháng {ToolbarMonth:00}/{ToolbarYear}";
    private string GridTitle => $"Bảng công tháng - khung cột {ToolbarMonth:00}/{ToolbarYear}";
    private string GridSubtitle => BuildGridSubtitle();
    private string PagerSummaryText => BuildPagerSummaryText();
    private string EmptyStateMessage => BuildEmptyStateMessage();
    // Chỉ tổng hợp dữ liệu đã tải ở trang hiện tại; không thêm truy vấn hoặc tham gia vào điều kiện lọc của DxGrid.
    private IReadOnlyList<AttendanceDayTypeSummary> DayTypeSummaries => BuildDayTypeSummaries();
    private IReadOnlyList<AttendanceResultCodeSummary> AttendanceResultCodeSummaries => BuildAttendanceResultCodeSummaries();

    protected override void OnInitialized()
    {
        // Lần mở đầu chỉ dựng khung ngày, tránh query bảng công trước khi người dùng xác nhận kỳ bằng nút Xem.
        RebuildGridStructure();
        base.OnInitialized();
    }

    private async Task OnViewClickAsync()
    {
        HasInteractedWithToolbar = true;
        currentPageIndex = 0;
        // Chụp kỳ đang chọn thành kỳ áp dụng để kết quả trả về không bị gắn nhầm vào input toolbar mới hơn.
        appliedPeriod = requestedPeriod;
        await ReloadAsync();
    }

    private async Task OnToolbarMonthChangedAsync(int value)
    {
        var updatedPeriod = NormalizePeriod(value, ToolbarYear);
        if(updatedPeriod == requestedPeriod)
        {
            return;
        }

        requestedPeriod = updatedPeriod;
        await HandleToolbarPeriodChangedAsync();
    }

    private async Task OnToolbarYearChangedAsync(int value)
    {
        var updatedPeriod = NormalizePeriod(ToolbarMonth, value);
        if(updatedPeriod == requestedPeriod)
        {
            return;
        }

        requestedPeriod = updatedPeriod;
        await HandleToolbarPeriodChangedAsync();
    }

    private Task OnSearchTextChangedAsync(string? value)
    {
        var normalizedValue = NormalizeSearchText(value);
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        currentPageIndex = 0;
        return HasRequestedSummaryLoad && appliedPeriod.HasValue
            ? ReloadAsync()
            : Task.CompletedTask;
    }

    private async Task HandleToolbarPeriodChangedAsync()
    {
        HasInteractedWithToolbar = true;
        // Vô hiệu hóa mọi summary đang chờ trước khi thay kỳ; đổi toolbar không tự tải bảng công.
        Interlocked.Increment(ref summaryReloadRequestedVersion);
        CancelActiveSummaryLoad();
        appliedPeriod = null;
        ResetSummaryState();
        RebuildGridStructure();
        await InvokeAsync(StateHasChanged);
        await EnsureWorkCalendarYearAsync(ToolbarYear, disposalTokenSource.Token);
    }

    private void ResetSummaryState()
    {
        CancelActiveDetailLoad();
        GridRows = [];
        totalEmployeeCount = 0;
        currentPageIndex = 0;
        SummaryLoadErrorMessage = null;
        HasRequestedSummaryLoad = false;
        DetailSummary = null;
        IsDetailPopupVisible = false;
    }

    private async Task ReloadAsync()
    {
        if(disposalTokenSource.IsCancellationRequested || appliedPeriod is null)
        {
            return;
        }

        // Mỗi trigger tạo một version mới. Nếu đang có reload giữ gate, vòng lặp bên dưới sẽ lấy version mới nhất.
        Interlocked.Increment(ref summaryReloadRequestedVersion);
        CancelActiveSummaryLoad();
        if(!await summaryReloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        IsSummaryLoading = true;
        SummaryLoadErrorMessage = null;
        HasRequestedSummaryLoad = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            while(!disposalTokenSource.IsCancellationRequested
                  && summaryReloadProcessedVersion < Volatile.Read(ref summaryReloadRequestedVersion))
            {
                var requestVersion = Volatile.Read(ref summaryReloadRequestedVersion);
                summaryReloadProcessedVersion = requestVersion;
                if(appliedPeriod is not { } requestPeriod)
                {
                    return;
                }

                await ReloadSummaryCoreAsync(requestVersion, CreateReloadSnapshot(requestPeriod));
            }
        }
        finally
        {
            IsSummaryLoading = false;
            summaryReloadGate.Release();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ReloadSummaryCoreAsync(
        int requestVersion,
        MonthlyWorkSummaryReloadSnapshot snapshot)
    {
        // Token riêng giúp thao tác mới hủy request cũ mà không hủy vòng đời của cả component.
        using var requestTokenSource = BeginSummaryLoad();
        var cancellationToken = requestTokenSource.Token;

        try
        {
            if(!CachedSpecialDaysByYear.ContainsKey(snapshot.Period.Year))
            {
                await EnsureWorkCalendarYearAsync(snapshot.Period.Year, cancellationToken);
            }

            var result = await MonthlyWorkSummaryDataProvider.LoadPageAsync(
                BuildListRequest(snapshot),
                cancellationToken);

            // Không để kết quả trả về cũ ghi đè grid sau khi người dùng đổi kỳ, trang hoặc kích thước trang.
            if(ShouldDiscardSummaryResult(requestVersion, snapshot))
            {
                return;
            }

            if(result.TotalCount > 0)
            {
                var maximumPageIndex = Math.Max(0, (int)Math.Ceiling(result.TotalCount / (double)snapshot.PageSize) - 1);
                if(snapshot.PageIndex > maximumPageIndex)
                {
                    // Dữ liệu có thể giảm giữa hai lần tải; quay về trang cuối hợp lệ rồi để reload loop lấy lại data.
                    currentPageIndex = maximumPageIndex;
                    Interlocked.Increment(ref summaryReloadRequestedVersion);
                    return;
                }
            }

            GridRows = result.Rows;
            totalEmployeeCount = result.TotalCount;
        }
        catch(OperationCanceledException)
        {
            // Hủy do thao tác mới hoặc dispose là bình thường; chỉ ném lại việc hủy còn hiệu lực cho caller.
            if(!disposalTokenSource.IsCancellationRequested && !ShouldDiscardSummaryResult(requestVersion, snapshot))
            {
                throw;
            }
        }
        catch(Exception ex)
        {
            if(ShouldDiscardSummaryResult(requestVersion, snapshot))
            {
                return;
            }

            GridRows = [];
            totalEmployeeCount = 0;
            SummaryLoadErrorMessage = $"Không thể tải attendance_workday_summaries tháng {snapshot.Period.Month:00}/{snapshot.Period.Year}: {ex.Message}";
            ToastService.ShowError("Không thể tải thông tin nhân viên cho Bảng công tháng.");
        }
        finally
        {
            if(ReferenceEquals(activeSummaryLoadTokenSource, requestTokenSource))
            {
                activeSummaryLoadTokenSource = null;
            }
        }
    }

    private bool ShouldDiscardSummaryResult(
        int requestVersion,
        MonthlyWorkSummaryReloadSnapshot snapshot)
    {
        if(requestVersion != Volatile.Read(ref summaryReloadRequestedVersion)
           || appliedPeriod is not { } currentPeriod)
        {
            return true;
        }

        return snapshot != CreateReloadSnapshot(currentPeriod);
    }

    private MonthlyWorkSummaryReloadSnapshot CreateReloadSnapshot(MonthlyWorkPeriod period) =>
        new(period, CurrentPageIndex, PageSize, NormalizeSearchText(SearchText));

    private static MonthlyWorkSummaryPageRequest BuildListRequest(
        MonthlyWorkSummaryReloadSnapshot snapshot)
    {
        var fromDate = new DateOnly(snapshot.Period.Year, snapshot.Period.Month, 1);
        var toDate = new DateOnly(
            snapshot.Period.Year,
            snapshot.Period.Month,
            DateTime.DaysInMonth(snapshot.Period.Year, snapshot.Period.Month));
        return new MonthlyWorkSummaryPageRequest(
            fromDate,
            toDate,
            snapshot.SearchText,
            snapshot.PageIndex * snapshot.PageSize,
            snapshot.PageSize);
    }

    private CancellationTokenSource BeginSummaryLoad()
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        activeSummaryLoadTokenSource = cancellationTokenSource;
        return cancellationTokenSource;
    }

    private void CancelActiveSummaryLoad() => activeSummaryLoadTokenSource?.Cancel();

    private void CancelActiveDetailLoad() => activeDetailLoadTokenSource?.Cancel();

    private void RebuildGridStructure()
    {
        if(!CachedSpecialDaysByYear.TryGetValue(ToolbarYear, out var specialDays))
        {
            specialDays = new Dictionary<DateOnly, AttendanceWorkCalendarDayType>();
        }

        SpecialDaysByDate = specialDays;
        DateColumns = BuildDateColumns(ToolbarMonth, ToolbarYear, specialDays);
        DateColumnsByFieldName = DateColumns.ToDictionary(column => column.FieldName, StringComparer.Ordinal);
        // Cột ngày thay đổi theo kỳ; key mới buộc DxGrid tạo lại cấu trúc cột unbound.
        GridRenderVersion++;
    }

    private async Task EnsureWorkCalendarYearAsync(int year, CancellationToken cancellationToken)
    {
        if(CachedSpecialDaysByYear.TryGetValue(year, out var cachedDays))
        {
            if(year == ToolbarYear)
            {
                SpecialDaysByDate = cachedDays;
                RebuildGridStructure();
                await InvokeAsync(StateHasChanged);
            }

            return;
        }

        // Calendar là dữ liệu tra cứu bổ trợ cho màu ngày; summary vẫn có thể hiển thị ở chế độ suy giảm nếu tải calendar thất bại.
        IsCalendarLoading = true;
        CalendarLoadErrorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var specialDays = await GetCalendarSpecialDaysAsync(year).WaitAsync(cancellationToken);
            if(year == ToolbarYear)
            {
                SpecialDaysByDate = specialDays;
                RebuildGridStructure();
            }
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
            lock(calendarLoadSync)
            {
                CalendarLoadsByYear.Remove(year);
            }

            if(year == ToolbarYear)
            {
                CalendarLoadErrorMessage = $"Không thể tải lịch làm việc năm {year}: {ex.Message}";
                SpecialDaysByDate = new Dictionary<DateOnly, AttendanceWorkCalendarDayType>();
                RebuildGridStructure();
                ToastService.ShowWarning("Không thể tải lịch làm việc để đánh dấu ngày lễ. Màn hiện chỉ tô màu Chủ nhật theo rule mặc định.");
            }
        }
        finally
        {
            IsCalendarLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task<IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType>> GetCalendarSpecialDaysAsync(int year)
    {
        lock(calendarLoadSync)
        {
            if(CachedSpecialDaysByYear.TryGetValue(year, out var cachedDays))
            {
                return Task.FromResult(cachedDays);
            }

            if(CalendarLoadsByYear.TryGetValue(year, out var existingLoad))
            {
                // Chỉ một lần tải theo năm để nhiều event không cùng gọi server cho một calendar.
                return existingLoad;
            }

            var calendarLoad = LoadCalendarSpecialDaysAsync(year);
            CalendarLoadsByYear[year] = calendarLoad;
            return calendarLoad;
        }
    }

    private async Task<IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType>> LoadCalendarSpecialDaysAsync(int year)
    {
        var calendarDays = await AttendanceWorkCalendarDataProvider.GetYearAsync(
            year,
            disposalTokenSource.Token);

        // Một ngày có thể có nhiều record legacy; ngày lễ được ưu tiên để UI luôn giữ trạng thái đặc biệt mạnh nhất.
        var specialDays = calendarDays
            .Where(day => day.WorkDateOnly.HasValue && AttendanceWorkCalendarDayTypes.IsSpecialDay(day.DayType))
            .GroupBy(day => day.WorkDateOnly!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(day => day.DayType == AttendanceWorkCalendarDayType.Holiday)
                    .First()
                    .DayType);

        CachedSpecialDaysByYear[year] = specialDays;
        return specialDays;
    }

    private string BuildGridSubtitle()
    {
        if(DateColumns.Count == 0)
        {
            return "Chưa có cột ngày để hiển thị.";
        }

        var fromDate = DateColumns[0].WorkDate;
        var toDate = DateColumns[^1].WorkDate;
        if(!HasRequestedSummaryLoad)
        {
            return $"{DateColumns.Count:N0} cột ngày từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}.";
        }

        if(TotalEmployeeCount == 0)
        {
            return string.IsNullOrWhiteSpace(SearchText)
                ? $"{DateColumns.Count:N0} cột ngày từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy} | chưa có nhân viên."
                : $"{DateColumns.Count:N0} cột ngày từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy} | không có kết quả tìm kiếm.";
        }

        return $"{DateColumns.Count:N0} cột ngày từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy} | hiển thị {GridRows.Count:N0}/{TotalEmployeeCount:N0} nhân viên.";
    }

    private string BuildPagerSummaryText()
    {
        if(!HasRequestedSummaryLoad || HasSummaryLoadError || TotalEmployeeCount == 0)
        {
            return "Chưa có trang dữ liệu";
        }

        return $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalEmployeeCount:N0} nhân viên";
    }

    private string BuildEmptyStateMessage()
    {
        if(!HasInteractedWithToolbar)
        {
            return "Màn chỉ dựng trước khung DxGrid ở client để phục vụ refactor layout. Chưa tải danh sách nhân viên hay attendance workday summaries từ database.";
        }

        if(!HasRequestedSummaryLoad)
        {
            return "Khung cột đã cập nhật theo tháng/năm đang chọn. Bấm Xem để tải thông tin nhân viên từ attendance_workday_summaries.";
        }

        if(!string.IsNullOrWhiteSpace(SummaryLoadErrorMessage))
        {
            return SummaryLoadErrorMessage;
        }

        if(!string.IsNullOrWhiteSpace(CalendarLoadErrorMessage))
        {
            return "Đã tải được nhân viên cho kỳ đang chọn, nhưng chưa lấy được lịch làm việc để tô ngày lễ. Chủ nhật vẫn được đánh dấu theo rule mặc định.";
        }

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            return "Không tìm thấy nhân viên phù hợp với từ khóa tìm kiếm trong kỳ đang chọn.";
        }

        return "Không tìm thấy dữ liệu attendance_workday_summaries trong kỳ đang chọn để dựng danh sách nhân viên.";
    }

    private IReadOnlyList<AttendanceDayTypeSummary> BuildDayTypeSummaries()
    {
        var dayTypeByDate = DateColumns.ToDictionary(column => column.WorkDate, column => column.DayType);
        var counts = new Dictionary<AttendanceWorkCalendarDayType, int>
        {
            [AttendanceWorkCalendarDayType.Regular] = 0,
            [AttendanceWorkCalendarDayType.Holiday] = 0,
            [AttendanceWorkCalendarDayType.DayOff] = 0
        };

        foreach(var dayCell in GridRows.SelectMany(row => row.DayCellsByDate.Values))
        {
            if(dayTypeByDate.TryGetValue(dayCell.WorkDate, out var dayType))
            {
                counts[dayType]++;
            }
        }

        return
        [
            new(
                AttendanceWorkCalendarDayTypes.Regular,
                counts[AttendanceWorkCalendarDayType.Regular],
                "bang-cong-thang-summary-button bang-cong-thang-summary-button--regular"),
            new(
                AttendanceWorkCalendarDayTypes.Holiday,
                counts[AttendanceWorkCalendarDayType.Holiday],
                "bang-cong-thang-summary-button bang-cong-thang-summary-button--holiday"),
            new(
                AttendanceWorkCalendarDayTypes.DayOff,
                counts[AttendanceWorkCalendarDayType.DayOff],
                "bang-cong-thang-summary-button bang-cong-thang-summary-button--day-off")
        ];
    }

    private IReadOnlyList<AttendanceResultCodeSummary> BuildAttendanceResultCodeSummaries() =>
        GridRows
            .SelectMany(row => row.DayCellsByDate.Values)
            .GroupBy(dayCell => NormalizeAttendanceResultCode(dayCell.Status), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetAttendanceResultCodeSortOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AttendanceResultCodeSummary(group.Key, group.Count()))
            .ToArray();

    private static string NormalizeAttendanceResultCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? "--" : code.Trim().ToUpperInvariant();

    private static int GetAttendanceResultCodeSortOrder(string code) => code switch
    {
        "FULL_WORK" => 0,
        "VR" => 1,
        "LATE_EARLY" => 2,
        "MISSING_LOG" => 3,
        "TS" => 4,
        "ABNORMAL" => 5,
        "KP" => 6,
        "--" => int.MaxValue,
        _ => 100
    };

    private string GetDayBandHeaderCssClass(MonthlyCalendarColumn column) => column.IsSpecialDay
        ? "bang-cong-thang-day-band-header bang-cong-thang-day-band-header-special"
        : "bang-cong-thang-day-band-header";

    private string GetDayHeaderCssClass(MonthlyCalendarColumn column) => column.IsSpecialDay
        ? "bang-cong-thang-day-header bang-cong-thang-day-header-special"
        : "bang-cong-thang-day-header";

    private static string GetDayCellCssClass(MonthlyWorkSummaryDayCellRecord? summary)
    {
        var classes = "bang-cong-thang-day-cell";
        if(summary is not null)
        {
            classes += " bang-cong-thang-day-cell-clickable";
            if(!HasDayCellTime(summary))
            {
                classes += " bang-cong-thang-day-cell-no-time";
            }
        }

        return summary?.IsLocked == true
            ? $"{classes} bang-cong-thang-day-cell-locked"
            : classes;
    }

    private static MonthlyWorkSummaryDayCellRecord? GetDaySummary(
        MonthlyWorkSummaryGridRowRecord row,
        MonthlyCalendarColumn column) =>
        row.DayCellsByDate.TryGetValue(column.WorkDate, out var summary) ? summary : null;

    private static string GetDayCellLockIconUrl(MonthlyWorkSummaryDayCellRecord summary) => summary.IsLocked
        ? VntaDevExpressIcons.Lock
        : VntaDevExpressIcons.Unlock;

    private static string GetDayCellLockIconCssClass(MonthlyWorkSummaryDayCellRecord summary) => summary.IsLocked
        ? "bang-cong-thang-day-cell-lock-icon is-locked"
        : "bang-cong-thang-day-cell-lock-icon is-unlocked";

    private static string GetDayCellLockStatusText(MonthlyWorkSummaryDayCellRecord summary) => summary.IsLocked
        ? "Ngày công đã khóa"
        : "Ngày công đang mở";

    private static bool HasDayCellTime(MonthlyWorkSummaryDayCellRecord summary) =>
        summary.CheckInDisplay is not null || summary.CheckOutDisplay is not null;

    private static string GetAttendanceResultCode(MonthlyWorkSummaryDayCellRecord summary) =>
        GetDisplayValue(summary.Status);

    private async Task OpenDayDetailAsync(
        MonthlyWorkSummaryGridRowRecord employee,
        MonthlyWorkSummaryDayCellRecord? dayCell)
    {
        if(dayCell is null || IsDetailSummaryLoading || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var requestVersion = Interlocked.Increment(ref detailLoadVersion);
        CancelActiveDetailLoad();
        using var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        activeDetailLoadTokenSource = requestTokenSource;
        IsDetailSummaryLoading = true;

        try
        {
            // Grid tháng chỉ giữ dữ liệu tối thiểu để paging nhanh. Khi click mới lấy snapshot đầy đủ cho UI dùng chung Bảng công ngày.
            var rows = await AttendanceWorkdaySummaryReadService.SearchAsync(
                new AttendanceWorkdaySummaryFilter(
                    dayCell.WorkDate,
                    dayCell.WorkDate,
                    GetEmployeeDetailSearchText(employee),
                    Take: 100),
                requestTokenSource.Token);
            var detail = rows.FirstOrDefault(row => row.Id == dayCell.Id);

            if(requestTokenSource.IsCancellationRequested || requestVersion != Volatile.Read(ref detailLoadVersion))
            {
                return;
            }

            if(detail is null)
            {
                ToastService.ShowWarning("Không tìm thấy dữ liệu chi tiết của ngày công đã chọn.");
                return;
            }

            DetailSummary = MapDetailSummary(detail);
            IsDetailPopupVisible = true;
        }
        catch(OperationCanceledException) when(requestTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception)
        {
            if(requestVersion == Volatile.Read(ref detailLoadVersion))
            {
                ToastService.ShowError("Không thể tải chi tiết ngày công đã chọn.");
            }
        }
        finally
        {
            if(ReferenceEquals(activeDetailLoadTokenSource, requestTokenSource))
            {
                activeDetailLoadTokenSource = null;
            }

            if(requestVersion == Volatile.Read(ref detailLoadVersion))
            {
                IsDetailSummaryLoading = false;
            }
        }
    }

    private Task OnDetailSummarySaved(AttendanceWorkdaySummaryRecord updatedSummary)
    {
        DetailSummary = updatedSummary;
        GridRows = GridRows
            .Select(row => row.Id != updatedSummary.EmployeeId
                ? row
                : new MonthlyWorkSummaryGridRowRecord
                {
                    Id = row.Id,
                    RowNumber = row.RowNumber,
                    EmployeeCode = row.EmployeeCode,
                    EmployeeName = row.EmployeeName,
                    DepartmentName = row.DepartmentName,
                    PositionName = row.PositionName,
                    DayCellsByDate = new Dictionary<DateOnly, MonthlyWorkSummaryDayCellRecord>(row.DayCellsByDate)
                    {
                        [updatedSummary.WorkDate] = MapDayCell(updatedSummary)
                    }
                })
            .ToArray();

        return Task.CompletedTask;
    }

    private static string? GetEmployeeDetailSearchText(MonthlyWorkSummaryGridRowRecord employee) =>
        employee.EmployeeCode != "--" ? employee.EmployeeCode : employee.EmployeeName != "--" ? employee.EmployeeName : null;

    private static AttendanceWorkdaySummaryRecord MapDetailSummary(AttendanceWorkdaySummaryListItemDto row) =>
        new()
        {
            Id = row.Id,
            EmployeeId = row.EmployeeId,
            EmployeeCode = row.EmployeeCode,
            EmployeeName = row.EmployeeName,
            DepartmentName = row.DepartmentName,
            PositionName = row.PositionName,
            WorkDate = row.WorkDate,
            DayType = row.DayType,
            ShiftId = row.ShiftId,
            ShiftCode = row.ShiftCode,
            ShiftShortName = row.ShiftShortName,
            ShiftName = row.ShiftName,
            ShiftColorHex = row.ShiftColorHex,
            ScheduledStartAt = row.ScheduledStartAt,
            ScheduledEndAt = row.ScheduledEndAt,
            CheckInAt = row.CheckInAt,
            CheckOutAt = row.CheckOutAt,
            LateMinutes = row.LateMinutes,
            EarlyLeaveMinutes = row.EarlyLeaveMinutes,
            Status = row.Status,
            IsLocked = row.IsLocked,
            OvertimeMinutes = row.OvertimeMinutes,
            OvertimeMinutes15 = row.OvertimeMinutes15,
            OvertimeMinutes20 = row.OvertimeMinutes20,
            OvertimeMinutes30 = row.OvertimeMinutes30,
            CheckInForOT15 = row.CheckInForOT15,
            IsRegisterForOT = row.IsRegisterForOT,
            RequireDocument = row.RequireDocument,
            Note = row.Note,
            ComputedAtUtc = row.ComputedAtUtc,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc
        };

    private static MonthlyWorkSummaryDayCellRecord MapDayCell(AttendanceWorkdaySummaryRecord summary) =>
        new()
        {
            Id = summary.Id,
            WorkDate = summary.WorkDate,
            DayType = summary.DayType,
            ShiftCode = summary.ShiftCode,
            ShiftShortName = summary.ShiftShortName,
            ShiftName = summary.ShiftName,
            ShiftColorHex = summary.ShiftColorHex,
            CheckInAt = summary.CheckInAt,
            CheckOutAt = summary.CheckOutAt,
            LateMinutes = summary.LateMinutes,
            EarlyLeaveMinutes = summary.EarlyLeaveMinutes,
            Status = summary.Status,
            IsLocked = summary.IsLocked,
            OvertimeMinutes = summary.OvertimeMinutes,
            OvertimeMinutes15 = summary.OvertimeMinutes15,
            OvertimeMinutes20 = summary.OvertimeMinutes20,
            OvertimeMinutes30 = summary.OvertimeMinutes30,
            ComputedAtUtc = summary.ComputedAtUtc,
            CreatedAtUtc = summary.CreatedAtUtc,
            UpdatedAtUtc = summary.UpdatedAtUtc
        };

    private static string GetDayCellDetailAriaLabel(MonthlyWorkSummaryDayCellRecord? summary) => summary is null
        ? "Ô ngày công trống"
        : $"Mở chi tiết ngày công. {GetDayCellTooltip(summary)}";

    private static string GetEmployeeCellTooltip(MonthlyWorkSummaryGridRowRecord employee) =>
        $"{employee.EmployeeCode} {employee.EmployeeName}{Environment.NewLine}{employee.DepartmentName} - {employee.PositionName}";

    private static string GetDayCellTooltip(MonthlyWorkSummaryDayCellRecord? summary)
    {
        if(summary is null)
        {
            return string.Empty;
        }

        var parts = new[]
        {
            $"Vào/Ra: {summary.CheckInOutDisplay}",
            $"Trạng thái: {GetDisplayValue(summary.Status)}",
            $"Loại ngày: {summary.DayTypeDisplay}",
            summary.LateEarlyTotalMinutes > 0 ? $"Đi trễ/về sớm: {summary.LateEarlyTotalMinutes:N0} phút" : null,
            summary.OvertimeMinutes > 0 ? $"Tăng ca: {summary.OvertimeMinutes:N0} phút" : null,
            summary.IsLocked ? "Khóa công: Đã khóa" : "Khóa công: Đang mở"
        };

        return string.Join(" | ", parts.Where(static part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string GetDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();

    private static string? NormalizeSearchText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void OnGridUnboundColumnData(GridUnboundColumnDataEventArgs e)
    {
        if(e.DataItem is not MonthlyWorkSummaryGridRowRecord row)
        {
            return;
        }

        if(!DateColumnsByFieldName.ContainsKey(e.FieldName))
        {
            return;
        }

        // Cell template tự render giờ, mã kết quả và icon khóa; field unbound chỉ giữ chỗ cho cột động của DxGrid.
        e.Value = string.Empty;
    }

    private async Task OnPageSizeChangedAsync(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : PageSizeOptions[0];
        if(pageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        try
        {
            // Giữ bản ghi đầu đang nhìn thấy khi đổi page size để thao tác không nhảy về đầu danh sách.
            var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
            pageSize = normalizedValue;
            currentPageIndex = firstVisibleRecordIndex / pageSize;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            if(HasRequestedSummaryLoad && appliedPeriod.HasValue)
            {
                await ReloadAsync();
            }
        }
        finally
        {
            IsChangingPageSize = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if(!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if(normalizedValue == currentPageIndex)
        {
            return;
        }

        currentPageIndex = normalizedValue;
        await ReloadAsync();
    }

    private static IReadOnlyList<MonthlyCalendarColumn> BuildDateColumns(
        int month,
        int year,
        IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType> specialDays)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var columns = new List<MonthlyCalendarColumn>(daysInMonth);

        for(var day = 1; day <= daysInMonth; day++)
        {
            var workDate = new DateOnly(year, month, day);
            var dayType = ResolveDayType(workDate, specialDays);

            columns.Add(new MonthlyCalendarColumn(
                workDate,
                $"{day:00}/{month:00}",
                GetWeekdayText(workDate.DayOfWeek),
                dayType));
        }

        return columns;
    }

    private static AttendanceWorkCalendarDayType ResolveDayType(
        DateOnly workDate,
        IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType> specialDays)
    {
        if(specialDays.TryGetValue(workDate, out var configuredDayType))
        {
            return configuredDayType;
        }

        return AttendanceWorkCalendarDayTypes.ResolveDefaultDayType(workDate);
    }

    private static string GetWeekdayText(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        _ => "CN"
    };

    public void Dispose()
    {
        // Hủy trước khi dispose để các request đang chờ không tiếp tục chạm vào component.
        CancelActiveSummaryLoad();
        CancelActiveDetailLoad();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        summaryReloadGate.Dispose();
    }

    private sealed record MonthlyCalendarColumn(
        DateOnly WorkDate,
        string DateCaption,
        string WeekdayText,
        AttendanceWorkCalendarDayType DayType)
    {
        public string FieldName => $"Day_{WorkDate:yyyy_MM_dd}";

        public string BandName => $"Band_{WorkDate:yyyy_MM_dd}";

        public bool IsSpecialDay =>
            WorkDate.DayOfWeek == DayOfWeek.Sunday
            || DayType == AttendanceWorkCalendarDayType.DayOff
            || DayType == AttendanceWorkCalendarDayType.Holiday;
    }

    private sealed record AttendanceDayTypeSummary(string Label, int Count, string CssClass)
    {
        public string Tooltip => $"{Label}: {Count:N0} công của nhân viên trên trang hiện tại. Chỉ để xem, không lọc dữ liệu.";
    }

    private sealed record AttendanceResultCodeSummary(string Code, int Count)
    {
        public string Tooltip => $"CODE {Code}: {Count:N0} công của nhân viên trên trang hiện tại. Chỉ để xem, không lọc dữ liệu.";
    }

    private readonly record struct MonthlyWorkPeriod(int Month, int Year);

    // Snapshot giữ nguyên kỳ, trang và page size của request để kết quả cũ không làm lệch ma trận đang hiển thị.
    private readonly record struct MonthlyWorkSummaryReloadSnapshot(
        MonthlyWorkPeriod Period,
        int PageIndex,
        int PageSize,
        string? SearchText);

    private static MonthlyWorkPeriod GetDefaultPeriod()
    {
        var now = DateTime.Today;
        return NormalizePeriod(now.Month, now.Year);
    }

    private static MonthlyWorkPeriod NormalizePeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        if(normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            normalizedMonth = MinimumSupportedMonth;
        }

        return new MonthlyWorkPeriod(normalizedMonth, normalizedYear);
    }

    private static IReadOnlyList<MonthOption> BuildMonthOptions() =>
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();

    private sealed record MonthOption(int Value, string Text);
}
