using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Utils;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapDocHai;

public sealed class HazardAllowanceBrowserApiServiceRegistrationTests
{
    [Fact]
    public void Browser_api_registers_the_export_job_contract_with_the_hazard_http_adapter()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddBrowserApiServices();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<HttpHazardAllowanceService>(
            serviceProvider.GetRequiredService<IHazardAllowanceExportJobService>());
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
