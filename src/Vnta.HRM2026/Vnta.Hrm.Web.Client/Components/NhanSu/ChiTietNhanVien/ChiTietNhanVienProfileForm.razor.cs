using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Application.Common;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.NhanSu.ChiTietNhanVien;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.ChiTietNhanVien;

public partial class ChiTietNhanVienProfileForm
{
    private const int LookupPageSize = 10;

    private static readonly IReadOnlyList<int> LookupPageSizeSelectorItems = [10, 20, 50];
    private static readonly IReadOnlyList<ChiTietNhanVienStatusOption> StatusOptions =
    [
        new(ChiTietNhanVienEmploymentStatus.Probation, "Thử việc"),
        new(ChiTietNhanVienEmploymentStatus.Official, "Chính thức"),
        new(ChiTietNhanVienEmploymentStatus.Resigned, "Nghỉ việc")
    ];

    private long observedSavedVersion;

    [Parameter]
    public ChiTietNhanVienRecord Employee { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<AttendanceDepartmentRecord> Departments { get; set; } = [];

    [Parameter]
    public IReadOnlyList<AttendancePositionRecord> Positions { get; set; } = [];

    [Parameter]
    public bool IsBusy { get; set; }

    [Parameter]
    public string? LookupErrorMessage { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public long SavedVersion { get; set; }

    [Parameter]
    public EventCallback<bool> EditModeChanged { get; set; }

    [Parameter]
    public EventCallback<ChiTietNhanVienEditModel> SaveRequested { get; set; }

    private ChiTietNhanVienEditModel? EditModel { get; set; }
    private EditContext? EditContext { get; set; }
    private bool IsEditMode { get; set; }
    private bool DepartmentDropDownVisible { get; set; }
    private bool PositionDropDownVisible { get; set; }
    private string? DepartmentSearchText { get; set; }
    private string? PositionSearchText { get; set; }
    private int DepartmentLookupPageIndex { get; set; }
    private int PositionLookupPageIndex { get; set; }
    private bool CanEdit => !IsBusy && string.IsNullOrWhiteSpace(LookupErrorMessage);
    private bool CanSave => CanEdit && EditModel is not null && EditContext is not null;
    private string? AvatarSource => AvatarImageSourceHelper.NormalizeSource(Employee.AvatarDataUrl);
    private string EmployeeInitials => BuildInitials(Employee.FullName);
    private IEnumerable<AttendanceDepartmentRecord> FilteredDepartments =>
        FilterRows(Departments, DepartmentSearchText, department => department.Name, department => department.FullPath);
    private IEnumerable<AttendancePositionRecord> FilteredPositions =>
        FilterRows(Positions, PositionSearchText, position => position.Name, position => position.Description);

    protected override async Task OnParametersSetAsync()
    {
        if(observedSavedVersion == SavedVersion)
        {
            return;
        }

        observedSavedVersion = SavedVersion;
        EditModel = null;
        EditContext = null;
        IsEditMode = false;
        await EditModeChanged.InvokeAsync(false);
    }

    public async Task BeginEditAsync()
    {
        if(!CanEdit)
        {
            return;
        }

        EditModel = new ChiTietNhanVienEditModel
        {
            EmployeeCode = Employee.EmployeeCode,
            FullName = Employee.FullName,
            DepartmentId = Employee.DepartmentId,
            PositionId = Employee.PositionId,
            Status = Employee.EmploymentStatus ?? ChiTietNhanVienEmploymentStatus.Probation,
            HireDate = Employee.HireDate,
            SeniorityStartDate = Employee.SeniorityStartDate,
            ResignedDate = Employee.ResignedDate,
            OriginalUpdatedAtUtc = Employee.UpdatedAtUtc ?? Employee.CreatedAtUtc
        };
        EditContext = new EditContext(EditModel);
        DepartmentSearchText = null;
        PositionSearchText = null;
        DepartmentLookupPageIndex = 0;
        PositionLookupPageIndex = 0;
        DepartmentDropDownVisible = false;
        PositionDropDownVisible = false;
        IsEditMode = true;
        await EditModeChanged.InvokeAsync(true);
    }

    private async Task SaveAsync()
    {
        if(!CanSave || EditModel is null || EditContext is null || !EditContext.Validate())
        {
            return;
        }

        await SaveRequested.InvokeAsync(EditModel);
    }

    private async Task CancelEditAsync()
    {
        if(IsBusy)
        {
            return;
        }

        EditModel = null;
        EditContext = null;
        CloseLookupDropDowns();
        IsEditMode = false;
        await EditModeChanged.InvokeAsync(false);
    }

    private static string DisplayOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();

    private static string DisplayDate(DateTime? value) =>
        value?.ToString("dd/MM/yyyy") ?? "Chưa có";

    private Task OnDepartmentDropDownVisibleChangedAsync(bool visible)
    {
        DepartmentDropDownVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnPositionDropDownVisibleChangedAsync(bool visible)
    {
        PositionDropDownVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnDepartmentLookupPageIndexChangedAsync(int pageIndex)
    {
        DepartmentLookupPageIndex = pageIndex;
        return Task.CompletedTask;
    }

    private Task OnPositionLookupPageIndexChangedAsync(int pageIndex)
    {
        PositionLookupPageIndex = pageIndex;
        return Task.CompletedTask;
    }

    private Task OnDepartmentValueChangedAsync(object? value)
    {
        if(EditModel is null) return Task.CompletedTask;

        EditModel.DepartmentId = ResolveGuid(value);
        NotifyFieldChanged(nameof(ChiTietNhanVienEditModel.DepartmentId));
        return Task.CompletedTask;
    }

    private Task OnPositionValueChangedAsync(object? value)
    {
        if(EditModel is null) return Task.CompletedTask;

        EditModel.PositionId = ResolveGuid(value);
        NotifyFieldChanged(nameof(ChiTietNhanVienEditModel.PositionId));
        return Task.CompletedTask;
    }

    private Task OnDepartmentLookupRowClickAsync(GridRowClickEventArgs args)
    {
        if(args.Grid.GetDataItem(args.VisibleIndex) is AttendanceDepartmentRecord department && EditModel is not null)
        {
            EditModel.DepartmentId = department.Id;
            DepartmentDropDownVisible = false;
            NotifyFieldChanged(nameof(ChiTietNhanVienEditModel.DepartmentId));
        }

        return Task.CompletedTask;
    }

    private Task OnPositionLookupRowClickAsync(GridRowClickEventArgs args)
    {
        if(args.Grid.GetDataItem(args.VisibleIndex) is AttendancePositionRecord position && EditModel is not null)
        {
            EditModel.PositionId = position.Id;
            PositionDropDownVisible = false;
            NotifyFieldChanged(nameof(ChiTietNhanVienEditModel.PositionId));
        }

        return Task.CompletedTask;
    }

    private string GetDepartmentDisplayText(DropDownBoxQueryDisplayTextContext context)
    {
        var departmentId = ResolveGuid(context.Value) ?? EditModel?.DepartmentId;
        return departmentId.HasValue
            ? FormatDepartment(Departments.FirstOrDefault(department => department.Id == departmentId.Value))
            : string.Empty;
    }

    private string GetPositionDisplayText(DropDownBoxQueryDisplayTextContext context)
    {
        var positionId = ResolveGuid(context.Value) ?? EditModel?.PositionId;
        return positionId.HasValue
            ? Positions.FirstOrDefault(position => position.Id == positionId.Value)?.Name ?? string.Empty
            : string.Empty;
    }

    private void CloseLookupDropDowns()
    {
        DepartmentDropDownVisible = false;
        PositionDropDownVisible = false;
    }

    private void NotifyFieldChanged(string fieldName)
    {
        if(EditModel is not null && EditContext is not null)
        {
            EditContext.NotifyFieldChanged(new FieldIdentifier(EditModel, fieldName));
        }
    }

    private static Guid? ResolveGuid(object? value) => value switch
    {
        Guid guid => guid,
        string text when Guid.TryParse(text, out var guid) => guid,
        _ => null
    };

    private static IEnumerable<T> FilterRows<T>(
        IEnumerable<T> rows,
        string? searchText,
        params Func<T, string?>[] selectors)
    {
        var normalizedSearch = Normalize(searchText);
        return string.IsNullOrWhiteSpace(normalizedSearch)
            ? rows
            : rows.Where(row => selectors.Any(selector =>
                Normalize(selector(row))?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true));
    }

    private static string FormatDepartment(AttendanceDepartmentRecord? department) =>
        department is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(department.FullPath)
                ? department.Name
                : department.FullPath;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildInitials(string? fullName)
    {
        var words = fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        return words.Length switch
        {
            0 => "NV",
            1 => words[0][..1].ToUpperInvariant(),
            _ => string.Concat(words[0][..1], words[^1][..1]).ToUpperInvariant()
        };
    }

    private sealed record ChiTietNhanVienStatusOption(ChiTietNhanVienEmploymentStatus Value, string Text);
}
