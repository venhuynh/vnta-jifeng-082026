namespace Vnta.Hrm.Web.Client.Models;

// View model của một nhân viên; không dùng lại EF entity ở biên UI.
public sealed class MonthlyWorkSummaryGridRowRecord
{
    public Guid Id { get; init; }

    public int RowNumber { get; init; }

    public string EmployeeCode { get; init; } = "--";

    public string EmployeeName { get; init; } = "--";

    public string DepartmentName { get; init; } = "--";

    public string PositionName { get; init; } = "--";

    // DxGrid tạo field động theo ngày, nên DateOnly là identity ổn định của từng ô unbound.
    public Dictionary<DateOnly, MonthlyWorkSummaryDayCellRecord> DayCellsByDate { get; init; } = [];
}
