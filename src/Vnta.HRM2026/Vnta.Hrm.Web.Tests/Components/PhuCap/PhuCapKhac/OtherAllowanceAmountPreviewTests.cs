using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Components.PhuCap.PhuCapKhac;

public sealed class OtherAllowanceAmountPreviewTests
{
    [Theory]
    [InlineData(10.4, 10)]
    [InlineData(10.5, 11)]
    [InlineData(10.6, 11)]
    public void Preview_fixed_amount_matches_the_server_rounding_rule(decimal enteredAmount, decimal expectedAmount) =>
        Assert.Equal(expectedAmount, OtherAllowanceAmountPreview.Calculate(
            isFixedAmount: true,
            enteredAllowanceAmount: enteredAmount));

    [Fact]
    public void Preview_non_fixed_or_negative_amount_preserves_existing_ui_semantics()
    {
        Assert.Equal(0m, OtherAllowanceAmountPreview.Calculate(isFixedAmount: false, 10m));
        Assert.Equal(0m, OtherAllowanceAmountPreview.Calculate(isFixedAmount: true, -1m));
    }
}
