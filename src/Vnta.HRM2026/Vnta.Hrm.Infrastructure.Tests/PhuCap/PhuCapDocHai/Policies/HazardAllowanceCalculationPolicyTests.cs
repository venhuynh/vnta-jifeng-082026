using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapDocHai.Policies;

public sealed class HazardAllowanceCalculationPolicyTests
{
    private readonly HazardAllowanceCalculationPolicy policy = new();

    [Fact]
    public void Calculate_multiplies_payable_workdays_by_the_fixed_rate_without_rounding()
    {
        var result = policy.Calculate(new HazardAllowanceCalculationInput(
            "Xưởng / Sản xuất",
            25m,
            0.0625m));

        Assert.True(result.IsEligibleDepartment);
        Assert.Equal(24.9375m, result.PayableWorkdayCount);
        Assert.Equal(7_700m, result.HazardAllowancePerDay);
        Assert.Equal(192_018.75m, result.HazardAllowanceAmount);
    }

    [Theory]
    [InlineData("Văn phòng")]
    [InlineData("Khối Văn Phòng / Ban Giám Đốc")]
    public void Calculate_excludes_office_department_before_user_entitlement(string departmentPath)
    {
        var result = policy.Calculate(new HazardAllowanceCalculationInput(
            departmentPath,
            26m,
            0m,
            IsEligibleForAllowance: true));

        Assert.False(result.IsEligibleDepartment);
        Assert.False(result.IsEligibleForAllowance);
        Assert.Equal(0m, result.HazardAllowanceAmount);
        Assert.NotNull(result.ExclusionReason);
    }

    [Theory]
    [InlineData("Sản xuất / Phòng kế toán / Kho vật tư")]
    [InlineData("Khối Sản Xuất / P. Kế Toán / Kho Vật Tư")]
    public void Calculate_excludes_accounting_materials_warehouse_path(string departmentPath)
    {
        var result = policy.Calculate(new HazardAllowanceCalculationInput(
            departmentPath,
            26m,
            0m,
            IsEligibleForAllowance: true,
            PositionName: "Nhân viên"));

        Assert.False(result.IsEligibleDepartment);
        Assert.False(result.IsEligibleForAllowance);
        Assert.Equal(0m, result.HazardAllowanceAmount);
        Assert.NotNull(result.ExclusionReason);
    }

    [Fact]
    public void Calculate_excludes_any_position_containing_temporary_worker_text()
    {
        var result = policy.Calculate(new HazardAllowanceCalculationInput(
            "Xưởng / Sản xuất",
            26m,
            0m,
            IsEligibleForAllowance: true,
            PositionName: "Nhân viên thời vụ kho"));

        Assert.False(result.IsEligibleDepartment);
        Assert.False(result.IsEligibleForAllowance);
        Assert.Equal(0m, result.HazardAllowanceAmount);
        Assert.NotNull(result.ExclusionReason);
    }

    [Fact]
    public void Calculate_keeps_the_fixed_rate_when_payable_workdays_are_low()
    {
        var result = policy.Calculate(new HazardAllowanceCalculationInput(
            "Xưởng",
            1m,
            0m));

        Assert.Equal(7_700m, result.HazardAllowanceAmount);
    }

    [Fact]
    public void Calculate_excludes_employee_when_department_path_is_missing()
    {
        var result = policy.Calculate(new HazardAllowanceCalculationInput(
            "  ",
            26m,
            0m));

        Assert.False(result.IsEligibleDepartment);
        Assert.Equal(0m, result.HazardAllowanceAmount);
        Assert.NotNull(result.ExclusionReason);
    }

    [Fact]
    public void Calculate_clamps_payable_workdays_to_zero_when_deduction_exceeds_qualified_workdays()
    {
        var result = policy.Calculate(new HazardAllowanceCalculationInput(
            "Xưởng",
            1m,
            1.125m));

        Assert.Equal(0m, result.PayableWorkdayCount);
        Assert.Equal(0m, result.HazardAllowanceAmount);
    }
}
