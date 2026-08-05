using Vnta.Hrm.Application.ChamCong.BangCongThang;
using Vnta.Hrm.Web.Client.Models.Attendance;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class MonthlyWorkSummaryDataProviderTests
{
    [Fact]
    public async Task Load_month_forwards_the_requested_server_page_without_loading_the_full_month()
    {
        var readService = new CapturingMonthlyWorkReadService(totalCount: 317);
        var provider = new MonthlyWorkSummaryDataProvider(readService);

        var result = await provider.LoadPageAsync(
            new MonthlyWorkSummaryPageRequest(
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 7, 31),
                SearchText: null,
                Skip: 100,
                Take: 50));

        Assert.Equal(317, result.TotalCount);
        Assert.NotNull(readService.ReceivedFilter);
        Assert.Equal(100, readService.ReceivedFilter!.Skip);
        Assert.Equal(50, readService.ReceivedFilter.Take);
        Assert.Null(readService.ReceivedFilter.SearchText);
        Assert.Null(readService.ReceivedFilter.EmployeeId);
        Assert.False(readService.ReceivedFilter.IncludeShiftDetails);
    }

    [Fact]
    public async Task Load_month_uses_the_bounded_default_page_size_when_the_ui_does_not_provide_one()
    {
        var readService = new CapturingMonthlyWorkReadService(totalCount: 0);
        var provider = new MonthlyWorkSummaryDataProvider(readService);

        await provider.LoadPageAsync(
            new MonthlyWorkSummaryPageRequest(
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 7, 31),
                SearchText: null,
                Skip: 0,
                Take: 0));

        Assert.NotNull(readService.ReceivedFilter);
        Assert.Equal(50, readService.ReceivedFilter!.Take);
    }

    [Fact]
    public async Task Load_month_forwards_normalized_search_text_to_the_server_filter()
    {
        var readService = new CapturingMonthlyWorkReadService(totalCount: 0);
        var provider = new MonthlyWorkSummaryDataProvider(readService);

        await provider.LoadPageAsync(
            new MonthlyWorkSummaryPageRequest(
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 7, 31),
                SearchText: "  Nguyễn Văn A  ",
                Skip: 0,
                Take: 50));

        Assert.NotNull(readService.ReceivedFilter);
        Assert.Equal("Nguyễn Văn A", readService.ReceivedFilter!.SearchText);
    }

    [Fact]
    public async Task Load_employee_month_keeps_shift_details_for_detail_consumers()
    {
        var readService = new CapturingMonthlyWorkReadService(totalCount: 0);
        var provider = new MonthlyWorkSummaryDataProvider(readService);

        await provider.LoadEmployeeMonthAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            Guid.NewGuid());

        Assert.NotNull(readService.ReceivedFilter);
        Assert.True(readService.ReceivedFilter!.IncludeShiftDetails);
    }

    [Fact]
    public async Task Load_page_does_not_map_shift_details_for_monthly_grid()
    {
        var dayCell = new AttendanceMonthlyWorkSummaryDayCellDto(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 1),
            "regular",
            "HC",
            "Hành chính",
            "Ca hành chính",
            "#FFFFFF",
            "08:00",
            "17:00",
            0,
            0,
            "FULL_WORK",
            false,
            0,
            0,
            0,
            0,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null);
        var row = new AttendanceMonthlyWorkSummaryGridRowDto(
            Guid.NewGuid(),
            1,
            "NV001",
            "Nguyễn Văn A",
            "Phòng Nhân sự",
            "Chuyên viên",
            [dayCell]);
        var readService = new CapturingMonthlyWorkReadService(totalCount: 1, rows: [row]);
        var provider = new MonthlyWorkSummaryDataProvider(readService);

        var result = await provider.LoadPageAsync(
            new MonthlyWorkSummaryPageRequest(
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 7, 31),
                SearchText: null,
                Skip: 0,
                Take: 50));

        var mappedDayCell = Assert.Single(Assert.Single(result.Rows).DayCellsByDate.Values);
        Assert.Null(mappedDayCell.ShiftCode);
        Assert.Null(mappedDayCell.ShiftShortName);
        Assert.Null(mappedDayCell.ShiftName);
        Assert.Null(mappedDayCell.ShiftColorHex);
    }

    private sealed class CapturingMonthlyWorkReadService(
        int totalCount,
        IReadOnlyList<AttendanceMonthlyWorkSummaryGridRowDto>? rows = null)
        : IAttendanceMonthlyWorkSummaryGridReadService
    {
        public AttendanceMonthlyWorkSummaryGridFilter? ReceivedFilter { get; private set; }

        public Task<AttendanceMonthlyWorkSummaryGridPageDto> SearchAsync(
            AttendanceMonthlyWorkSummaryGridFilter filter,
            CancellationToken cancellationToken = default)
        {
            ReceivedFilter = filter;
            return Task.FromResult(new AttendanceMonthlyWorkSummaryGridPageDto(rows ?? [], totalCount));
        }
    }
}
