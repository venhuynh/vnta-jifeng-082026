using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.AuditTrail;

public partial class AuditTrail : IDisposable
{
    private const int PageSize = 50;
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly CancellationTokenSource _disposalTokenSource = new();
    private readonly List<AuditEventCursor?> _pageCursors = [null];

    [Inject]
    private IAuditTrailQueryService AuditTrailQueryService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IReadOnlyList<AuditEventListItemDto> Events { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private AuditEventCursor? NextCursor { get; set; }
    private int CurrentPageIndex { get; set; }
    private DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-6);
    private DateTime? ToDate { get; set; } = DateTime.Today;
    private string? ActorId { get; set; }
    private string? Action { get; set; }
    private string? EntityType { get; set; }
    private string? EntityId { get; set; }
    private string? CorrelationId { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; }
    private bool IsDetailPopupVisible { get; set; }
    private bool IsDetailLoading { get; set; }
    private AuditEventDetailDto? SelectedDetail { get; set; }

    private bool CanQuery => !_disposalTokenSource.IsCancellationRequested && !IsLoading;
    private bool CanViewDetail => CanQuery && GetSelectedEvent() is not null;
    private bool CanGoPrevious => CanQuery && CurrentPageIndex > 0;
    private bool CanGoNext => CanQuery && NextCursor is not null;
    private int CurrentPageNumber => CurrentPageIndex + 1;

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentPageAsync(showLoading: true);
        await base.OnInitializedAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        ResetPaging();
        await LoadCurrentPageAsync(showLoading: true);
    }

    private async Task ResetFiltersAsync()
    {
        FromDate = DateTime.Today.AddDays(-6);
        ToDate = DateTime.Today;
        ActorId = null;
        Action = null;
        EntityType = null;
        EntityId = null;
        CorrelationId = null;
        ResetPaging();
        await LoadCurrentPageAsync(showLoading: true);
    }

    private Task RefreshAsync() => LoadCurrentPageAsync(showLoading: true);

    private async Task NextPageAsync()
    {
        if (NextCursor is null)
        {
            return;
        }

        if (_pageCursors.Count == CurrentPageIndex + 1)
        {
            _pageCursors.Add(NextCursor);
        }

        CurrentPageIndex++;
        await LoadCurrentPageAsync(showLoading: true);
    }

    private async Task PreviousPageAsync()
    {
        if (CurrentPageIndex == 0)
        {
            return;
        }

        CurrentPageIndex--;
        await LoadCurrentPageAsync(showLoading: true);
    }

    private async Task LoadCurrentPageAsync(bool showLoading)
    {
        if (_disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        LoadErrorMessage = null;
        if (showLoading)
        {
            IsLoading = true;
        }

        try
        {
            var access = await GetReadAccessAsync();
            var page = await AuditTrailQueryService.GetPageAsync(
                BuildFilter(_pageCursors[CurrentPageIndex]),
                access,
                _disposalTokenSource.Token);

            Events = page.Items;
            NextCursor = page.NextCursor;
            await ClearSelectionAsync();
        }
        catch (OperationCanceledException) when (_disposalTokenSource.IsCancellationRequested)
        {
            // Disposal cancels pending Interactive Server work.
        }
        catch (Exception ex)
        {
            Events = [];
            NextCursor = null;
            LoadErrorMessage = ex is ArgumentException or ArgumentOutOfRangeException
                ? ex.Message
                : "Có lỗi khi tải nhật ký kiểm toán. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải nhật ký kiểm toán.");
        }
        finally
        {
            if (showLoading)
            {
                IsLoading = false;
            }
        }
    }

    private async Task OpenSelectedDetailAsync()
    {
        var selectedEvent = GetSelectedEvent();
        if (selectedEvent is not null)
        {
            await OpenDetailAsync(selectedEvent.Id);
        }
    }

    private async Task OpenDetailAsync(Guid id)
    {
        if (_disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsDetailPopupVisible = true;
        IsDetailLoading = true;
        SelectedDetail = null;

        try
        {
            SelectedDetail = await AuditTrailQueryService.GetDetailAsync(
                id,
                await GetReadAccessAsync(),
                _disposalTokenSource.Token);
        }
        catch (OperationCanceledException) when (_disposalTokenSource.IsCancellationRequested)
        {
            // Disposal cancels pending Interactive Server work.
        }
        catch
        {
            ToastService.ShowError("Không thể tải chi tiết sự kiện audit.");
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    private Task OnDetailPopupVisibleChangedAsync(bool visible)
    {
        IsDetailPopupVisible = visible;
        if (!visible)
        {
            SelectedDetail = null;
        }

        return Task.CompletedTask;
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private AuditEventFilter BuildFilter(AuditEventCursor? cursor) => new(
        FromUtc: ToUtcStart(FromDate),
        ToUtc: ToUtcEnd(ToDate),
        ActorId: ActorId,
        Action: Action,
        EntityType: EntityType,
        EntityId: EntityId,
        CorrelationId: CorrelationId,
        Cursor: cursor,
        PageSize: PageSize);

    private async Task<AuditReadAccess> GetReadAccessAsync()
    {
        var authenticationState = await AuthenticationStateProvider
            .GetAuthenticationStateAsync()
            .ConfigureAwait(false);

        return new AuditReadAccess(
            InternalAccountCapabilityResolver.HasCapability(
                authenticationState.User,
                InternalAccountCapabilities.AuditSensitiveRead));
    }

    private void ResetPaging()
    {
        _pageCursors.Clear();
        _pageCursors.Add(null);
        CurrentPageIndex = 0;
        NextCursor = null;
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        if (Grid is not null)
        {
            await Grid.DeselectAllAsync();
            Grid.SetFocusedRowIndex(-1);
        }
    }

    private AuditEventListItemDto? GetSelectedEvent() =>
        SelectedDataItems.OfType<AuditEventListItemDto>().SingleOrDefault();

    private static DateTimeOffset? ToUtcStart(DateTime? value) =>
        value is { } date
            ? new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Local)).ToUniversalTime()
            : null;

    private static DateTimeOffset? ToUtcEnd(DateTime? value) =>
        value is { } date
            ? new DateTimeOffset(DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local)).ToUniversalTime()
            : null;

    private static string FormatOccurredAt(DateTimeOffset occurredAtUtc) =>
        occurredAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss", DisplayCulture);

    private static string FormatAuditValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    public void Dispose()
    {
        _disposalTokenSource.Cancel();
        _disposalTokenSource.Dispose();
    }
}
