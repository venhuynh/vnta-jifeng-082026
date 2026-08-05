using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Export;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Sections;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.State;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiemGanNhanVien</c> phục vụ màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
public partial class PhuCapTrachNhiemGanNhanVien : IDisposable
{
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private static readonly IReadOnlyList<EmployeeAssignmentMonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new EmployeeAssignmentMonthOption(month, $"Tháng {month:00}"))
            .ToArray();

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private const int MinimumSupportedMonth = 6;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private const int MinimumSupportedYear = 2026;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private const int MaximumSupportedYear = 2100;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private const string GradePresenceAssignedKey = "assigned";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private const string GradePresenceUnassignedKey = "unassigned";
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Các kích thước trang được hỗ trợ cho truy vấn máy chủ.</summary>
    private static readonly IReadOnlyList<int> PageSizeOptions = [50, 100, 200];

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private readonly EmployeeAssignmentReloadLifecycleState ReloadLifecycleState = new();
    private readonly EmployeeAssignmentSelectionState SelectionState = new();
    private IEmployeeAssignmentFilterFactory FilterFactory { get; } = new EmployeeAssignmentFilterFactory();

    [Inject]
    /// <summary>Truy vấn và thay đổi gán cấp bậc theo nhân viên.</summary>
    private PhuCapTrachNhiemGanNhanVienDataProvider AssignmentProvider { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>ToastService</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    /// <summary>Ghi nhận lỗi tải dữ liệu để có thể truy vết theo kỳ đang thao tác.</summary>
    private ILogger<PhuCapTrachNhiemGanNhanVien> Logger { get; set; } = default!;

    /// <summary>Giá trị <c>Grid</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private PhuCapTrachNhiemGanNhanVienGrid? Grid { get; set; }
    /// <summary>Giá trị <c>ExportGrid</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private PhuCapTrachNhiemGanNhanVienExportGrid? ExportGrid { get; set; }
    /// <summary>Giá trị <c>Records</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentDto> Records { get; set; } = [];
    /// <summary>Giá trị <c>ExportRecords</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto> ExportRecords { get; set; } = [];
    /// <summary>Giá trị <c>Grades</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> Grades { get; set; } = [];
    /// <summary>Giá trị <c>SelectedDataItems</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private IReadOnlyList<object> SelectedDataItems => SelectionState.Items;
    /// <summary>Giá trị <c>SearchText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string? SearchText { get; set; }
    /// <summary>Giá trị <c>SelectedGradePresenceKey</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string SelectedGradePresenceKey { get; set; } = string.Empty;
    /// <summary>Giá trị <c>LoadErrorMessage</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string? LoadErrorMessage { get; set; }
    /// <summary>Giá trị <c>ToolbarMonth</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private int ToolbarMonth { get; set; }
    /// <summary>Giá trị <c>ToolbarYear</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private int ToolbarYear { get; set; }
    /// <summary>Giá trị <c>AppliedMonth</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private int AppliedMonth { get; set; }
    /// <summary>Giá trị <c>AppliedYear</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private int AppliedYear { get; set; }
    /// <summary>Giá trị <c>HasRequestedData</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool HasRequestedData { get; set; }
    /// <summary>Giá trị <c>IsLoading</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool IsLoading { get; set; }
    private bool IsLoadingPreviousMonth { get; set; }
    /// <summary>Giá trị <c>IsSaving</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool IsSaving { get; set; }
    /// <summary>Giá trị <c>IsExporting</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool IsExporting { get; set; }
    /// <summary>Giá trị <c>IsChangingPageSize</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool IsChangingPageSize { get; set; }
    /// <summary>Giá trị <c>IsEditPopupVisible</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool IsEditPopupVisible { get; set; }
    /// <summary>Giá trị <c>EditingRecord</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private PayrollResponsibilityAllowanceEmployeeAssignmentDto? EditingRecord { get; set; }
    /// <summary>Giá trị <c>EditModel</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private PhuCapTrachNhiemGanNhanVienEditModel EditModel { get; set; } = new();
    /// <summary>Giá trị <c>EditContext</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private EditContext EditContext { get; set; } = new(new PhuCapTrachNhiemGanNhanVienEditModel());
    /// <summary>Giá trị <c>AvailableMonthOptions</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private IReadOnlyList<EmployeeAssignmentMonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;

    /// <summary>Giá trị <c>ActiveGrades</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> ActiveGrades =>
        Grades.Where(grade => grade.IsActive)
            .OrderBy(grade => grade.DisplayOrder)
            .ThenBy(grade => grade.Code)
            .ToArray();

    /// <summary>Giá trị <c>HasLoadError</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    /// <summary>Giá trị <c>ShowLoadingPanel</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool ShowLoadingPanel => IsLoading || IsLoadingPreviousMonth || IsSaving || IsExporting || IsChangingPageSize;
    /// <summary>Giá trị <c>HasPendingPeriodChange</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool HasPendingPeriodChange => HasRequestedData && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    /// <summary>Giá trị <c>CanLoad</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool CanLoad => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanChangeFilters</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool CanChangeFilters => !ShowLoadingPanel;
    /// <summary>Trạng thái tương tác chung của các bộ lọc và section.</summary>
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    /// <summary>Trạng thái cho phép điều hướng dữ liệu hiện tại.</summary>
    private bool CanBrowsePages => CanInteract && HasRequestedData && !HasPendingPeriodChange && TotalRecordCount > 0;
    /// <summary>Giá trị <c>CanManageAssignments</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool CanManageAssignments => HasRequestedData && !HasPendingPeriodChange && !ShowLoadingPanel && !HasLoadError;
    private bool CanLoadFromPreviousMonth => CanManageAssignments;
    /// <summary>Giá trị <c>CanExport</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private bool CanExport => CanManageAssignments && TotalRecordCount > 0;
    /// <summary>Giá trị <c>PageSize</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private int PageSize { get; set; } = 50;
    /// <summary>Trang hiện tại, đánh số từ 0.</summary>
    private int CurrentPageIndex { get; set; }
    /// <summary>Tổng số hàng thỏa bộ lọc trên máy chủ.</summary>
    private int TotalRecordCount { get; set; }
    /// <summary>Tổng hợp số lượng gán theo bộ lọc hiện hành.</summary>
    private PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto AssignmentSummary { get; set; } = new(0, 0, 0);
    /// <summary>Số trang tối thiểu một để pager luôn hợp lệ.</summary>
    private int PageCount => Math.Max(1, (int)Math.Ceiling(TotalRecordCount / (double)PageSize));
    /// <summary>Giá trị <c>AssignedGradeCount</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private int AssignedGradeCount => AssignmentSummary.AssignedCount;
    /// <summary>Giá trị <c>UnassignedGradeCount</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private int UnassignedGradeCount => AssignmentSummary.UnassignedCount;
    /// <summary>Giá trị <c>AppliedPeriodLabel</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string AppliedPeriodLabel => HasRequestedData ? $"{AppliedMonth:00}/{AppliedYear}" : "Chưa chọn";
    /// <summary>Giá trị <c>LoadingText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string LoadingText => IsLoadingPreviousMonth
        ? $"Đang lấy giá trị từ kỳ trước cho kỳ {AppliedPeriodLabel}..."
        : IsSaving
            ? "Đang lưu cấp bậc nhân viên..."
            : IsExporting
                ? "Đang xuất danh sách cấp bậc nhân viên..."
                : "Đang tải danh sách cấp bậc nhân viên...";
    /// <summary>Giá trị <c>EmptyStateTitle</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải danh sách cấp bậc nhân viên"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
            : "Chưa có dữ liệu gán cấp bậc";
    /// <summary>Giá trị <c>EmptyStateMessage</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để tải dữ liệu."
        : HasPendingPeriodChange
            ? "Nhấn Xem để tải dữ liệu theo kỳ lương đang chọn."
            : "Nhấn Lấy từ tháng trước để đồng bộ nhân viên với Phụ cấp tổng hợp và kế thừa giá trị kỳ trước.";
    /// <summary>Giá trị <c>EmptyStateActionText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo nhân viên.</summary>
    private string EmptyStateActionText => HasRequestedData && !HasPendingPeriodChange ? "Làm mới" : "Xem dữ liệu";

    /// <summary>Xử lý sự kiện cho luồng <c>OnInitialized</c>.</summary>
    protected override void OnInitialized()
    {
        var period = GetDefaultPayrollPeriod();
        ToolbarMonth = period.Month;
        ToolbarYear = period.Year;
    }

    /// <summary>Giải phóng tài nguyên cho luồng <c>Dispose</c>.</summary>
    public void Dispose()
    {
        CancelActiveReload();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

}
