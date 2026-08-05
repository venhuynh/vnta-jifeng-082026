using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Web.Client.Services.DataProviders;

namespace Vnta.Hrm.Web.Client.Services;

internal static class HazardAllowanceDataProviderServiceExtensions
{
    internal static IServiceCollection AddHazardAllowanceDataProvider(this IServiceCollection services)
    {
        services.AddScoped<HazardAllowanceDataProvider>();
        return services;
    }
}
