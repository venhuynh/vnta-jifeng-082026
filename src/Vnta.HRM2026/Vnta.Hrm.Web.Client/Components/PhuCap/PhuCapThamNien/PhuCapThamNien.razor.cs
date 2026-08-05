using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>
/// Quản lý giao diện, dữ liệu và các thao tác nghiệp vụ của màn hình phụ cấp thâm niên.
/// </summary>
public partial class PhuCapThamNien : IDisposable
{
    #region Hằng số và cấu hình màn hình

    /// <summary>Văn hóa dùng để hiển thị tiền tệ, ngày tháng và số ngày công theo chuẩn Việt Nam.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Kỳ lương mặc định, được suy ra từ thời điểm hiện tại theo múi giờ Việt Nam.</summary>
    private static readonly (int Month, int Year) DefaultPayrollPeriod = GetDefaultPayrollPeriod();
    /// <summary>Danh sách 12 tháng dùng cho bộ chọn kỳ lương.</summary>
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();
    /// <summary>Số dòng tối đa cho tùy chọn hiển thị toàn bộ dữ liệu trong một lần tải.</summary>
    private const int AllPageSize = 5000;
    /// <summary>Các lựa chọn số dòng cho pager tùy biến.</summary>
    private static readonly IReadOnlyList<PageSizeOption> PageSizeOptions =
    [
        new(20, "20"),
        new(50, "50"),
        new(100, "100"),
        new(AllPageSize, "Tất cả")
    ];

    /// <summary>Các khoảng thâm niên có thể chọn để lọc và thống kê dữ liệu.</summary>
    private static readonly IReadOnlyList<SeniorityRangeOption> SeniorityRanges =
    [
        new(string.Empty, "Tất cả", "TC"),
        new("under-1", "Dưới 1 năm", "<1"),
        new("1-3", "Từ 1 đến dưới 3 năm", "1-<3"),
        new("3-6", "Từ 3 đến dưới 6 năm", "3-<6"),
        new("6-10", "Từ 6 đến dưới 10 năm", "6-<10"),
        new("10-13", "Từ 10 đến dưới 13 năm", "10-<13"),
        new("13-plus", "Từ 13 năm trở lên", "≥13")
    ];

    /// <summary>Tháng bắt đầu được hỗ trợ trong năm dữ liệu đầu tiên.</summary>
    private const int MinimumSupportedMonth = 6;
    /// <summary>Năm kỳ lương sớm nhất mà màn hình cho phép chọn.</summary>
    private const int MinimumSupportedYear = 2026;
    /// <summary>Năm kỳ lương muộn nhất mà màn hình cho phép chọn.</summary>
    private const int MaximumSupportedYear = 2100;
    /// <summary>Khóa định danh phạm vi thao tác khóa/mở khóa các dòng đã chọn.</summary>
    private const string LockScopeSelectedRows = "selected-rows";
    /// <summary>Khóa định danh phạm vi thao tác khóa/mở khóa toàn bộ kỳ lương.</summary>
    private const string LockScopeWholePeriod = "whole-period";
    /// <summary>Nội dung hiển thị mặc định trong lớp phủ đang tải.</summary>
    private const string DefaultLoadingText = "Đang tải dữ liệu phụ cấp thâm niên...";

    #endregion

    #region Phụ thuộc được tiêm

    /// <summary>Phát tín hiệu hủy các tác vụ đang chạy khi component được giải phóng.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Đồng bộ các yêu cầu tải lại để tránh chạy đồng thời nhiều lần.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    /// <summary>Cung cấp dữ liệu và các thao tác phụ cấp thâm niên theo kỳ lương.</summary>
    private IPayrollEmployeeSeniorityAllowanceDataProvider DataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Cung cấp chi tiết bảng công tháng của nhân viên.</summary>
    private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Hiển thị thông báo kết quả thao tác cho người dùng.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    /// <summary>Ghi chi tiết lỗi thao tác để vận hành có thể tra cứu nguyên nhân gốc.</summary>
    private ILogger<PhuCapThamNien> Logger { get; set; } = default!;

    #endregion

    #region Trạng thái màn hình

    #region Dữ liệu và tham chiếu lưới

    /// <summary>Toàn bộ bản ghi của kỳ lương hiện hành sau khi tải từ nguồn dữ liệu.</summary>
    private IReadOnlyList<PhuCapThamNienRecord> AllRecords { get; set; } = [];
    /// <summary>Số dòng của truy vấn server-side hiện hành, không bị giới hạn bởi trang đang xem.</summary>
    private int ServerTotalRecordCount { get; set; }
    /// <summary>Tổng phụ cấp của truy vấn server-side hiện hành.</summary>
    private decimal ServerTotalAllowanceAmount { get; set; }
    /// <summary>Số dòng theo từng khoảng thâm niên do server trả về theo cùng ngữ nghĩa tìm kiếm.</summary>
    private IReadOnlyDictionary<string, int> SeniorityRangeCounts { get; set; } = new Dictionary<string, int>();
    /// <summary>Bản ghi của toàn kỳ dùng riêng cho lưới xuất tệp.</summary>
    private IReadOnlyList<PhuCapThamNienRecord> ExportRecords { get; set; } = [];
    /// <summary>Các đối tượng đang được chọn trong lưới chính.</summary>
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    /// <summary>Tham chiếu đến lưới dữ liệu chính.</summary>
    private PhuCapThamNienGrid? GridSection { get; set; }
    /// <summary>Tham chiếu đến lưới ẩn dùng để xuất toàn bộ dữ liệu.</summary>
    private PhuCapThamNienExportGrid? ExportSource { get; set; }
    /// <summary>Hoàn tất khi lưới xuất đã render và sẵn sàng thực hiện xuất tệp.</summary>
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;

    #endregion

    #region Kỳ lương, bộ lọc và phân trang

    /// <summary>Từ khóa tìm kiếm đang áp dụng cho dữ liệu phụ cấp.</summary>
    private string? SearchText { get; set; }
    /// <summary>Khóa của khoảng thâm niên đang được chọn để lọc dữ liệu.</summary>
    private string SelectedRangeKey { get; set; } = string.Empty;
    /// <summary>Thông báo lỗi của lần tải dữ liệu gần nhất, nếu có.</summary>
    private string? LoadErrorMessage { get; set; }
    /// <summary>Tháng người dùng đang chọn trên thanh công cụ.</summary>
    private int ToolbarMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Năm người dùng đang chọn trên thanh công cụ.</summary>
    private int ToolbarYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Tháng của bộ dữ liệu đã được tải và đang hiển thị.</summary>
    private int AppliedMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Năm của bộ dữ liệu đã được tải và đang hiển thị.</summary>
    private int AppliedYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Số dòng hiển thị trên mỗi trang của lưới.</summary>
    private int pageSize = PageSizeOptions[0].Value;
    /// <summary>Chỉ số trang đang hiển thị, bắt đầu từ không.</summary>
    private int currentPageIndex;
    /// <summary>Cho biết người dùng đã yêu cầu tải dữ liệu ít nhất một lần.</summary>
    private bool HasRequestedData { get; set; }

    #endregion

    #region Trạng thái xử lý và cửa sổ bật lên

    /// <summary>Cho biết hệ thống đang chuẩn bị snapshot dữ liệu cho kỳ lương.</summary>
    private bool IsPreparingPeriod { get; set; }
    /// <summary>Cho biết lưới chính đang tải lại dữ liệu.</summary>
    private bool IsLoading { get; set; }
    /// <summary>Cho biết thao tác làm mới dữ liệu toàn kỳ đang chạy.</summary>
    private bool IsRefreshing { get; set; }
    /// <summary>Cho biết thao tác làm mới một dòng dữ liệu đang chạy.</summary>
    private bool IsRefreshingRow { get; set; }
    /// <summary>Cho biết lưới đang cập nhật sau khi thay đổi kích thước trang.</summary>
    private bool IsChangingPageSize { get; set; }
    /// <summary>Cho biết hệ thống đang chuẩn bị hoặc tạo tệp xuất.</summary>
    private bool IsExporting { get; set; }
    /// <summary>Điều khiển trạng thái hiển thị cửa sổ quy tắc tính phụ cấp.</summary>
    private bool IsRulesPopupVisible { get; set; }
    /// <summary>Điều khiển trạng thái hiển thị cửa sổ bảng công tháng.</summary>
    private bool IsMonthlyWorkPopupVisible { get; set; }
    /// <summary>Cho biết chi tiết bảng công tháng đang được tải.</summary>
    private bool IsMonthlyWorkPopupLoading { get; set; }
    /// <summary>Điều khiển trạng thái hiển thị cửa sổ chỉnh sửa phụ cấp.</summary>
    private bool IsEditPopupVisible { get; set; }
    /// <summary>Điều khiển trạng thái hiển thị hộp xác nhận tính lại.</summary>
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    /// <summary>Điều khiển trạng thái hiển thị hộp chọn phạm vi khóa/mở khóa.</summary>
    private bool IsLockActionPopupVisible { get; set; }
    /// <summary>Cho biết thao tác lưu chỉnh sửa phụ cấp đang chạy.</summary>
    private bool IsSavingEdit { get; set; }
    /// <summary>Trạng thái khóa sẽ được áp dụng trong thao tác đang chờ xác nhận.</summary>
    private bool PendingLockActionState { get; set; } = true;
    /// <summary>Tháng của thao tác khóa/mở khóa đang chờ xác nhận.</summary>
    private int PendingLockActionMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Năm của thao tác khóa/mở khóa đang chờ xác nhận.</summary>
    private int PendingLockActionYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Phạm vi khóa/mở khóa đang được chọn trong hộp xác nhận.</summary>
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    /// <summary>Mô hình dữ liệu được liên kết với biểu mẫu chỉnh sửa.</summary>
    private PhuCapThamNienEditModel EditModel { get; set; } = new();
    /// <summary>Tiêu đề hiển thị của cửa sổ chỉnh sửa.</summary>
    private string EditPopupTitle { get; set; } = "Sửa phụ cấp thâm niên";
    /// <summary>Lỗi nghiệp vụ hoặc concurrency hiển thị ngay trong biểu mẫu điều chỉnh.</summary>
    private string? EditErrorMessage { get; set; }
    /// <summary>Thông báo lỗi khi tải chi tiết bảng công tháng.</summary>
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    /// <summary>Tiêu đề hiển thị của cửa sổ bảng công tháng.</summary>
    private string MonthlyWorkPopupTitle { get; set; } = "Bảng công tháng";
    /// <summary>Ngữ cảnh nhân viên và kỳ lương hiển thị trong cửa sổ bảng công.</summary>
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    /// <summary>Các dòng ngày công hiển thị trong cửa sổ bảng công.</summary>
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    /// <summary>Bản ghi phụ cấp đang được đối chiếu bảng công.</summary>
    private PhuCapThamNienRecord? MonthlyWorkPopupRecord { get; set; }
    /// <summary>Số ngày công tính lương dùng để đối chiếu trong cửa sổ bảng công.</summary>
    private decimal MonthlyWorkPopupSalaryWorkDays { get; set; }
    /// <summary>Nội dung hiện tại của lớp phủ tiến trình.</summary>
    private string LoadingText { get; set; } = DefaultLoadingText;
    /// <summary>Phiên bản mới nhất của yêu cầu tải lại được gửi đến hàng đợi.</summary>
    private int reloadRequestedVersion;
    /// <summary>Phiên bản yêu cầu tải lại gần nhất đã được xử lý.</summary>
    private int reloadProcessedVersion;
    /// <summary>Token của lần tải pager hiện tại.</summary>
    private CancellationTokenSource? activeReloadTokenSource;

    #endregion

    #endregion

    #region Trạng thái suy diễn và quyền thao tác

    private int PageSize => pageSize;
    private IReadOnlyList<PageSizeOption> AvailablePageSizeOptions => TotalRecordCount > AllPageSize
        ? PageSizeOptions.Where(option => option.Value != AllPageSize).ToArray()
        : PageSizeOptions;
    private bool IsShowingAllRows => PageSize == AllPageSize;
    private string PageSizeDescription => IsShowingAllRows ? "tất cả dòng" : "dòng/trang";
    private int CurrentPageIndex => currentPageIndex;
    private int TotalRecordCount => ServerTotalRecordCount;
    private int TotalPageCount => TotalRecordCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
    private int CurrentPageStartRecord => TotalRecordCount == 0
        ? 0
        : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalRecordCount == 0
        ? 0
        : Math.Min(TotalRecordCount, CurrentPageIndex * PageSize + AllRecords.Count);
    private bool CanBrowsePages => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    private string PagerSummaryText => !HasRequestedData || HasLoadError || TotalRecordCount == 0
        ? "Chưa có trang dữ liệu"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";

    /// <summary>Các badge khoảng thâm niên kèm số lượng bản ghi của từng khoảng.</summary>
    private IReadOnlyList<PhuCapThamNienRangeBadge> SeniorityRangeBadges =>
        SeniorityRanges
            .Select(range => new PhuCapThamNienRangeBadge(
                range.Key,
                range.Label,
                range.ShortLabel,
                SeniorityRangeCounts.GetValueOrDefault(range.Key)))
            .ToArray();

    /// <summary>Các tháng hợp lệ cho năm đang chọn, có xét giới hạn của năm đầu tiên.</summary>
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;

    /// <summary>Cho biết lần tải dữ liệu gần nhất có lỗi cần hiển thị.</summary>
    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    /// <summary>Cho biết kỳ trên thanh công cụ khác với kỳ của dữ liệu đang hiển thị.</summary>
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);

    /// <summary>Cho biết có thao tác bất đồng bộ cần khóa giao diện và hiển thị lớp phủ tiến trình.</summary>
    private bool ShowLoadingPanel =>
        IsPreparingPeriod
        || IsLoading
        || IsRefreshing
        || IsRefreshingRow
        || IsChangingPageSize
        || IsExporting
        || IsSavingEdit;

    /// <summary>Cho biết người dùng có thể tương tác với dữ liệu hiện tại.</summary>
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    /// <summary>Cho biết có thể gửi yêu cầu xem dữ liệu mà không bị trạng thái tải chặn.</summary>
    private bool CanView => !ShowLoadingPanel;
    /// <summary>Cho biết bộ dữ liệu hiện tại hợp lệ để thực hiện thao tác nghiệp vụ.</summary>
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    /// <summary>Cho biết có thể mở thao tác tính lại phụ cấp cho kỳ hiện tại.</summary>
    private bool CanRecalculate => CanOperateOnCurrentDataset;
    /// <summary>Số bản ghi đang chọn còn tồn tại trong tập dữ liệu đang hiển thị.</summary>
    private int SelectedRecordCount => GetSelectedRecords().Count;
    /// <summary>Cho biết có thể mở thao tác khóa dữ liệu.</summary>
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    /// <summary>Cho biết có thể mở thao tác mở khóa dữ liệu.</summary>
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    /// <summary>Cho biết có ít nhất một dòng để áp dụng phạm vi các dòng đã chọn.</summary>
    private bool CanChooseSelectedRowsScope => SelectedRecordCount > 0;
    /// <summary>Cho biết thao tác khóa/mở khóa đang đủ điều kiện để xác nhận.</summary>
    private bool CanConfirmLockAction =>
        CanOperateOnCurrentDataset
        && (string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal) || CanChooseSelectedRowsScope);
    /// <summary>Cho biết có thể thay đổi bộ lọc khi không có tác vụ đang chạy.</summary>
    private bool CanChangeFilters => !ShowLoadingPanel;
    /// <summary>Cho biết có thể xuất toàn bộ dữ liệu của kỳ đang áp dụng.</summary>
    private bool CanExport => CanOperateOnCurrentDataset;
    /// <summary>Cho biết biểu mẫu chỉnh sửa đủ điều kiện để lưu.</summary>
    private bool CanSaveEdit =>
        !IsSavingEdit
        && !HasPendingPeriodChange
        && EditModel.PayrollAllowanceSummaryRecordId != Guid.Empty
        && !EditModel.IsLocked;

    /// <summary>Nhãn kỳ lương người dùng đang chọn trên thanh công cụ.</summary>
    private string CurrentPeriodLabel => $"{ToolbarMonth:00}/{ToolbarYear}";
    /// <summary>Nhãn kỳ lương của dữ liệu đã được tải.</summary>
    private string AppliedPeriodLabel => $"{AppliedMonth:00}/{AppliedYear}";
    /// <summary>Nhãn kỳ lương của thao tác khóa/mở khóa đang chờ xác nhận.</summary>
    private string PendingLockActionPeriodLabel => $"{PendingLockActionMonth:00}/{PendingLockActionYear}";
    /// <summary>Tiêu đề của hộp xác nhận khóa hoặc mở khóa.</summary>
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa dữ liệu phụ cấp thâm niên"
        : "Mở khóa dữ liệu phụ cấp thâm niên";
    /// <summary>Nhãn của nút xác nhận thao tác khóa hoặc mở khóa.</summary>
    private string LockActionConfirmText => PendingLockActionState ? "Khóa" : "Mở khóa";
    /// <summary>Lời nhắc chọn phạm vi thực hiện thao tác khóa hoặc mở khóa.</summary>
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu phụ cấp thâm niên."
        : "Chọn phạm vi cần mở khóa dữ liệu phụ cấp thâm niên.";
    /// <summary>Giải thích ảnh hưởng của lựa chọn phạm vi toàn kỳ trong hộp xác nhận.</summary>
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Lựa chọn toàn kỳ sẽ bỏ qua bộ lọc tìm kiếm và nhóm thâm niên đang hiển thị. Cả Khóa và Mở khóa đều bỏ qua các dòng có bản ghi Tổng hợp phụ cấp đã khóa.";
    /// <summary>Mô tả số dòng bị ảnh hưởng khi chọn phạm vi các dòng đang chọn.</summary>
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRecordCount:N0} dòng đang được chọn trong lưới."
        : "Chưa có dòng nào được chọn trong lưới hiện tại.";
    /// <summary>Mô tả phạm vi ảnh hưởng khi áp dụng thao tác cho toàn bộ kỳ.</summary>
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Áp dụng cho toàn bộ dữ liệu phụ cấp thâm niên của kỳ {PendingLockActionPeriodLabel}."
        : $"Mở khóa toàn bộ dữ liệu phụ cấp thâm niên của kỳ {PendingLockActionPeriodLabel}.";

    /// <summary>Tiêu đề trạng thái rỗng phù hợp với dữ liệu, bộ lọc và kỳ lương hiện tại.</summary>
    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu phụ cấp thâm niên"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
            : !string.IsNullOrWhiteSpace(SearchText) || !string.IsNullOrWhiteSpace(SelectedRangeKey)
                ? "Không tìm thấy dòng phụ cấp thâm niên phù hợp"
                : "Chưa có dữ liệu phụ cấp thâm niên";

    /// <summary>Nội dung hướng dẫn tương ứng với trạng thái rỗng của lưới.</summary>
    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để tải dữ liệu khi bạn sẵn sàng."
        : HasPendingPeriodChange
            ? $"Bạn đã đổi kỳ lương sang {CurrentPeriodLabel}. Nhấn Xem để tải dữ liệu của kỳ này."
            : !string.IsNullOrWhiteSpace(SearchText) || !string.IsNullOrWhiteSpace(SelectedRangeKey)
                ? "Hãy thử từ khóa khác hoặc đổi nhóm thâm niên để xem thêm dữ liệu."
                : $"Dữ liệu phụ cấp thâm niên của kỳ {AppliedPeriodLabel} sẽ hiển thị tại đây sau khi hệ thống tạo snapshot cho kỳ lương này.";

    /// <summary>Nhãn hành động trong trạng thái rỗng, dùng để tải mới hoặc tải lại.</summary>
    private string EmptyStateActionText => !HasRequestedData || HasPendingPeriodChange
        ? "Xem dữ liệu"
        : "Tải lại";

    #endregion

    #region Điểm vào của giao diện

    /// <summary>
    /// Hoàn tất đồng bộ hậu render cho lưới xuất dữ liệu ẩn.
    /// </summary>
    /// <param name="firstRender">Cho biết đây có phải lần render đầu tiên hay không.</param>
    /// <returns>Tác vụ render cơ sở hoặc cập nhật lại giao diện khi cần đồng bộ summary.</returns>
    private Task OnExportGridRendered()
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    #endregion

    #region Chuẩn bị và tải dữ liệu

    /// <summary>Chuẩn hóa kỳ được chọn, tạo dữ liệu kỳ lương và tải lưới chính.</summary>
    private async Task OnViewRequestedAsync()
    {
        if(!CanView)
        {
            return;
        }

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        LoadErrorMessage = null;
        currentPageIndex = 0;

        try
        {
            IsPreparingPeriod = true;
            SetLoadingText($"Đang chuẩn bị dữ liệu phụ cấp thâm niên kỳ {CurrentPeriodLabel}...");
            await ClearSelectionAsync();
            await DataProvider.PreparePeriodAsync(ToolbarYear, ToolbarMonth, disposalTokenSource.Token);

            AppliedMonth = ToolbarMonth;
            AppliedYear = ToolbarYear;
            HasRequestedData = true;

            await ReloadAsync();
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            const string errorMessage = "Không thể chuẩn bị dữ liệu phụ cấp thâm niên. Vui lòng thử lại.";
            LoadErrorMessage = errorMessage;
            ToastService.ShowError(errorMessage);
        }
        finally
        {
            IsPreparingPeriod = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    /// <summary>Tải mới dữ liệu hoặc chuẩn bị lại kỳ lương tùy theo trạng thái hiện tại.</summary>
    private Task OnRetryAsync()
    {
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            return OnViewRequestedAsync();
        }

        return ReloadAsync();
    }

    /// <summary>Xếp yêu cầu tải lại vào hàng đợi, chỉ xử lý phiên bản yêu cầu mới nhất.</summary>
    private async Task ReloadAsync()
    {
        if(!HasRequestedData || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        CancelActiveReload();
        if(!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        IsLoading = true;
        LoadErrorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            while(!disposalTokenSource.IsCancellationRequested
                  && !HasPendingPeriodChange
                  && reloadProcessedVersion < Volatile.Read(ref reloadRequestedVersion))
            {
                var requestVersion = Volatile.Read(ref reloadRequestedVersion);
                reloadProcessedVersion = requestVersion;
                await ReloadCoreAsync(requestVersion, CreateReloadSnapshot());
            }
        }
        finally
        {
            IsLoading = false;
            SetLoadingText(DefaultLoadingText);
            reloadGate.Release();

            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>Tải dữ liệu phụ cấp theo bộ lọc hiện tại và đồng bộ lại lựa chọn/tổng tiền.</summary>
    private async Task ReloadCoreAsync(int requestVersion, SeniorityAllowanceReloadSnapshot snapshot)
    {
        using var requestTokenSource = BeginReload();
        var cancellationToken = requestTokenSource.Token;
        LoadErrorMessage = null;
        SetLoadingText(DefaultLoadingText);

        try
        {
            await ClearSelectionAsync();
            var filter = BuildFilter(snapshot);
            // Interactive Server resolves both capabilities to one scoped service and DbContext.
            // EF Core does not allow simultaneous operations on the same DbContext, so these
            // queries must remain sequential even though the WebAssembly implementation uses HTTP.
            var page = await DataProvider.SearchPageAsync(filter, cancellationToken);
            var rangeSummaries = await DataProvider.LoadRangeSummariesAsync(filter, cancellationToken);

            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            if(page.TotalCount == 0 && snapshot.PageIndex != 0)
            {
                currentPageIndex = 0;
                Interlocked.Increment(ref reloadRequestedVersion);
                return;
            }

            if(page.TotalCount > 0)
            {
                if(snapshot.PageSize == AllPageSize && page.TotalCount > AllPageSize)
                {
                    pageSize = PageSizeOptions[0].Value;
                    currentPageIndex = 0;
                    ToastService.ShowWarning($"Kỳ lương có hơn {AllPageSize:N0} dòng nên màn hình chuyển về 20 dòng/trang.");
                    Interlocked.Increment(ref reloadRequestedVersion);
                    return;
                }

                var maximumPageIndex = Math.Max(0, (int)Math.Ceiling(page.TotalCount / (double)PageSize) - 1);
                if(snapshot.PageIndex > maximumPageIndex)
                {
                    currentPageIndex = maximumPageIndex;
                    Interlocked.Increment(ref reloadRequestedVersion);
                    return;
                }
            }

            AllRecords = page.Rows;
            ServerTotalRecordCount = page.TotalCount;
            ServerTotalAllowanceAmount = page.TotalAllowanceAmount;
            SeniorityRangeCounts = rangeSummaries
                .ToDictionary(summary => summary.RangeKey, summary => summary.Count, StringComparer.Ordinal);
            await PruneSelectionToVisibleRecordsAsync();
        }
        catch(OperationCanceledException) when(
            disposalTokenSource.IsCancellationRequested || ShouldDiscardReloadResult(requestVersion, snapshot))
        {
            // Đổi bộ lọc, đổi trang hoặc hủy component: kết quả cũ không được phép commit.
        }
        catch(Exception)
        {
            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            AllRecords = [];
            ServerTotalRecordCount = 0;
            ServerTotalAllowanceAmount = 0m;
            SeniorityRangeCounts = new Dictionary<string, int>();
            const string errorMessage = "Không thể tải dữ liệu phụ cấp thâm niên. Vui lòng thử lại.";
            LoadErrorMessage = errorMessage;
            ToastService.ShowError(errorMessage);
        }
        finally
        {
            if(ReferenceEquals(activeReloadTokenSource, requestTokenSource))
            {
                activeReloadTokenSource = null;
            }
        }
    }

    #endregion

    #region Thao tác trên thanh công cụ và màn hình

    #region Bộ lọc và điều hướng

    /// <summary>Chuẩn hóa và cập nhật tháng được chọn trên thanh công cụ.</summary>
    private Task OnSelectedMonthChangedAsync(int month)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(month, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    /// <summary>Chuẩn hóa và cập nhật năm được chọn trên thanh công cụ.</summary>
    private Task OnSelectedYearChangedAsync(int year)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, year);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    /// <summary>Chuẩn hóa từ khóa tìm kiếm và tải lại dữ liệu nếu kỳ hiện tại đã được áp dụng.</summary>
    private Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        currentPageIndex = 0;
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            return Task.CompletedTask;
        }

        return ReloadAsync();
    }

    /// <summary>Cập nhật số dòng mỗi trang, giữ dòng đầu tiên đang xem và render lại lưới với lớp phủ tiến trình.</summary>
    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Any(option => option.Value == value)
            ? value
            : PageSizeOptions[0].Value;
        if(PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        SetLoadingText("Đang cập nhật số dòng hiển thị...");

        try
        {
            var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
            pageSize = normalizedValue;
            currentPageIndex = firstVisibleRecordIndex / PageSize;
            await ClearSelectionAsync();
            await ReloadAsync();
        }
        finally
        {
            IsChangingPageSize = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    /// <summary>Chuyển trang của pager tùy biến.</summary>
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
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    /// <summary>Thực hiện hành động trong trạng thái rỗng: xem dữ liệu hoặc tải lại.</summary>
    private async Task OnEmptyStateActionClick()
    {
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        await ReloadAsync();
    }

    /// <summary>Chọn khoảng thâm niên, tính lại tổng hiển thị và loại bỏ các lựa chọn không còn thấy.</summary>
    private async Task SelectRangeAsync(string rangeKey)
    {
        if(string.Equals(SelectedRangeKey, rangeKey, StringComparison.Ordinal))
        {
            return;
        }

        SelectedRangeKey = rangeKey;
        currentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    /// <summary>Mở cửa sổ giải thích quy tắc tính phụ cấp thâm niên.</summary>
    private void OpenRulesPopup()
    {
        IsRulesPopupVisible = true;
    }

    #endregion

    /// <summary>Tính lại và đồng bộ riêng một dòng phụ cấp chưa bị khóa.</summary>
    private async Task RefreshRowAsync(PhuCapThamNienRecord record)
    {
        if(!CanRefreshRow(record))
        {
            return;
        }

        try
        {
            IsRefreshingRow = true;
            SetLoadingText($"Đang làm mới phụ cấp thâm niên của {record.EmployeeDisplay}...");

            var result = await DataProvider.RefreshAsync(
                new RefreshPayrollEmployeeSeniorityAllowanceRequest(
                    record.PayrollYear,
                    record.PayrollMonth,
                    record.PayrollAllowanceSummaryRecordId),
                disposalTokenSource.Token);

            await ReloadAsync();
            ToastService.ShowSuccess(
                $"Đã làm mới {result.UpdatedCount:N0} dòng phụ cấp thâm niên, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(HrmApiException exception) when(exception.Kind == HrmApiErrorKind.Conflict)
        {
            ToastService.ShowWarning("Trạng thái khóa đã được thay đổi bởi thao tác khác. Vui lòng tải lại dữ liệu.");
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể làm mới phụ cấp thâm niên của {record.EmployeeDisplay}.");
        }
        finally
        {
            IsRefreshingRow = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    /// <summary>Đảo trạng thái khóa của một dòng phụ cấp và cập nhật lưới.</summary>
    private async Task ToggleLockStateAsync(PhuCapThamNienRecord record)
    {
        if(!CanToggleLock(record))
        {
            return;
        }

        try
        {
            var updatedRecord = await DataProvider.SetLockStateAsync(
                record.PayrollAllowanceSummaryRecordId,
                !record.IsLocked,
                record.UpdatedAtUtc,
                disposalTokenSource.Token);

            ApplyUpdatedRecord(updatedRecord);

            ToastService.ShowSuccess(
                updatedRecord.IsLocked
                    ? $"Đã khóa dòng phụ cấp thâm niên của {updatedRecord.EmployeeDisplay}."
                    : $"Đã mở khóa dòng phụ cấp thâm niên của {updatedRecord.EmployeeDisplay}.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(HrmApiException exception) when(exception.Kind == HrmApiErrorKind.Conflict)
        {
            ToastService.ShowWarning("Trạng thái khóa đã được thay đổi bởi thao tác khác. Vui lòng tải lại dữ liệu.");
        }
        catch(HrmApiException exception)
        {
            ToastService.ShowWarning($"Không thực hiện được thao tác với dòng {record.EmployeeDisplay}: {exception.UserMessage}");
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể cập nhật trạng thái khóa của {record.EmployeeDisplay}.");
        }
    }

    /// <summary>Thực hiện khóa/mở khóa theo phạm vi đã chọn, rồi tải lại dữ liệu.</summary>
    private async Task ConfirmLockActionAsync()
    {
        var shouldLock = PendingLockActionState;
        var actionScope = SelectedLockActionScope;

        if(!CanOperateOnCurrentDataset)
        {
            return;
        }

        Guid[]? targetRecordIds = null;
        var targetRowCount = 0;
        if(!IsWholePeriodLockActionScope(actionScope))
        {
            var selectedRecords = GetSelectedRecords()
                .DistinctBy(record => record.PayrollAllowanceSummaryRecordId)
                .ToArray();
            if(selectedRecords.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }

            targetRecordIds = selectedRecords
                .Select(record => record.PayrollAllowanceSummaryRecordId)
                .ToArray();
            targetRowCount = targetRecordIds.Length;

            if(shouldLock
               && IsEditPopupVisible
               && selectedRecords.Any(record => record.PayrollAllowanceSummaryRecordId == EditModel.PayrollAllowanceSummaryRecordId))
            {
                CloseEditPopupCore();
            }
        }
        else if(shouldLock && IsEditPopupVisible)
        {
            CloseEditPopupCore();
        }

        try
        {
            IsRefreshing = true;
            IsLockActionPopupVisible = false;
            SetLoadingText(BuildLockActionPendingLoadingMessage(shouldLock, actionScope, targetRowCount > 0 ? targetRowCount : null));
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.SetLockStateBatchAsync(
                new SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest(
                    PendingLockActionYear,
                    PendingLockActionMonth,
                    shouldLock,
                    targetRecordIds),
                disposalTokenSource.Token);

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionNoDataMessage(shouldLock, actionScope));
                return;
            }

            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionAlreadyAppliedMessage(
                    shouldLock,
                    actionScope,
                    result.TargetRowCount,
                    result.UnchangedCount,
                    result.SkippedSummaryLockedCount,
                    result.SkippedRows));
                return;
            }

            await ReloadAsync();
            ToastService.ShowSuccess(BuildLockActionSuccessMessage(
                shouldLock,
                actionScope,
                result.TargetRowCount,
                result.UpdatedCount,
                result.UnchangedCount,
                result.SkippedSummaryLockedCount,
                result.SkippedRows));
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(HrmApiException exception)
        {
            ToastService.ShowError($"Không thực hiện được {LockActionConfirmText.ToLowerInvariant()} dữ liệu phụ cấp thâm niên: {exception.UserMessage}");
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể {LockActionConfirmText.ToLowerInvariant()} dữ liệu phụ cấp thâm niên của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            IsRefreshing = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    #endregion

    #region Hỗ trợ chọn dòng và lọc dữ liệu

    /// <summary>Xóa lựa chọn và dòng đang focus trên lưới chính.</summary>
    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if(GridSection is null)
        {
            return;
        }

        await GridSection.ClearSelectionAsync();
    }

    /// <summary>Loại bỏ các dòng đã chọn nhưng không còn nằm trong tập đang hiển thị.</summary>
    private async Task PruneSelectionToVisibleRecordsAsync()
    {
        if(SelectedDataItems.Count == 0)
        {
            return;
        }

        var visibleIds = AllRecords
            .Select(record => record.Id)
            .ToHashSet();
        var visibleSelection = SelectedDataItems
            .OfType<PhuCapThamNienRecord>()
            .Where(record => visibleIds.Contains(record.Id))
            .DistinctBy(record => record.Id)
            .Cast<object>()
            .ToArray();

        if(visibleSelection.Length == SelectedDataItems.Count)
        {
            return;
        }

        SelectedDataItems = visibleSelection;
        if(visibleSelection.Length == 0)
        {
            if(GridSection is not null)
            {
                await GridSection.ClearSelectionAsync();
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Lấy các bản ghi hợp lệ đang chọn theo thứ tự của danh sách hiển thị.</summary>
    private List<PhuCapThamNienRecord> GetSelectedRecords()
    {
        var selectedIds = SelectedDataItems
            .OfType<PhuCapThamNienRecord>()
            .Select(record => record.Id)
            .ToHashSet();

        return AllRecords
            .Where(record => selectedIds.Contains(record.Id))
            .DistinctBy(record => record.Id)
            .ToList();
    }

    /// <summary>Tạo điều kiện tìm kiếm cho kỳ đã tải hoặc kỳ đang chọn trước khi tải lần đầu.</summary>
    private SeniorityAllowanceReloadSnapshot CreateReloadSnapshot() =>
        new(
            HasRequestedData ? AppliedMonth : ToolbarMonth,
            HasRequestedData ? AppliedYear : ToolbarYear,
            NormalizeOptional(SearchText),
            SelectedRangeKey,
            CurrentPageIndex,
            PageSize);

    private bool ShouldDiscardReloadResult(
        int requestVersion,
        SeniorityAllowanceReloadSnapshot snapshot) =>
        requestVersion != Volatile.Read(ref reloadRequestedVersion)
        || !HasRequestedData
        || HasPendingPeriodChange
        || snapshot != CreateReloadSnapshot();

    private CancellationTokenSource BeginReload()
    {
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        activeReloadTokenSource = tokenSource;
        return tokenSource;
    }

    private void CancelActiveReload() => activeReloadTokenSource?.Cancel();

    private PayrollEmployeeSeniorityAllowanceFilter BuildFilter(SeniorityAllowanceReloadSnapshot snapshot) =>
        new(
            snapshot.PayrollMonth,
            snapshot.PayrollYear,
            null,
            snapshot.SearchText,
            null,
            snapshot.PageSize,
            snapshot.PageIndex * snapshot.PageSize,
            snapshot.SeniorityRangeKey);

    /// <summary>Kiểm tra dòng có được phép chỉnh sửa thủ công hay không.</summary>
    private bool CanEditRow(PhuCapThamNienRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked;

    /// <summary>Kiểm tra dòng có được phép tính lại riêng lẻ hay không.</summary>
    private bool CanRefreshRow(PhuCapThamNienRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked;

    /// <summary>Kiểm tra dòng có được phép đổi trạng thái khóa hay không.</summary>
    private bool CanToggleLock(PhuCapThamNienRecord record) =>
        CanOperateOnCurrentDataset && !record.IsSummaryLocked;

    /// <summary>Kiểm tra có thể mở bảng công tháng của nhân viên trong dòng hay không.</summary>
    private bool CanViewMonthlyWork(PhuCapThamNienRecord record) =>
        CanOperateOnCurrentDataset && record.EmployeeId != Guid.Empty;

    /// <summary>Kiểm tra phạm vi thao tác có áp dụng cho toàn bộ kỳ lương hay không.</summary>
    private static bool IsWholePeriodLockActionScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    /// <summary>Tạo tên tệp xuất ổn định theo kỳ lương đang áp dụng.</summary>
    private string BuildExportFileName() =>
        $"phu-cap-tham-nien-{AppliedYear}-{AppliedMonth:00}";

    /// <summary>Tạo thông báo khi không có bản ghi phù hợp để khóa hoặc mở khóa.</summary>
    private string BuildLockActionNoDataMessage(bool shouldLock, string scope)
    {
        if(IsWholePeriodLockActionScope(scope))
        {
            return $"Không có dữ liệu phụ cấp thâm niên của kỳ {PendingLockActionPeriodLabel} để {(shouldLock ? "khóa" : "mở khóa")}.";
        }

        return "Không còn dòng phụ cấp thâm niên hợp lệ trong phạm vi đang chọn để xử lý.";
    }

    /// <summary>Tạo thông báo khi các dòng mục tiêu đã ở đúng trạng thái khóa/mở khóa.</summary>
    private string BuildLockActionAlreadyAppliedMessage(
        bool shouldLock,
        string scope,
        int targetRowCount,
        int unchangedCount,
        int skippedSummaryLockedCount,
        IReadOnlyList<PayrollEmployeeSeniorityAllowanceLockStateSkippedRow>? skippedRows)
    {
        if(skippedSummaryLockedCount > 0)
        {
            return $"Không có dòng nào được {(shouldLock ? "khóa" : "mở khóa")}. "
                + $"{unchangedCount:N0} dòng đã đúng trạng thái và {skippedSummaryLockedCount:N0} dòng không thực hiện được vì Phụ cấp tổng hợp đã khóa: {FormatSkippedLockRows(skippedRows)}.";
        }

        var stateText = shouldLock ? "khóa" : "mở";
        if(IsWholePeriodLockActionScope(scope))
        {
            return $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng của kỳ {PendingLockActionPeriodLabel} đã ở trạng thái {stateText}.";
        }

        return $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng đã chọn đã ở trạng thái {stateText}.";
    }

    /// <summary>Tạo nội dung tiến trình cho thao tác khóa/mở khóa đang thực hiện.</summary>
    private string BuildLockActionPendingLoadingMessage(bool shouldLock, string scope, int? affectedCount = null)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        if(!IsWholePeriodLockActionScope(scope) && affectedCount.HasValue)
        {
            return $"Đang xử lý {actionText} {affectedCount.Value:N0} dòng phụ cấp thâm niên đã chọn...";
        }

        return IsWholePeriodLockActionScope(scope)
            ? $"Đang xử lý {actionText} dữ liệu phụ cấp thâm niên của kỳ {PendingLockActionPeriodLabel}..."
            : $"Đang xử lý {actionText} các dòng phụ cấp thâm niên đã chọn...";
    }

    /// <summary>Tạo thông báo thành công, bao gồm số dòng thay đổi và số dòng giữ nguyên.</summary>
    private string BuildLockActionSuccessMessage(
        bool shouldLock,
        string scope,
        int targetRowCount,
        int updatedCount,
        int unchangedCount,
        int skippedSummaryLockedCount,
        IReadOnlyList<PayrollEmployeeSeniorityAllowanceLockStateSkippedRow>? skippedRows)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var details = new List<string>();
        if(unchangedCount > 0)
        {
            details.Add($"giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái");
        }

        if(skippedSummaryLockedCount > 0)
        {
            details.Add($"không thực hiện được {skippedSummaryLockedCount:N0} dòng do Phụ cấp tổng hợp đã khóa: {FormatSkippedLockRows(skippedRows)}");
        }

        var detailText = details.Count == 0 ? string.Empty : $", {string.Join(", ", details)}";

        return IsWholePeriodLockActionScope(scope)
            ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} dòng phụ cấp thâm niên của kỳ {PendingLockActionPeriodLabel}{detailText}."
            : $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} dòng đã chọn{detailText}.";
    }

    private static string FormatSkippedLockRows(
        IReadOnlyList<PayrollEmployeeSeniorityAllowanceLockStateSkippedRow>? skippedRows)
    {
        if(skippedRows is null || skippedRows.Count == 0)
        {
            return "không xác định được dòng";
        }

        const int maxRowsInToast = 10;
        var labels = skippedRows.Take(maxRowsInToast)
            .Select(row => string.IsNullOrWhiteSpace(row.EmployeeCode)
                ? (string.IsNullOrWhiteSpace(row.EmployeeName) ? row.PayrollAllowanceSummaryRecordId.ToString("D") : row.EmployeeName!.Trim())
                : $"{row.EmployeeCode.Trim()} - {row.EmployeeName?.Trim() ?? "không rõ tên"}")
            .ToList();
        if(skippedRows.Count > maxRowsInToast)
        {
            labels.Add($"và {skippedRows.Count - maxRowsInToast:N0} dòng khác");
        }

        return string.Join(", ", labels);
    }

    /// <summary>Thay thế bản ghi đã cập nhật trong bộ nhớ và đóng biểu mẫu nếu bản ghi vừa bị khóa.</summary>
    private void ApplyUpdatedRecord(PhuCapThamNienRecord updatedRecord)
    {
        AllRecords = AllRecords
            .Select(item => item.PayrollAllowanceSummaryRecordId == updatedRecord.PayrollAllowanceSummaryRecordId
                ? updatedRecord
                : item)
            .ToArray();

        if(updatedRecord.IsLocked
           && IsEditPopupVisible
           && EditModel.PayrollAllowanceSummaryRecordId == updatedRecord.PayrollAllowanceSummaryRecordId)
        {
            CloseEditPopupCore();
        }
    }

    #endregion

    #region Hỗ trợ hiển thị

    /// <summary>Định dạng giá trị tiền tệ theo chuẩn Việt Nam, không có phần thập phân.</summary>
    private string FormatCurrency(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

    /// <summary>Định dạng ngày tùy chọn theo mẫu ngày/tháng/năm hoặc trả về giá trị thay thế.</summary>
    private static string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("dd/MM/yyyy", DisplayCulture) : "--";

    /// <summary>Định dạng Công HC không có phần thập phân; giá trị rỗng hoặc bằng 0 không hiển thị.</summary>
    private static string FormatAdministrativeWorkDays(decimal? value) =>
        value.HasValue && value.Value != 0m ? value.Value.ToString("0", DisplayCulture) : string.Empty;

    /// <summary>Định dạng số ngày công với bốn chữ số thập phân; giá trị rỗng hoặc bằng 0 không hiển thị.</summary>
    private static string FormatWorkDays(decimal? value) =>
        value.HasValue && value.Value != 0m ? value.Value.ToString("0.0000", DisplayCulture) : string.Empty;

    /// <summary>Chuẩn hóa chuỗi dùng để hiển thị, thay giá trị trống bằng dấu gạch ngang.</summary>
    private static string GetDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();

    /// <summary>Mã hóa HTML và đánh dấu các đoạn khớp với từ khóa tìm kiếm hiện tại.</summary>
    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = GetDisplayValue(value);
        if(string.IsNullOrWhiteSpace(SearchText))
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        if(searchText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while(true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if(matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"seniority-allowance-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if(builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    /// <summary>Tạo lớp CSS biểu diễn trạng thái khóa của bản ghi.</summary>
    private static string GetLockStatusCssClass(bool isLocked) => string.Join(
        ' ',
        "yes-no-status",
        isLocked ? "yes-no-status-no" : "yes-no-status-yes");

    /// <summary>Tạo lớp CSS của nhãn quy tắc tính phụ cấp.</summary>
    private static string GetRuleStatusCssClass(string? ruleKey) => string.Join(
        ' ',
        "kind-status",
        ResolveRuleStatusCssClass(ruleKey));

    /// <summary>Ánh xạ khóa quy tắc sang lớp CSS màu sắc tương ứng.</summary>
    private static string ResolveRuleStatusCssClass(string? ruleKey) => ruleKey switch
    {
        "temporary-position" => "kind-status-absence",
        "blocked-salary-work" => "kind-status-absence",
        "13-plus" => "kind-status-workday",
        "10-13" => "kind-status-business-trip",
        "6-10" => "kind-status-business-trip",
        "3-6" => "kind-status-leave",
        "1-3" => "kind-status-leave",
        _ => "kind-status-neutral"
    };

    /// <summary>Tạo lớp CSS cho badge của khoảng thâm niên.</summary>
    private static string GetRangeBadgeCssClass(string rangeKey) =>
        string.IsNullOrEmpty(rangeKey)
            ? "seniority-allowance-summary-button seniority-allowance-summary-button-all"
            : $"seniority-allowance-summary-button seniority-allowance-summary-button-{rangeKey}";

    /// <summary>Cập nhật nội dung hiển thị trong lớp phủ tiến trình.</summary>
    private void SetLoadingText(string value)
    {
        LoadingText = value;
    }

    /// <summary>Cắt khoảng trắng của chuỗi tùy chọn và chuyển chuỗi rỗng thành <see langword="null"/>.</summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Giới hạn tháng/năm vào miền kỳ lương được màn hình hỗ trợ.</summary>
    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        if(normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            return (MinimumSupportedMonth, MinimumSupportedYear);
        }

        return (normalizedMonth, normalizedYear);
    }

    /// <summary>Lấy kỳ lương mặc định từ thời điểm UTC sau khi quy đổi sang múi giờ Việt Nam.</summary>
    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }

    #endregion

    #region Giải phóng tài nguyên và kiểu nội bộ

    /// <summary>Hủy các tác vụ đang chạy và giải phóng tài nguyên đồng bộ của component.</summary>
    public void Dispose()
    {
        CancelActiveReload();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    /// <summary>Đại diện một lựa chọn tháng gồm giá trị số và nhãn hiển thị.</summary>
    public sealed record MonthOption(int Value, string Text);

    /// <summary>Đại diện cấu hình một khoảng thâm niên dùng để lọc dữ liệu.</summary>
    private sealed record SeniorityRangeOption(string Key, string Label, string ShortLabel);

    #endregion
}
