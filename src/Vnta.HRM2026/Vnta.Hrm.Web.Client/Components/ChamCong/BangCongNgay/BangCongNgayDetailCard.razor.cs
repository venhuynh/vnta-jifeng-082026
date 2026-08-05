using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.BangCongNgay;

public partial class BangCongNgayDetailCard : IDisposable
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim logLoadGate = new(1, 1);

    private IReadOnlyList<AttendanceLogListItemDto> Logs { get; set; } = [];
    private IReadOnlyList<AttendanceStatusCodeRecord> StatusCodeOptions { get; set; } = [];
    private WorkdaySummaryEditModel? EditModel { get; set; }
    private Guid? LoadedSummaryId { get; set; }
    private Guid? LoadedEditorSummaryId { get; set; }
    private DateOnly? LoadedWorkDate { get; set; }
    private string? LogErrorMessage { get; set; }
    private string? SaveErrorMessage { get; set; }
    private bool IsLoadingLogs { get; set; }
    private bool IsSaving { get; set; }
    private bool AreStatusCodesLoaded { get; set; }
    private bool StatusCodeDropDownVisible { get; set; }
    private bool AllowCloseWhileSaving { get; set; }
    private Task? statusCodeLoadTask;
    private int logLoadVersion;
    private int processedLogLoadVersion;
    private CancellationTokenSource? logLoadTokenSource;

    [Inject]
    private IAttendanceLogReadService AttendanceLogReadService { get; set; } = default!;

    [Inject]
    private IAttendanceStatusCodeService AttendanceStatusCodeService { get; set; } = default!;

    [Inject]
    private IAttendanceWorkdaySummaryService AttendanceWorkdaySummaryService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private HrmOperationExecutor OperationExecutor { get; set; } = default!;

    [Parameter]
    public AttendanceWorkdaySummaryRecord? Summary { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public EventCallback<AttendanceWorkdaySummaryRecord> SummarySaved { get; set; }

    private bool IsReadOnlyLocked => Summary?.IsLocked == true;

    private bool CanEditFields => !IsSaving && EditModel is not null && !IsReadOnlyLocked;

    private bool CanSave => !IsSaving && EditModel is not null && !IsReadOnlyLocked;

    private bool CanEditLateEarlyMinutes =>
        CanEditFields && EditModel?.IsLateEarlyApplied == true;

    private bool CanEditOvertimeMinutes =>
        CanEditFields && EditModel?.IsOvertimeApplied == true;

    private string? LockedNoticeMessage => IsReadOnlyLocked
        ? "Dòng bảng công ngày đang khóa. Hãy mở khóa trước khi chỉnh sửa."
        : null;

    protected override async Task OnParametersSetAsync()
    {
        if (!Visible || Summary is null)
        {
            return;
        }

        InitializeEditModelIfNeeded();
        var canReuseLogs =
            LoadedSummaryId == Summary.Id
            && LoadedWorkDate == Summary.WorkDate
            && string.IsNullOrWhiteSpace(LogErrorMessage);

        await EnsureStatusCodesLoadedAsync();

        if (canReuseLogs)
        {
            return;
        }

        await LoadLogsAsync();
    }

    private void InitializeEditModelIfNeeded()
    {
        if (Summary is null)
        {
            return;
        }

        if (EditModel is not null && LoadedEditorSummaryId == Summary.Id)
        {
            return;
        }

        EditModel = WorkdaySummaryEditModel.FromRecord(Summary);
        LoadedEditorSummaryId = Summary.Id;
        SaveErrorMessage = null;
        StatusCodeDropDownVisible = false;
        AllowCloseWhileSaving = false;
    }

    private async Task EnsureStatusCodesLoadedAsync()
    {
        if (AreStatusCodesLoaded || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (statusCodeLoadTask is not null)
        {
            await statusCodeLoadTask;
            return;
        }

        statusCodeLoadTask = LoadStatusCodesCoreAsync();

        try
        {
            await statusCodeLoadTask;
        }
        finally
        {
            statusCodeLoadTask = null;
        }
    }

    private async Task LoadStatusCodesCoreAsync()
    {
        try
        {
            var outcome = await OperationExecutor.ExecuteAsync(
                cancellationToken => AttendanceStatusCodeService.GetAsync(cancellationToken),
                "Không thể tải danh sách kết quả chấm công.",
                disposalTokenSource.Token,
                showFailureToast: false);

            if (!outcome.Succeeded)
            {
                return;
            }

            var rows = outcome.Value ?? [];

            if (disposalTokenSource.IsCancellationRequested)
            {
                return;
            }

            StatusCodeOptions = rows.Select(MapStatusCodeRecord).ToList();
            AreStatusCodesLoaded = true;
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            StatusCodeOptions = [];
        }
    }

    private async Task LoadLogsAsync()
    {
        if (Summary is null || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref logLoadVersion);
        if (!await logLoadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
                   && processedLogLoadVersion < Volatile.Read(ref logLoadVersion))
            {
                processedLogLoadVersion = Volatile.Read(ref logLoadVersion);
                await LoadLogsCoreAsync();
            }
        }
        finally
        {
            logLoadGate.Release();
        }
    }

    private async Task LoadLogsCoreAsync()
    {
        if (Summary is null || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var summaryId = Summary.Id;
        var workDate = Summary.WorkDate;
        var employeeId = Summary.EmployeeId;
        var requestVersion = processedLogLoadVersion;
        var requestTokenSource = CreateLogLoadTokenSource();
        var requestToken = requestTokenSource.Token;

        Logs = [];
        LoadedSummaryId = null;
        LoadedWorkDate = null;
        LogErrorMessage = null;
        IsLoadingLogs = true;

        try
        {
            var workDateTime = workDate.ToDateTime(TimeOnly.MinValue);
            var outcome = await OperationExecutor.ExecuteAsync(
                cancellationToken => AttendanceLogReadService.SearchAsync(
                    new AttendanceLogFilter(
                        null,
                        workDateTime,
                        workDateTime,
                        employeeId,
                        Take: 5000),
                    cancellationToken),
                "Không thể tải lịch sử chấm công.",
                requestToken,
                showFailureToast: false);

            if (!outcome.Succeeded)
            {
                if (!requestToken.IsCancellationRequested
                    && requestVersion == Volatile.Read(ref logLoadVersion))
                {
                    Logs = [];
                    LogErrorMessage = outcome.Message ?? "Có lỗi khi tải lịch sử chấm công theo ngày công và nhân viên đã chọn.";
                }

                return;
            }

            var rows = outcome.Value ?? [];

            if (requestToken.IsCancellationRequested
                || requestVersion != Volatile.Read(ref logLoadVersion))
            {
                return;
            }

            Logs = rows;
            LoadedSummaryId = summaryId;
            LoadedWorkDate = workDate;
        }
        catch (OperationCanceledException) when (requestToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (requestToken.IsCancellationRequested
                || requestVersion != Volatile.Read(ref logLoadVersion))
            {
                return;
            }

            Logs = [];
            LogErrorMessage = "Có lỗi khi tải lịch sử chấm công theo ngày công và nhân viên đã chọn.";
            ToastService.ShowError("Không thể tải lịch sử chấm công.");
        }
        finally
        {
            if (!requestToken.IsCancellationRequested
                && requestVersion == Volatile.Read(ref logLoadVersion))
            {
                IsLoadingLogs = false;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (Summary is null || EditModel is null || IsSaving || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        SaveErrorMessage = ValidateEditModel(EditModel);
        if (!string.IsNullOrWhiteSpace(SaveErrorMessage))
        {
            return;
        }

        IsSaving = true;

        try
        {
            var request = BuildUpdateRequest(Summary.Id, EditModel);
            var outcome = await OperationExecutor.ExecuteAsync(
                cancellationToken => AttendanceWorkdaySummaryService.UpdateAsync(request, cancellationToken),
                "Không thể lưu điều chỉnh bảng công ngày.",
                disposalTokenSource.Token,
                showFailureToast: false);

            if (!outcome.Succeeded)
            {
                if (outcome.Status != HrmOperationStatus.Canceled)
                {
                    SaveErrorMessage = outcome.Message ?? "Không thể lưu điều chỉnh bảng công ngày.";
                    ToastService.ShowError(SaveErrorMessage);
                }

                return;
            }

            var updatedRow = outcome.Value!;
            var updatedSummary = MapRecord(updatedRow);

            EditModel = WorkdaySummaryEditModel.FromRecord(updatedSummary);
            LoadedEditorSummaryId = updatedSummary.Id;
            SaveErrorMessage = null;

            await SummarySaved.InvokeAsync(updatedSummary);
            ToastService.ShowSuccess($"Đã cập nhật dòng bảng công ngày của {updatedSummary.EmployeeDisplay}.");

            AllowCloseWhileSaving = true;
            IsSaving = false;
            await OnVisibleChanged(false);
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            SaveErrorMessage = ex.Message;
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            SaveErrorMessage = "Không thể lưu điều chỉnh bảng công ngày.";
            ToastService.ShowError("Không thể lưu điều chỉnh bảng công ngày.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private static string? ValidateEditModel(WorkdaySummaryEditModel model)
    {
        if (string.IsNullOrWhiteSpace(model.DayType))
        {
            return "Loại ngày công không được để trống.";
        }

        if (string.IsNullOrWhiteSpace(model.StatusCode))
        {
            return "Kết quả chấm công không được để trống.";
        }

        if (!IsValidTimeValue(model.CheckInAt))
        {
            return "Giờ vào phải đúng định dạng HH:mm.";
        }

        if (!IsValidTimeValue(model.CheckOutAt))
        {
            return "Giờ ra phải đúng định dạng HH:mm.";
        }

        if (model.LateEarlyTotalMinutes < 0)
        {
            return "Số phút đi trễ / về sớm không hợp lệ.";
        }

        if (model.IsLateEarlyApplied && model.LateEarlyTotalMinutes <= 0)
        {
            return "Hãy nhập số phút đi trễ / về sớm.";
        }

        if (model.OvertimeMinutes < 0)
        {
            return "Số phút tăng ca không hợp lệ.";
        }

        if (model.IsOvertimeApplied && model.OvertimeMinutes <= 0)
        {
            return "Hãy nhập số phút tăng ca.";
        }

        return null;
    }

    private static bool IsValidTimeValue(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if (normalizedValue is null)
        {
            return true;
        }

        return TimeOnly.TryParseExact(
            normalizedValue,
            ["HH:mm", "HH:mm:ss"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    private static UpdateAttendanceWorkdaySummaryRequest BuildUpdateRequest(
        Guid summaryId,
        WorkdaySummaryEditModel model)
    {
        var (lateMinutes, earlyLeaveMinutes) = ResolveLateEarlyMinutes(model);

        return new UpdateAttendanceWorkdaySummaryRequest(
            summaryId,
            model.DayType,
            NormalizeOptional(model.CheckInAt),
            NormalizeOptional(model.CheckOutAt),
            NormalizeOptional(model.StatusCode),
            lateMinutes,
            earlyLeaveMinutes,
            model.IsOvertimeApplied,
            model.IsOvertimeApplied ? model.OvertimeMinutes : 0,
            model.RequireDocument,
            NormalizeOptional(model.Note));
    }

    private static (int LateMinutes, int EarlyLeaveMinutes) ResolveLateEarlyMinutes(WorkdaySummaryEditModel model)
    {
        if (!model.IsLateEarlyApplied || model.LateEarlyTotalMinutes <= 0)
        {
            return (0, 0);
        }

        var originalTotal = Math.Max(0, model.OriginalLateMinutes) + Math.Max(0, model.OriginalEarlyLeaveMinutes);
        if (model.LateEarlyTotalMinutes == originalTotal)
        {
            return (Math.Max(0, model.OriginalLateMinutes), Math.Max(0, model.OriginalEarlyLeaveMinutes));
        }

        return (model.LateEarlyTotalMinutes, 0);
    }

    private Task OnStatusCodeValueChanged(object? value)
    {
        if (EditModel is null)
        {
            return Task.CompletedTask;
        }

        EditModel.StatusCode = NormalizeOptional(value as string);
        SyncEditModelWithSelectedStatusCode(EditModel);
        return Task.CompletedTask;
    }

    private Task OnStatusCodeDropDownVisibleChanged(bool visible)
    {
        StatusCodeDropDownVisible = visible;
        return Task.CompletedTask;
    }

    private Task OnStatusCodeLookupRowClick(GridRowClickEventArgs args)
    {
        if (args.Grid.GetDataItem(args.VisibleIndex) is AttendanceStatusCodeRecord statusCode)
        {
            SelectStatusCode(statusCode);
        }

        return Task.CompletedTask;
    }

    private void SelectStatusCode(AttendanceStatusCodeRecord statusCode)
    {
        if (EditModel is null)
        {
            return;
        }

        EditModel.StatusCode = NormalizeOptional(statusCode.Code);
        SyncEditModelWithSelectedStatusCode(EditModel);
        StatusCodeDropDownVisible = false;
    }

    private Task OnLateEarlyAppliedChanged(bool value)
    {
        if (EditModel is null)
        {
            return Task.CompletedTask;
        }

        if (!value)
        {
            EditModel.LastLateEarlyTotalMinutes = Math.Max(0, EditModel.LateEarlyTotalMinutes);
            EditModel.LateEarlyTotalMinutes = 0;
        }
        else if (EditModel.LateEarlyTotalMinutes <= 0)
        {
            EditModel.LateEarlyTotalMinutes = EditModel.LastLateEarlyTotalMinutes > 0
                ? EditModel.LastLateEarlyTotalMinutes
                : Math.Max(0, EditModel.OriginalLateMinutes + EditModel.OriginalEarlyLeaveMinutes);
        }

        EditModel.IsLateEarlyApplied = value;
        return Task.CompletedTask;
    }

    private Task OnOvertimeAppliedChanged(bool value)
    {
        if (EditModel is null)
        {
            return Task.CompletedTask;
        }

        if (!value)
        {
            EditModel.LastOvertimeMinutes = Math.Max(0, EditModel.OvertimeMinutes);
            EditModel.OvertimeMinutes = 0;
        }
        else if (EditModel.OvertimeMinutes <= 0)
        {
            EditModel.OvertimeMinutes = EditModel.LastOvertimeMinutes > 0
                ? EditModel.LastOvertimeMinutes
                : Math.Max(0, EditModel.OriginalOvertimeMinutes);
        }

        EditModel.IsOvertimeApplied = value;
        return Task.CompletedTask;
    }

    private void SyncEditModelWithSelectedStatusCode(WorkdaySummaryEditModel model)
    {
        var selectedStatusCode = ResolveSelectedStatusCode(model.StatusCode);
        if (selectedStatusCode is null)
        {
            return;
        }

        if (selectedStatusCode.CongTangCa && !model.IsOvertimeApplied)
        {
            model.IsOvertimeApplied = true;
            if (model.OvertimeMinutes <= 0)
            {
                model.OvertimeMinutes = model.LastOvertimeMinutes > 0
                    ? model.LastOvertimeMinutes
                    : Math.Max(0, model.OriginalOvertimeMinutes);
            }
        }

        if (string.Equals(selectedStatusCode.Code, "LATE_EARLY", StringComparison.OrdinalIgnoreCase)
            && !model.IsLateEarlyApplied)
        {
            model.IsLateEarlyApplied = true;
            if (model.LateEarlyTotalMinutes <= 0)
            {
                model.LateEarlyTotalMinutes = model.LastLateEarlyTotalMinutes > 0
                    ? model.LastLateEarlyTotalMinutes
                    : Math.Max(0, model.OriginalLateMinutes + model.OriginalEarlyLeaveMinutes);
            }
        }
    }

    private async Task OnVisibleChanged(bool visible)
    {
        if (!visible)
        {
            if (IsSaving && !AllowCloseWhileSaving)
            {
                await VisibleChanged.InvokeAsync(true);
                return;
            }

            AllowCloseWhileSaving = false;
            ResetPopupState();
        }

        await VisibleChanged.InvokeAsync(visible);
    }

    private Task CloseAsync()
    {
        if (IsSaving)
        {
            return Task.CompletedTask;
        }

        return OnVisibleChanged(false);
    }

    private CancellationTokenSource CreateLogLoadTokenSource()
    {
        var nextTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        var previousTokenSource = Interlocked.Exchange(ref logLoadTokenSource, nextTokenSource);

        if (previousTokenSource is not null)
        {
            CancelAndDisposeTokenSource(previousTokenSource);
        }

        return nextTokenSource;
    }

    private void ResetPopupState()
    {
        ResetLogState();
        ResetEditState();
    }

    private void ResetLogState()
    {
        var previousTokenSource = Interlocked.Exchange(ref logLoadTokenSource, null);
        if (previousTokenSource is not null)
        {
            CancelAndDisposeTokenSource(previousTokenSource);
        }

        Logs = [];
        LoadedSummaryId = null;
        LoadedWorkDate = null;
        LogErrorMessage = null;
        IsLoadingLogs = false;
        logLoadVersion = 0;
        processedLogLoadVersion = 0;
    }

    private void ResetEditState()
    {
        EditModel = null;
        LoadedEditorSummaryId = null;
        SaveErrorMessage = null;
        StatusCodeDropDownVisible = false;
        AllowCloseWhileSaving = false;
        IsSaving = false;
    }

    private static void CancelAndDisposeTokenSource(CancellationTokenSource tokenSource)
    {
        try
        {
            tokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        tokenSource.Dispose();
    }

    private string GetStatusCodeDisplayText(DropDownBoxQueryDisplayTextContext context)
    {
        var code = context.Value as string ?? EditModel?.StatusCode ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var statusCode = ResolveSelectedStatusCode(code);
        return statusCode is null ? code.Trim() : BuildStatusCodeDisplayText(statusCode);
    }

    private AttendanceStatusCodeRecord? ResolveSelectedStatusCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return StatusCodeOptions.FirstOrDefault(item =>
            string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private string GetOvertimeMinutesCaption()
    {
        var dayType = EditModel?.DayType;
        if (string.IsNullOrWhiteSpace(dayType))
        {
            return "Số phút tăng ca";
        }

        return dayType switch
        {
            AttendanceWorkCalendarDayTypes.DayOff => "Số phút X2",
            AttendanceWorkCalendarDayTypes.Holiday => "Số phút X3",
            _ => "Số phút X1.5"
        };
    }

    private static string BuildStatusCodeDisplayText(AttendanceStatusCodeRecord statusCode) =>
        $"{statusCode.Code} - {statusCode.Name}";

    private static int GetEffectiveOvertimeMinutes(AttendanceWorkdaySummaryRecord summary) => summary.DayTypeDisplay switch
    {
        AttendanceWorkCalendarDayTypes.DayOff => Math.Max(0, summary.OvertimeMinutes20),
        AttendanceWorkCalendarDayTypes.Holiday => Math.Max(0, summary.OvertimeMinutes30),
        _ => Math.Max(0, summary.OvertimeMinutes15 > 0 ? summary.OvertimeMinutes15 : summary.OvertimeMinutes)
    };

    private static AttendanceStatusCodeRecord MapStatusCodeRecord(AttendanceStatusCodeListItemDto row) =>
        new()
        {
            Id = row.Id,
            Code = row.Code,
            Name = row.Name,
            Kind = row.Kind,
            CongTangCa = row.CongTangCa,
            CongHanhChinh = row.CongHanhChinh,
            PhuCapTrachNhiemTinhNangSuat = row.PhuCapTrachNhiemTinhNangSuat,
            PhuCapDocHai = row.PhuCapDocHai,
            PhuCapTrachNhiemKhac = row.PhuCapTrachNhiemKhac,
            PhuCapPhepLe = row.PhuCapPhepLe,
            PhuCapTrachNhiemKhongTinhNangSuat = row.PhuCapTrachNhiemKhongTinhNangSuat,
            PhuCapThamNien = row.PhuCapThamNien,
            KhauTruTamUng = row.KhauTruTamUng,
            IsActive = row.IsActive,
            Note = row.Note,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc
        };

    private static AttendanceWorkdaySummaryRecord MapRecord(AttendanceWorkdaySummaryListItemDto row) =>
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

    private static string BuildDetailSubtitle(AttendanceWorkdaySummaryRecord summary)
    {
        var workDate = FormatDate(summary.WorkDate);
        return $"{workDate} | {summary.DayTypeDisplay}";
    }

    private static string FormatDateTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        var displayValue = value.Value.Kind == DateTimeKind.Utc
            ? value.Value.ToLocalTime()
            : value.Value;

        return displayValue.ToString("HH:mm:ss", DisplayCulture);
    }

    private static string FormatDate(DateOnly value) => value.ToString("dd/MM/yyyy", DisplayCulture);

    private static string FormatVerifyMode(string? verify) => verify switch
    {
        "1" => "Vân tay",
        "3" => "Thẻ",
        "15" => "Khuôn mặt",
        "0" => "Mật khẩu",
        _ => string.IsNullOrWhiteSpace(verify) ? "Không xác định" : $"Mã {verify}"
    };

    private static string FormatStatusCodeKind(string? kind) => (kind ?? string.Empty).Trim().ToUpperInvariant() switch
    {
        "WORK" => "Ngày công",
        "LEAVE" => "Nghỉ",
        _ => string.IsNullOrWhiteSpace(kind) ? "--" : kind.Trim()
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        ResetPopupState();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        logLoadGate.Dispose();
    }

    private sealed class WorkdaySummaryEditModel
    {
        public string DayType { get; set; } = string.Empty;

        public string? CheckInAt { get; set; }

        public string? CheckOutAt { get; set; }

        public string? StatusCode { get; set; }

        public bool IsLateEarlyApplied { get; set; }

        public int LateEarlyTotalMinutes { get; set; }

        public int OriginalLateMinutes { get; set; }

        public int OriginalEarlyLeaveMinutes { get; set; }

        public int LastLateEarlyTotalMinutes { get; set; }

        public bool IsOvertimeApplied { get; set; }

        public int OvertimeMinutes { get; set; }

        public int OriginalOvertimeMinutes { get; set; }

        public int LastOvertimeMinutes { get; set; }

        public bool RequireDocument { get; set; }

        public string? Note { get; set; }

        public static WorkdaySummaryEditModel FromRecord(AttendanceWorkdaySummaryRecord summary) =>
            new()
            {
                DayType = summary.DayTypeDisplay,
                CheckInAt = BangCongNgayDetailCard.NormalizeOptional(summary.CheckInDisplay),
                CheckOutAt = BangCongNgayDetailCard.NormalizeOptional(summary.CheckOutDisplay),
                StatusCode = BangCongNgayDetailCard.NormalizeOptional(summary.Status),
                IsLateEarlyApplied = summary.LateEarlyTotalMinutes > 0
                    || string.Equals(summary.Status, "LATE_EARLY", StringComparison.OrdinalIgnoreCase),
                LateEarlyTotalMinutes = summary.LateEarlyTotalMinutes,
                OriginalLateMinutes = Math.Max(0, summary.LateMinutes),
                OriginalEarlyLeaveMinutes = Math.Max(0, summary.EarlyLeaveMinutes),
                LastLateEarlyTotalMinutes = summary.LateEarlyTotalMinutes,
                IsOvertimeApplied = summary.IsRegisterForOT || BangCongNgayDetailCard.GetEffectiveOvertimeMinutes(summary) > 0,
                OvertimeMinutes = BangCongNgayDetailCard.GetEffectiveOvertimeMinutes(summary),
                OriginalOvertimeMinutes = BangCongNgayDetailCard.GetEffectiveOvertimeMinutes(summary),
                LastOvertimeMinutes = BangCongNgayDetailCard.GetEffectiveOvertimeMinutes(summary),
                RequireDocument = summary.RequireDocument,
                Note = BangCongNgayDetailCard.NormalizeOptional(summary.Note)
            };
    }
}
