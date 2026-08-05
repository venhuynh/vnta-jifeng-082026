using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Đại diện kiểu <c>PhuCapThamNienEditPopup</c> phục vụ giao diện phụ cấp thâm niên.</summary>
public partial class PhuCapThamNienEditPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Sửa phụ cấp thâm niên";
    [Parameter] public PhuCapThamNienEditModel Model { get; set; } = new();
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback<PhuCapThamNienEditModel> SaveRequested { get; set; }

    private PhuCapThamNienEditModel? sourceModel;
    private PhuCapThamNienEditModel Draft { get; set; } = new();

    protected override void OnParametersSet()
    {
        if(ReferenceEquals(sourceModel, Model))
        {
            return;
        }

        sourceModel = Model;
        Draft = Clone(Model);
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    /// <summary>Lưu cho luồng <c>SaveAsync</c>.</summary>
    private Task SaveAsync() => SaveRequested.InvokeAsync(Clone(Draft));

    /// <summary>Xử lý sự kiện cho luồng <c>OnAllowanceAmountChangedAsync</c>.</summary>
    private Task OnAllowanceAmountChangedAsync(decimal value)
    {
        Draft.AllowanceAmount = Math.Max(0m, value);
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnNoteChangedAsync</c>.</summary>
    private Task OnNoteChangedAsync(string? value)
    {
        Draft.Note = value;
        return Task.CompletedTask;
    }

    private static PhuCapThamNienEditModel Clone(PhuCapThamNienEditModel source) => new()
    {
        PayrollAllowanceSummaryRecordId = source.PayrollAllowanceSummaryRecordId,
        EmployeeDisplay = source.EmployeeDisplay,
        AllowanceAmount = source.AllowanceAmount,
        Note = source.Note,
        IsLocked = source.IsLocked,
        OriginalUpdatedAtUtc = source.OriginalUpdatedAtUtc
    };
}
