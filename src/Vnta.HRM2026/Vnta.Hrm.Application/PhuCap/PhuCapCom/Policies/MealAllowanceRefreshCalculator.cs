using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;

namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;

/// <summary>
/// Application service cho rule cần nguồn dữ liệu ngoài. Adapter nguồn công được inject,
/// còn việc xác định điều kiện và tiền vẫn nằm hoàn toàn trong <see cref="MealAllowancePolicy"/>.
/// </summary>
public sealed class MealAllowanceRefreshCalculator(IMealAllowanceWorkdaySource workdaySource)
    : IMealAllowanceRefreshCalculator
{
    public async Task<IReadOnlyDictionary<Guid, MealAllowanceCalculationResult>> CalculateAsync(
        MealAllowanceRefreshPeriod period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);

        var sourceRows = await workdaySource.LoadAsync(period, cancellationToken);
        var results = new Dictionary<Guid, MealAllowanceCalculationResult>();

        foreach(var employeeRows in sourceRows.GroupBy(row => row.EmployeeId))
        {
            var calculation = MealAllowancePolicy.Calculate(new MealAllowanceCalculationInput(
                employeeRows.Select(row => row.Workday).ToArray(),
                MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay));

            // Refresh cũ chỉ ghi nhận nhân viên có ít nhất một ngày đạt rule.
            if(calculation.QualifiedMealDays > 0)
            {
                results.Add(employeeRows.Key, calculation);
            }
        }

        return results;
    }
}
