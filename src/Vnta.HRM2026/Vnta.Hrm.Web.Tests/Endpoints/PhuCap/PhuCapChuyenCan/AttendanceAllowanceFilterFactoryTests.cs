using Vnta.Hrm.Application.PhuCap.Common;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class AttendanceAllowanceFilterFactoryTests
{
    [Fact]
    public void CreatePageFilter_preserves_snapshot_filters_and_calculates_skip()
    {
        var snapshot = new AttendanceAllowanceReloadSnapshot(
            PayrollMonth: 7,
            PayrollYear: 2026,
            SearchText: "NV001",
            LockState: AttendanceAllowanceLockState.Locked,
            AttendanceClass: "B",
            PageIndex: 2,
            PageSize: 50);

        var filter = new AttendanceAllowanceFilterFactory().CreatePageFilter(snapshot);

        Assert.Equal(PayrollAllowanceKind.Attendance, filter.AllowanceKind);
        Assert.Equal(7, filter.PayrollMonth);
        Assert.Equal(2026, filter.PayrollYear);
        Assert.Equal("NV001", filter.SearchText);
        Assert.Equal(AttendanceAllowanceLockState.Locked, filter.LockState);
        Assert.Equal("B", filter.AttendanceClass);
        Assert.Equal(100, filter.Skip);
        Assert.Equal(50, filter.Take);
    }
}
