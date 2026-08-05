namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;

/// <summary>Đọc dữ kiện công cần thiết cho phép tính phụ cấp cơm.</summary>
public interface IMealAllowanceWorkdaySource
{
    Task<IReadOnlyList<MealAllowanceEmployeeWorkday>> LoadAsync(
        MealAllowanceRefreshPeriod period,
        CancellationToken cancellationToken = default);
}

/// <summary>Điều phối đọc dữ liệu công và chạy policy cho kỳ phụ cấp cơm.</summary>
public interface IMealAllowanceRefreshCalculator
{
    Task<IReadOnlyDictionary<Guid, MealAllowanceCalculationResult>> CalculateAsync(
        MealAllowanceRefreshPeriod period,
        CancellationToken cancellationToken = default);
}

/// <summary>Phạm vi dữ liệu công được phép dùng để làm mới snapshot.</summary>
public sealed record MealAllowanceRefreshPeriod(int PayrollMonth, int PayrollYear, Guid? EmployeeId);

/// <summary>Một dòng công gắn với nhân viên, không phụ thuộc persistence model.</summary>
public sealed record MealAllowanceEmployeeWorkday(Guid EmployeeId, MealAllowanceWorkday Workday);
