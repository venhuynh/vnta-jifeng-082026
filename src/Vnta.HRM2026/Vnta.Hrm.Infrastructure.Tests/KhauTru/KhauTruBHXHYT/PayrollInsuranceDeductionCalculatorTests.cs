using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruBHXHYT;

public sealed class PayrollInsuranceDeductionCalculatorTests
{
    [Fact]
    public void Calculates_each_component_with_away_from_zero_rounding()
    {
        var result = PayrollInsuranceDeductionCalculator.Calculate(new PayrollInsuranceDeductionCalculationInput(
            100.5m,
            PayrollInsuranceDeductionStandardRates.SocialInsurance,
            PayrollInsuranceDeductionStandardRates.HealthInsurance,
            PayrollInsuranceDeductionStandardRates.UnemploymentInsurance,
            InsuranceParticipationStatus.Participating));

        Assert.Equal(PayrollInsuranceDeductionStandardRates.Total, result.TotalInsuranceRate);
        Assert.Equal(8m, result.SocialInsuranceAmount);
        Assert.Equal(2m, result.HealthInsuranceAmount);
        Assert.Equal(1m, result.UnemploymentInsuranceAmount);
        Assert.Equal(11m, result.TotalDeductionAmount);
    }

    [Fact]
    public void Non_participating_employee_has_zero_amounts_and_rate()
    {
        var result = PayrollInsuranceDeductionCalculator.Calculate(new PayrollInsuranceDeductionCalculationInput(
            10_000_000m,
            PayrollInsuranceDeductionStandardRates.SocialInsurance,
            PayrollInsuranceDeductionStandardRates.HealthInsurance,
            PayrollInsuranceDeductionStandardRates.UnemploymentInsurance,
            InsuranceParticipationStatus.NotParticipating));

        Assert.Equal(0m, result.TotalInsuranceRate);
        Assert.Equal(0m, result.TotalDeductionAmount);
    }

    [Fact]
    public void Rounds_half_vnd_away_from_zero_for_each_component_before_summing()
    {
        var result = PayrollInsuranceDeductionCalculator.Calculate(new PayrollInsuranceDeductionCalculationInput(
            6.25m,
            PayrollInsuranceDeductionStandardRates.SocialInsurance,
            PayrollInsuranceDeductionStandardRates.HealthInsurance,
            PayrollInsuranceDeductionStandardRates.UnemploymentInsurance,
            InsuranceParticipationStatus.Participating));

        Assert.Equal(1m, result.SocialInsuranceAmount);
        Assert.Equal(0m, result.HealthInsuranceAmount);
        Assert.Equal(0m, result.UnemploymentInsuranceAmount);
        Assert.Equal(1m, result.TotalDeductionAmount);
    }

    [Fact]
    public void Preserves_zero_base_and_standard_ten_point_five_percent_rates()
    {
        var result = PayrollInsuranceDeductionCalculator.Calculate(new PayrollInsuranceDeductionCalculationInput(
            0m,
            PayrollInsuranceDeductionStandardRates.SocialInsurance,
            PayrollInsuranceDeductionStandardRates.HealthInsurance,
            PayrollInsuranceDeductionStandardRates.UnemploymentInsurance,
            InsuranceParticipationStatus.Participating));

        Assert.Equal(PayrollInsuranceDeductionStandardRates.Total, result.TotalInsuranceRate);
        Assert.Equal(0m, result.TotalDeductionAmount);
    }
}
