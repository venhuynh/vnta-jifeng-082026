using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.CaKip.LichLamViec;

public partial class LichLamViec : IDisposable
{
    private const int MinimumSupportedYear = 1900;
    private const int MaximumSupportedYear = 2100;
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly CancellationTokenSource disposalTokenSource = new();

    private static readonly IReadOnlyList<WorkCalendarDayTypeOption> DayTypeOptions =
    [
        new(
            AttendanceWorkCalendarDayType.Regular,
            AttendanceWorkCalendarDayTypes.GetDisplayName(AttendanceWorkCalendarDayType.Regular)),
        new(
            AttendanceWorkCalendarDayType.DayOff,
            AttendanceWorkCalendarDayTypes.GetDisplayName(AttendanceWorkCalendarDayType.DayOff)),
        new(
            AttendanceWorkCalendarDayType.Holiday,
            AttendanceWorkCalendarDayTypes.GetDisplayName(AttendanceWorkCalendarDayType.Holiday))
    ];

    [Inject]
    private AttendanceWorkCalendarDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private IHrmDialogService DialogService { get; set; } = default!;

    private IReadOnlyList<AttendanceWorkCalendarDayRecord> CalendarDays { get; set; } = [];
    private IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayRecord> CalendarDaysByDate { get; set; } =
        new Dictionary<DateOnly, AttendanceWorkCalendarDayRecord>();
    private IReadOnlyList<WorkCalendarMonthView> Months { get; set; } = [];
    private AttendanceWorkCalendarDayRecord? EditModel { get; set; }
    private EditContext? EditContext { get; set; }
    private DateTime SelectedCalendarDate { get; set; } = DateTime.Today;
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private int SelectedYear { get; set; } = DateTime.Today.Year;
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsCreatingNewDay { get; set; }

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanInteract => !IsLoading && !IsSaving && !HasLoadError;
    private bool CanChangeYear => !IsLoading && !IsSaving;
    private bool CanCreateSpecialDay => CanInteract;
    private bool CanEditSelectedDay => CanInteract && SelectedSpecialDay is not null;
    private bool CanDeleteSelectedDay => CanInteract && SelectedSpecialDay is not null;
    private DateOnly SelectedDate => DateOnly.FromDateTime(SelectedCalendarDate.Date);
    private AttendanceWorkCalendarDayRecord? SelectedSpecialDay =>
        CalendarDaysByDate.TryGetValue(SelectedDate, out var record)
            ? record
            : null;
    private DateTime SelectedYearFirstDate => new(SelectedYear, 1, 1);
    private DateTime SelectedYearLastDate => new(SelectedYear, 12, 31);
    private string EditPopupTitle => EditModel?.WorkDateOnly is DateOnly workDate
        ? $"Cập nhật {FormatDate(workDate)}"
        : "Cập nhật loại ngày";
    private string SelectedDayTitle => FormatDate(SelectedDate);
    private string SelectedDayTypeText =>
        AttendanceWorkCalendarDayTypes.GetDisplayName(ResolveDayType(SelectedDate, SelectedSpecialDay));
    private string SelectedDayBadgeCssClass => ResolveDayType(SelectedDate, SelectedSpecialDay) switch
    {
        AttendanceWorkCalendarDayType.Holiday => "day-type-badge day-type-badge-holiday",
        AttendanceWorkCalendarDayType.DayOff => "day-type-badge day-type-badge-day-off",
        _ => "day-type-badge day-type-badge-regular"
    };

    protected override async Task OnInitializedAsync()
    {
        Months = BuildMonths(SelectedYear);
        await LoadYearAsync(showLoading: true);
        await base.OnInitializedAsync();
    }

    private async Task ReloadAsync()
    {
        if(disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        LoadErrorMessage = null;
        IsLoading = true;

        try
        {
            CalendarDays = await DataProvider.EnsureSundayDayOffsAsync(
                SelectedYear,
                disposalTokenSource.Token);
            RebuildCalendarIndex();
            ToastService.ShowSuccess("Đã đồng bộ toàn bộ Chủ nhật trong năm thành Ngày nghỉ.");
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
            CalendarDays = [];
            RebuildCalendarIndex();
            LoadErrorMessage = $"Có lỗi khi làm mới lịch làm việc: {ex.Message}";
            ToastService.ShowError("Không thể đồng bộ Chủ nhật trong năm.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnSelectedYearChangedAsync(int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        if(normalizedYear == SelectedYear)
        {
            return;
        }

        SelectedYear = normalizedYear;
        Months = BuildMonths(SelectedYear);
        EnsureSelectedDateInsideYear();
        await LoadYearAsync(showLoading: true);
    }

    private Task OnMonthVisibleDateChangedAsync(int month, DateTime visibleDate)
    {
        return Task.CompletedTask;
    }

    private Task OnCalendarDateSelectedAsync(DateTime selectedDate)
    {
        SelectedCalendarDate = selectedDate.Date;
        return Task.CompletedTask;
    }

    private Task SelectDayAsync(DateTime selectedDate, int month)
    {
        if(selectedDate.Month != month || !CanInteract)
        {
            return Task.CompletedTask;
        }

        SelectedCalendarDate = selectedDate.Date;
        OpenDayPopup(selectedDate.Date);
        return Task.CompletedTask;
    }

    private Task SelectConfiguredDayAsync(AttendanceWorkCalendarDayRecord day)
    {
        if(day.WorkDate.HasValue)
        {
            SelectedCalendarDate = day.WorkDate.Value.Date;
            OpenDayPopup(SelectedCalendarDate);
        }

        return Task.CompletedTask;
    }

    private Task OpenCreateDayAsync()
    {
        OpenDayPopup(SelectedCalendarDate);
        return Task.CompletedTask;
    }

    private Task OpenEditSelectedDayAsync()
    {
        OpenDayPopup(SelectedCalendarDate);
        return Task.CompletedTask;
    }

    private async Task DeleteSelectedDayAsync()
    {
        if(SelectedSpecialDay is null)
        {
            ToastService.ShowWarning("Hãy chọn ngày nghỉ hoặc ngày lễ cần đưa về Ngày thường.");
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có chắc muốn đưa ngày {FormatDate(SelectedSpecialDay.WorkDateOnly)} về Ngày thường?",
            title: "Xác nhận đưa về Ngày thường",
            okText: "Đồng ý",
            cancelText: "Hủy");

        if(!confirmed)
        {
            return;
        }

        IsLoading = true;
        try
        {
            CalendarDays = await DataProvider.DeleteAsync(
                SelectedSpecialDay.Id,
                SelectedYear,
                disposalTokenSource.Token);
            RebuildCalendarIndex();
            ToastService.ShowSuccess("Đã đưa ngày đã chọn về Ngày thường.");
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
            ToastService.ShowError($"Không thể đưa ngày đã chọn về Ngày thường: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveEditAsync()
    {
        if(EditModel is null)
        {
            return;
        }

        EditErrorMessage = null;
        NormalizeEditModel(EditModel);

        var workDate = EditModel.WorkDateOnly;
        if(workDate.HasValue && workDate.Value.Year != SelectedYear)
        {
            SelectedYear = workDate.Value.Year;
            Months = BuildMonths(SelectedYear);
        }

        IsSaving = true;
        try
        {
            if(EditModel.DayType == AttendanceWorkCalendarDayType.Regular)
            {
                await SaveRegularDayAsync(EditModel);
                return;
            }

            var validationMessage = await DataProvider.ValidateAsync(EditModel, disposalTokenSource.Token);
            if(!string.IsNullOrWhiteSpace(validationMessage))
            {
                EditErrorMessage = validationMessage;
                return;
            }

            CalendarDays = await DataProvider.SaveAsync(
                EditModel,
                IsCreatingNewDay,
                SelectedYear,
                disposalTokenSource.Token);
            RebuildCalendarIndex();
            if(EditModel.WorkDate.HasValue)
            {
                SelectedCalendarDate = EditModel.WorkDate.Value.Date;
            }

            IsEditPopupVisible = false;
            ToastService.ShowSuccess(IsCreatingNewDay ? "Đã thêm ngày đặc biệt." : "Đã cập nhật ngày đặc biệt.");
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
            EditErrorMessage = ex.Message;
            ToastService.ShowError("Không thể lưu lịch làm việc.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private Task OnInvalidSubmitAsync(EditContext editContext)
    {
        EditErrorMessage = "Vui lòng kiểm tra lại các thông tin bắt buộc.";
        return Task.CompletedTask;
    }

    private Task OnEditPopupVisibleChangedAsync(bool visible)
    {
        IsEditPopupVisible = visible;
        if(!visible)
        {
            EditModel = null;
            EditContext = null;
            EditErrorMessage = null;
        }

        return Task.CompletedTask;
    }

    private Task CloseEditPopupAsync() => OnEditPopupVisibleChangedAsync(false);

    private async Task LoadYearAsync(bool showLoading)
    {
        LoadErrorMessage = null;
        if(showLoading)
        {
            IsLoading = true;
        }

        try
        {
            CalendarDays = await DataProvider.GetYearAsync(SelectedYear, disposalTokenSource.Token);
            RebuildCalendarIndex();
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
            CalendarDays = [];
            RebuildCalendarIndex();
            LoadErrorMessage = $"Có lỗi khi tải lịch làm việc: {ex.Message}";
            ToastService.ShowError("Không thể tải lịch làm việc.");
        }
        finally
        {
            if(showLoading)
            {
                IsLoading = false;
            }
        }
    }

    private void OpenEditPopup(AttendanceWorkCalendarDayRecord model, bool isNew)
    {
        EditModel = model;
        EditContext = new EditContext(EditModel);
        EditErrorMessage = null;
        IsCreatingNewDay = isNew;
        IsEditPopupVisible = true;
    }

    private void OpenDayPopup(DateTime selectedDate)
    {
        var workDate = DateOnly.FromDateTime(selectedDate.Date);
        if(CalendarDaysByDate.TryGetValue(workDate, out var configuredDay))
        {
            OpenEditPopup(configuredDay.Clone(), isNew: false);
            return;
        }

        var now = DateTime.UtcNow;
        OpenEditPopup(
            new AttendanceWorkCalendarDayRecord
            {
                Id = Guid.NewGuid(),
                WorkDate = selectedDate.Date,
                DayType = AttendanceWorkCalendarDayTypes.ResolveDefaultDayType(workDate),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            isNew: true);
    }

    private async Task SaveRegularDayAsync(AttendanceWorkCalendarDayRecord model)
    {
        var workDate = model.WorkDateOnly;
        if(!workDate.HasValue)
        {
            EditErrorMessage = "Ngày làm việc không được để trống.";
            return;
        }

        if(CalendarDaysByDate.TryGetValue(workDate.Value, out var configuredDay))
        {
            CalendarDays = await DataProvider.DeleteAsync(
                configuredDay.Id,
                workDate.Value.Year,
                disposalTokenSource.Token);
            RebuildCalendarIndex();
        }

        SelectedYear = workDate.Value.Year;
        Months = BuildMonths(SelectedYear);
        SelectedCalendarDate = workDate.Value.ToDateTime(TimeOnly.MinValue);
        IsEditPopupVisible = false;
        ToastService.ShowSuccess("Đã đưa ngày đã chọn về Ngày thường.");
    }

    private void RebuildCalendarIndex()
    {
        CalendarDaysByDate = CalendarDays
            .Where(day => day.WorkDateOnly.HasValue)
            .ToDictionary(day => day.WorkDateOnly!.Value, day => day);
    }

    private void EnsureSelectedDateInsideYear()
    {
        if(SelectedCalendarDate.Year == SelectedYear)
        {
            return;
        }

        SelectedCalendarDate = new DateTime(SelectedYear, 1, 1);
    }

    private AttendanceWorkCalendarDayRecord? GetDayRecord(DateTime date)
    {
        var workDate = DateOnly.FromDateTime(date.Date);
        return CalendarDaysByDate.TryGetValue(workDate, out var record) ? record : null;
    }

    private IReadOnlyList<AttendanceWorkCalendarDayRecord> GetMonthSpecialDays(int month) =>
        CalendarDays
            .Where(day => day.WorkDate?.Year == SelectedYear && day.WorkDate?.Month == month)
            .OrderBy(day => day.WorkDate)
            .ToArray();

    private string GetMonthSpecialDayCountText(int month)
    {
        var count = GetMonthSpecialDays(month).Count;
        return count == 0 ? "0 ngày đặc biệt" : $"{count:N0} ngày đặc biệt";
    }

    private string GetDayCellCssClass(DateTime date, int month)
    {
        var cssClass = "work-calendar-day-cell";
        if(date.Month != month)
        {
            cssClass += " work-calendar-day-cell-outside";
        }

        if(SelectedCalendarDate.Date == date.Date)
        {
            cssClass += " work-calendar-day-cell-selected";
        }

        var dayRecord = GetDayRecord(date);
        var dayType = ResolveDayType(date, dayRecord);

        return dayType switch
        {
            AttendanceWorkCalendarDayType.Holiday => $"{cssClass} work-calendar-day-cell-holiday",
            AttendanceWorkCalendarDayType.DayOff => $"{cssClass} work-calendar-day-cell-day-off",
            _ => cssClass
        };
    }

    private string GetMonthDayButtonCssClass(AttendanceWorkCalendarDayRecord day) => day.DayType switch
    {
        AttendanceWorkCalendarDayType.Holiday => "work-calendar-month-day work-calendar-month-day-holiday",
        _ => "work-calendar-month-day work-calendar-month-day-day-off"
    };

    private string BuildDayCellTitle(DateTime date, AttendanceWorkCalendarDayRecord? dayRecord)
    {
        var dayText = DateOnly.FromDateTime(date.Date).ToString("dd/MM/yyyy", DisplayCulture);
        var dayType = ResolveDayType(date, dayRecord);
        return dayRecord is null
            ? $"{dayText} - {AttendanceWorkCalendarDayTypes.GetDisplayName(dayType)}"
            : $"{dayText} - {AttendanceWorkCalendarDayTypes.GetDisplayName(dayType)}: {dayRecord.DisplayName}";
    }

    private static AttendanceWorkCalendarDayType ResolveDayType(
        DateTime date,
        AttendanceWorkCalendarDayRecord? dayRecord) =>
        ResolveDayType(DateOnly.FromDateTime(date.Date), dayRecord);

    private static AttendanceWorkCalendarDayType ResolveDayType(
        DateOnly workDate,
        AttendanceWorkCalendarDayRecord? dayRecord) =>
        dayRecord?.DayType ?? AttendanceWorkCalendarDayTypes.ResolveDefaultDayType(workDate);

    private static string GetShortDayType(AttendanceWorkCalendarDayType dayType) =>
        AttendanceWorkCalendarDayTypes.GetShortDisplayName(dayType);

    private string BuildConfiguredDayTitle(AttendanceWorkCalendarDayRecord day) =>
        $"{FormatDate(day.WorkDateOnly)} - {AttendanceWorkCalendarDayTypes.GetDisplayName(day.DayType)}: {day.DisplayName}";

    private string FormatDate(DateOnly? value) =>
        value.HasValue ? FormatDate(value.Value) : "--";

    private string FormatDate(DateOnly value) =>
        value.ToString("dd/MM/yyyy", DisplayCulture);

    private static void NormalizeEditModel(AttendanceWorkCalendarDayRecord model)
    {
        model.WorkDate = model.WorkDate?.Date;
        if(!AttendanceWorkCalendarDayTypes.All.Contains(model.DayType))
        {
            model.DayType = AttendanceWorkCalendarDayType.Regular;
        }

        model.Name = model.DayType == AttendanceWorkCalendarDayType.Holiday
            ? Normalize(model.Name)
            : null;
        model.Note = Normalize(model.Note);

        if(model.CreatedAtUtc == default)
        {
            model.CreatedAtUtc = DateTime.UtcNow;
        }

        model.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<WorkCalendarMonthView> BuildMonths(int year) =>
        Enumerable.Range(1, 12)
            .Select(month =>
            {
                var firstDate = new DateTime(year, month, 1);
                var lastDate = firstDate.AddMonths(1).AddDays(-1);
                return new WorkCalendarMonthView(
                    month,
                    firstDate.ToString("MMMM yyyy", DisplayCulture),
                    firstDate,
                    lastDate);
            })
            .ToArray();

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }

    private sealed record WorkCalendarMonthView(
        int Month,
        string Title,
        DateTime FirstDate,
        DateTime LastDate);
}
