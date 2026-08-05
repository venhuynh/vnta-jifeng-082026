using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruBHXHYT;

public sealed class PayrollInsuranceDeductionRequestValidatorTests
{
    [Fact]
    public void Rejects_rates_that_exceed_the_database_total_rate_constraint()
    {
        var result = PayrollInsuranceDeductionRequestValidator.Validate(
            new PayrollInsuranceDeductionValidationInput(10_000m, .8m, .15m, .1m, 0),
            "Salary base cannot be negative.");

        Assert.Equal("Tổng tỷ lệ BHXH, BHYT và BHTN không được vượt quá 100%.", result);
    }

    [Fact]
    public void Accepts_rates_at_the_database_total_rate_boundary()
    {
        var result = PayrollInsuranceDeductionRequestValidator.Validate(
            new PayrollInsuranceDeductionValidationInput(10_000m, .8m, .15m, .05m, 0),
            "Salary base cannot be negative.");

        Assert.Null(result);
    }

    [Fact]
    public void Rejects_negative_salary_base_before_rate_checks()
    {
        var result = PayrollInsuranceDeductionRequestValidator.Validate(
            new PayrollInsuranceDeductionValidationInput(-.01m, .08m, .015m, .01m, 0),
            "Salary base cannot be negative.");

        Assert.Equal("Salary base cannot be negative.", result);
    }

    [Fact]
    public void Rejects_invalid_participation_change_type_at_boundary()
    {
        var result = PayrollInsuranceDeductionRequestValidator.Validate(
            new PayrollInsuranceDeductionValidationInput(10_000m, .08m, .015m, .01m, 4),
            "Salary base cannot be negative.");

        Assert.Equal("Loại biến động tham gia không hợp lệ.", result);
    }
}
