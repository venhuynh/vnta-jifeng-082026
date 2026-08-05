using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT.Models;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.KhauTru.KhauTruBHXHYT;

public sealed class PayrollInsuranceDeductionRecordTests
{
    [Fact]
    public void Client_preview_uses_the_server_whole_vnd_rounding_contract()
    {
        var record = new PayrollInsuranceDeductionRecord
        {
            InsuranceSalaryBaseAmount = 12_345.67m,
            SocialInsuranceRate = .08m,
            HealthInsuranceRate = .015m,
            UnemploymentInsuranceRate = .01m,
            IsParticipating = true
        };

        record.RecalculateDerivedValues();

        Assert.Equal(988m, record.SocialInsuranceAmount);
        Assert.Equal(185m, record.HealthInsuranceAmount);
        Assert.Equal(123m, record.UnemploymentInsuranceAmount);
        Assert.Equal(1_296m, record.TotalDeductionAmount);
    }
}
