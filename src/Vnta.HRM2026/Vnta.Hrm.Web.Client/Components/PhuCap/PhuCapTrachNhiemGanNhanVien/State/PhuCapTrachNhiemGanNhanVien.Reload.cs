using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.State;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien;

public partial class PhuCapTrachNhiemGanNhanVien
{
    private async Task ReloadAsync()
    {
        if (!HasRequestedData || HasPendingPeriodChange || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
        CancelActiveReload();
        try
        {
            if (!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
            {
                return;
            }
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        try
        {
            LoadErrorMessage = null;
            IsLoading = true;
            await InvokeAsync(StateHasChanged);

            while (!disposalTokenSource.IsCancellationRequested
                   && !HasPendingPeriodChange
                   && ReloadLifecycleState.ProcessedVersion < Volatile.Read(ref ReloadLifecycleState.RequestedVersion))
            {
                var requestVersion = Volatile.Read(ref ReloadLifecycleState.RequestedVersion);
                ReloadLifecycleState.ProcessedVersion = requestVersion;
                await ReloadCoreAsync(requestVersion, CreateReloadSnapshot());
            }
        }
        finally
        {
            IsLoading = false;
            reloadGate.Release();
            if (!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task ReloadCoreAsync(int requestVersion, EmployeeAssignmentReloadSnapshot snapshot)
    {
        using var requestTokenSource = BeginReload();

        try
        {
            var page = await AssignmentProvider.SearchAsync(
                FilterFactory.CreatePageQuery(snapshot),
                requestTokenSource.Token);

            if (ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            if (page.TotalCount > 0)
            {
                var maximumPageIndex = Math.Max(0, (int)Math.Ceiling(page.TotalCount / (double)snapshot.PageSize) - 1);
                if (snapshot.PageIndex > maximumPageIndex)
                {
                    CurrentPageIndex = maximumPageIndex;
                    Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
                    return;
                }
            }

            ApplyPage(page);
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested && !ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                throw;
            }
        }
        catch (HrmApiException exception)
        {
            if (ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            ClearPage();
            LoadErrorMessage = exception.UserMessage;
            Logger.LogWarning(exception,
                "Không thể tải danh sách cấp bậc nhân viên kỳ {PayrollMonth}/{PayrollYear}. TraceId: {TraceId}",
                snapshot.PayrollMonth, snapshot.PayrollYear, exception.TraceId);
            ToastService.ShowError(LoadErrorMessage);
        }
        catch (Exception exception)
        {
            if (ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            ClearPage();
            LoadErrorMessage = "Không thể tải danh sách cấp bậc nhân viên. Vui lòng thử lại.";
            Logger.LogError(exception,
                "Không thể tải danh sách cấp bậc nhân viên kỳ {PayrollMonth}/{PayrollYear}.",
                snapshot.PayrollMonth, snapshot.PayrollYear);
            ToastService.ShowError(LoadErrorMessage);
        }
        finally
        {
            if (ReferenceEquals(ReloadLifecycleState.ActiveRequestTokenSource, requestTokenSource))
            {
                ReloadLifecycleState.ActiveRequestTokenSource = null;
            }
        }
    }

    private EmployeeAssignmentReloadSnapshot CreateReloadSnapshot() => new(
        AppliedYear,
        AppliedMonth,
        NormalizeOptional(SearchText),
        string.IsNullOrWhiteSpace(SelectedGradePresenceKey) ? null : SelectedGradePresenceKey,
        CurrentPageIndex,
        PageSize);

    private void ApplyPage(PayrollResponsibilityAllowanceEmployeeAssignmentPageDto page)
    {
        Records = page.Rows;
        Grades = page.ActiveGrades;
        TotalRecordCount = page.TotalCount;
        AssignmentSummary = page.Summary;
    }

    private void ClearPage()
    {
        Records = [];
        Grades = [];
        TotalRecordCount = 0;
        AssignmentSummary = new PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto(0, 0, 0);
    }

    private bool ShouldDiscardReloadResult(int requestVersion, EmployeeAssignmentReloadSnapshot snapshot) =>
        requestVersion != Volatile.Read(ref ReloadLifecycleState.RequestedVersion)
        || !HasRequestedData
        || HasPendingPeriodChange
        || snapshot != CreateReloadSnapshot();

    private CancellationTokenSource BeginReload()
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        ReloadLifecycleState.ActiveRequestTokenSource = cancellationTokenSource;
        return cancellationTokenSource;
    }

    private void CancelActiveReload() => ReloadLifecycleState.ActiveRequestTokenSource?.Cancel();
}
