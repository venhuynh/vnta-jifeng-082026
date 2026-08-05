using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Employees;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.NhanVien;

public partial class NhanVienCreatePopup
{
    private const int LookupPageSize = 8;

    private static readonly int[] LookupPageSizeSelectorItems = [8, 15, 30, 50];
    private static readonly IReadOnlyList<EmployeeStatusOption> StatusOptions =
    [
        new(EmployeeEmploymentStatus.Probation, "Thử việc", "status-probation"),
        new(EmployeeEmploymentStatus.Official, "Chính thức", "status-official"),
        new(EmployeeEmploymentStatus.Resigned, "Nghỉ việc", "status-resigned")
    ];

    private EditContext? CreateEditContext { get; set; }
    private string? departmentSearchText;
    private string? positionSearchText;
    private bool DepartmentDropDownVisible { get; set; }
    private bool PositionDropDownVisible { get; set; }
    private int DepartmentLookupPageIndex { get; set; }
    private int PositionLookupPageIndex { get; set; }
    private bool isSubmittingCreate;

    private string? DepartmentSearchText
    {
        get => departmentSearchText;
        set
        {
            if (departmentSearchText == value)
            {
                return;
            }

            departmentSearchText = value;
            DepartmentLookupPageIndex = 0;
        }
    }

    private string? PositionSearchText
    {
        get => positionSearchText;
        set
        {
            if (positionSearchText == value)
            {
                return;
            }

            positionSearchText = value;
            PositionLookupPageIndex = 0;
        }
    }

    [Parameter]
    public CreateEmployeeFormModel? CreateModel { get; set; }

    [Parameter]
    public IReadOnlyList<AttendanceDepartmentRecord> Departments { get; set; } = [];

    [Parameter]
    public IReadOnlyList<AttendancePositionRecord> Positions { get; set; } = [];

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public bool IsLoadingLookups { get; set; }

    [Parameter]
    public string? EditErrorMessage { get; set; }

    [Parameter]
    public string? LookupErrorMessage { get; set; }

    [Parameter]
    public EventCallback<CreateEmployeeFormModel> CreateRequested { get; set; }

    [Parameter]
    public bool IsEditMode { get; set; }

    [Parameter]
    public EventCallback<CreateEmployeeFormModel> UpdateRequested { get; set; }

    private bool HasLookupError => !string.IsNullOrWhiteSpace(LookupErrorMessage);

    private bool IsCreateActionBusy => IsLoadingLookups || IsSaving || isSubmittingCreate;

    private bool CanEditCreateFields => !IsCreateActionBusy && !HasLookupError;

    private bool CanClosePopup => !IsSaving && !isSubmittingCreate;

    private bool CanSave => !IsCreateActionBusy && !HasLookupError && CreateEditContext is not null;

    private string HeaderText => IsEditMode ? "Điều chỉnh nhân viên" : "Thêm mới nhân viên";

    private string SaveButtonText => IsSaving || isSubmittingCreate ? "Đang lưu" : "Lưu";

    private string CreateLoadingText => IsSaving || isSubmittingCreate
        ? "Đang lưu thông tin nhân viên..."
        : "Đang tải danh mục phòng ban và chức vụ...";

    private IEnumerable<AttendanceDepartmentRecord> FilteredDepartments =>
        FilterRows(
            Departments,
            DepartmentSearchText,
            department => department.Name,
            department => department.FullPath);

    private IEnumerable<AttendancePositionRecord> FilteredPositions =>
        FilterRows(
            Positions,
            PositionSearchText,
            position => position.Name,
            position => position.Description);

    protected override void OnParametersSet()
    {
        if (CreateModel is not null
            && (CreateEditContext is null || !ReferenceEquals(CreateEditContext.Model, CreateModel)))
        {
            CreateEditContext = new EditContext(CreateModel);
            DepartmentSearchText = null;
            PositionSearchText = null;
            DepartmentLookupPageIndex = 0;
            PositionLookupPageIndex = 0;
            DepartmentDropDownVisible = false;
            PositionDropDownVisible = false;
            isSubmittingCreate = false;
        }

        base.OnParametersSet();
    }

    private Task OnVisibleChanged(bool visible)
    {
        if (!visible)
        {
            CloseLookupDropDowns();
            isSubmittingCreate = false;
        }

        return VisibleChanged.InvokeAsync(visible);
    }

    private Task CloseAsync()
    {
        CloseLookupDropDowns();
        isSubmittingCreate = false;
        return VisibleChanged.InvokeAsync(false);
    }

    private async Task OnValidSubmitAsync()
    {
        CloseLookupDropDowns();
        await SaveCreateAsync();
    }

    private async Task OnSaveClickAsync()
    {
        if (!CanSave || CreateEditContext is null)
        {
            return;
        }

        CloseLookupDropDowns();
        if (CreateEditContext.Validate())
        {
            await SaveCreateAsync();
        }
    }

    private async Task SaveCreateAsync()
    {
        if (CreateModel is null || !CanSave)
        {
            return;
        }

        isSubmittingCreate = true;

        try
        {
            if (IsEditMode)
            {
                await UpdateRequested.InvokeAsync(CreateModel);
            }
            else
            {
                await CreateRequested.InvokeAsync(CreateModel);
            }
        }
        finally
        {
            isSubmittingCreate = false;
        }
    }

    private Task OnCancelClickAsync() => CanClosePopup ? CloseAsync() : Task.CompletedTask;

    private void CloseLookupDropDowns()
    {
        DepartmentDropDownVisible = false;
        PositionDropDownVisible = false;
    }

    private Task OnDepartmentDropDownVisibleChanged(bool visible)
    {
        DepartmentDropDownVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnPositionDropDownVisibleChanged(bool visible)
    {
        PositionDropDownVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnDepartmentLookupPageIndexChanged(int pageIndex)
    {
        DepartmentLookupPageIndex = pageIndex;
        return Task.CompletedTask;
    }

    private Task OnPositionLookupPageIndexChanged(int pageIndex)
    {
        PositionLookupPageIndex = pageIndex;
        return Task.CompletedTask;
    }

    private Task OnDepartmentValueChanged(object? value)
    {
        if (CreateModel is null)
        {
            return Task.CompletedTask;
        }

        CreateModel.DepartmentId = value as Guid?;
        NotifyFieldChanged(nameof(CreateEmployeeFormModel.DepartmentId));
        return Task.CompletedTask;
    }

    private Task OnPositionValueChanged(object? value)
    {
        if (CreateModel is null)
        {
            return Task.CompletedTask;
        }

        CreateModel.PositionId = value as Guid?;
        NotifyFieldChanged(nameof(CreateEmployeeFormModel.PositionId));
        return Task.CompletedTask;
    }

    private Task OnStatusValueChanged(EmployeeEmploymentStatus value)
    {
        if (CreateModel is null)
        {
            return Task.CompletedTask;
        }

        CreateModel.Status = value;
        NotifyFieldChanged(nameof(CreateEmployeeFormModel.Status));
        return Task.CompletedTask;
    }

    private Task OnDepartmentLookupRowClick(GridRowClickEventArgs args)
    {
        if (args.Grid.GetDataItem(args.VisibleIndex) is AttendanceDepartmentRecord department)
        {
            SelectDepartment(department);
        }

        return Task.CompletedTask;
    }

    private Task OnPositionLookupRowClick(GridRowClickEventArgs args)
    {
        if (args.Grid.GetDataItem(args.VisibleIndex) is AttendancePositionRecord position)
        {
            SelectPosition(position);
        }

        return Task.CompletedTask;
    }

    private void SelectDepartment(AttendanceDepartmentRecord department)
    {
        if (CreateModel is null)
        {
            return;
        }

        CreateModel.DepartmentId = department.Id;
        DepartmentDropDownVisible = false;
        NotifyFieldChanged(nameof(CreateEmployeeFormModel.DepartmentId));
    }

    private void SelectPosition(AttendancePositionRecord position)
    {
        if (CreateModel is null)
        {
            return;
        }

        CreateModel.PositionId = position.Id;
        PositionDropDownVisible = false;
        NotifyFieldChanged(nameof(CreateEmployeeFormModel.PositionId));
    }

    private string GetDepartmentDisplayText(DropDownBoxQueryDisplayTextContext context)
    {
        var departmentId = ResolveGuid(context.Value) ?? CreateModel?.DepartmentId;
        return departmentId.HasValue
            ? FormatDepartment(Departments.FirstOrDefault(department => department.Id == departmentId.Value))
            : string.Empty;
    }

    private string GetPositionDisplayText(DropDownBoxQueryDisplayTextContext context)
    {
        var positionId = ResolveGuid(context.Value) ?? CreateModel?.PositionId;
        return positionId.HasValue
            ? FormatPosition(Positions.FirstOrDefault(position => position.Id == positionId.Value))
            : string.Empty;
    }

    private static Guid? ResolveGuid(object? value) =>
        value switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var guid) => guid,
            _ => null
        };

    private void NotifyFieldChanged(string fieldName)
    {
        if (CreateModel is not null)
        {
            CreateEditContext?.NotifyFieldChanged(new FieldIdentifier(CreateModel, fieldName));
        }
    }

    private static IEnumerable<T> FilterRows<T>(
        IEnumerable<T> rows,
        string? searchText,
        params Func<T, string?>[] selectors)
    {
        var normalizedSearch = Normalize(searchText);
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return rows;
        }

        return rows.Where(row =>
            selectors.Any(selector =>
                Normalize(selector(row))?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true));
    }

    private static string FormatDepartment(AttendanceDepartmentRecord? department) =>
        department is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(department.FullPath)
                ? department.Name
                : department.FullPath;

    private static string FormatPosition(AttendancePositionRecord? position) =>
        position?.Name ?? string.Empty;

    private static string GetStatusOptionCssClass(EmployeeStatusOption? option) =>
        string.Join(' ', "employee-status-option", option?.CssClass);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record EmployeeStatusOption(
        EmployeeEmploymentStatus Value,
        string Text,
        string CssClass);
}
