namespace Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;

/// <summary>Đọc số công chuẩn theo kỳ lương cho các luồng tính phụ cấp nội bộ.</summary>
public interface IBasicSalaryWorkdaySource
{
    Task<IReadOnlyDictionary<Guid, decimal>> LoadStandardWorkingDaysAsync(
        int payrollYear,
        int payrollMonth,
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default);
}
