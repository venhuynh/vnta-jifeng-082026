using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Trình bày và chuyển tiếp thao tác cấu hình bậc/chức vụ về component cha.
/// Component không chứa quy tắc nghiệp vụ hoặc gọi HTTP trực tiếp.
/// </summary>
public partial class PhuCapTrachNhiemConfigurationPopup
{
    [Parameter] public bool IsConfigPopupVisible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string ConfigPopupPeriodLabel { get; set; } = string.Empty;
    [Parameter] public string? ConfigPopupErrorMessage { get; set; }
    [Parameter] public int ConfigPopupActiveTabIndex { get; set; }
    [Parameter] public EventCallback<int> ConfigPopupActiveTabIndexChanged { get; set; }
    [Parameter] public IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> GradeRows { get; set; } = [];
    [Parameter] public IReadOnlyList<PayrollResponsibilityAllowanceGradePositionDto> MappingRows { get; set; } = [];
    [Parameter] public IReadOnlyList<AttendancePositionRecord> PositionRows { get; set; } = [];
    [Parameter] public GradeFormModel GradeForm { get; set; } = GradeFormModel.CreateDefault();
    [Parameter] public MappingFormModel MappingForm { get; set; } = MappingFormModel.CreateDefault();
    [Parameter] public Guid? EditingGradeId { get; set; }
    [Parameter] public Guid? EditingMappingId { get; set; }
    [Parameter] public EventCallback OpenAssignmentsRequested { get; set; }
    [Parameter] public EventCallback CopyGradesRequested { get; set; }
    [Parameter] public EventCallback ResetGradeRequested { get; set; }
    [Parameter] public EventCallback<decimal> GradeAmountChanged { get; set; }
    [Parameter] public EventCallback<int> GradeDisplayOrderChanged { get; set; }
    [Parameter] public EventCallback SaveGradeRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceGradeDto> StartEditGradeRequested { get; set; }
    [Parameter] public EventCallback CopyMappingsRequested { get; set; }
    [Parameter] public EventCallback ResetMappingRequested { get; set; }
    [Parameter] public EventCallback SaveMappingRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceGradePositionDto> StartEditMappingRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceGradePositionDto> DeactivateMappingRequested { get; set; }
    [Parameter] public Func<decimal, string> FormatCurrency { get; set; } = static _ => string.Empty;
    [Parameter] public Func<bool, string> GetActiveTextCssClass { get; set; } = static _ => string.Empty;
    [Parameter] public Func<bool, string> GetActiveText { get; set; } = static _ => string.Empty;
    [Parameter] public Func<Guid, string> GetGradeLabel { get; set; } = static _ => string.Empty;

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool value) => VisibleChanged.InvokeAsync(value);
    /// <summary>Xử lý sự kiện cho luồng <c>OnActiveTabIndexChangedAsync</c>.</summary>
    private Task OnActiveTabIndexChangedAsync(int value) => ConfigPopupActiveTabIndexChanged.InvokeAsync(value);
    /// <summary>Mở cho luồng <c>OpenAssignmentsFromConfigAsync</c>.</summary>
    private Task OpenAssignmentsFromConfigAsync() => OpenAssignmentsRequested.InvokeAsync();
    /// <summary>Thực hiện xử lý cho luồng <c>CopyGradesFromPreviousMonthAsync</c>.</summary>
    private Task CopyGradesFromPreviousMonthAsync() => CopyGradesRequested.InvokeAsync();
    /// <summary>Đặt lại cho luồng <c>ResetGradeForm</c>.</summary>
    private Task ResetGradeForm() => ResetGradeRequested.InvokeAsync();
    /// <summary>Xử lý sự kiện cho luồng <c>OnGradeAmountChanged</c>.</summary>
    private Task OnGradeAmountChanged(decimal value) => GradeAmountChanged.InvokeAsync(value);
    /// <summary>Xử lý sự kiện cho luồng <c>OnGradeDisplayOrderChanged</c>.</summary>
    private Task OnGradeDisplayOrderChanged(int value) => GradeDisplayOrderChanged.InvokeAsync(value);
    /// <summary>Lưu cho luồng <c>SaveGradeAsync</c>.</summary>
    private Task SaveGradeAsync() => SaveGradeRequested.InvokeAsync();
    /// <summary>Thực hiện xử lý cho luồng <c>StartEditGrade</c>.</summary>
    private Task StartEditGrade(PayrollResponsibilityAllowanceGradeDto value) => StartEditGradeRequested.InvokeAsync(value);
    /// <summary>Thực hiện xử lý cho luồng <c>CopyMappingsFromPreviousMonthAsync</c>.</summary>
    private Task CopyMappingsFromPreviousMonthAsync() => CopyMappingsRequested.InvokeAsync();
    /// <summary>Đặt lại cho luồng <c>ResetMappingForm</c>.</summary>
    private Task ResetMappingForm() => ResetMappingRequested.InvokeAsync();
    /// <summary>Lưu cho luồng <c>SaveMappingAsync</c>.</summary>
    private Task SaveMappingAsync() => SaveMappingRequested.InvokeAsync();
    /// <summary>Thực hiện xử lý cho luồng <c>StartEditMapping</c>.</summary>
    private Task StartEditMapping(PayrollResponsibilityAllowanceGradePositionDto value) => StartEditMappingRequested.InvokeAsync(value);
    /// <summary>Thực hiện xử lý cho luồng <c>DeactivateMappingAsync</c>.</summary>
    private Task DeactivateMappingAsync(PayrollResponsibilityAllowanceGradePositionDto value) => DeactivateMappingRequested.InvokeAsync(value);
}
