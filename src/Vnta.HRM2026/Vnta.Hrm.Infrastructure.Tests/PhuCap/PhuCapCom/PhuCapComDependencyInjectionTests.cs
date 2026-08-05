using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.DependencyInjection;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapCom;

public sealed class PhuCapComDependencyInjectionTests
{
    [Fact]
    public void Read_and_export_capabilities_resolve_to_the_same_scoped_implementation()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"phu-cap-com-di-{Guid.NewGuid():N}"));
        services.AddPhuCapCom();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var readService = scope.ServiceProvider.GetRequiredService<IMealAllowanceReadService>();
        var exportService = scope.ServiceProvider.GetRequiredService<IMealAllowanceExportService>();

        Assert.Same(readService, exportService);
    }
}
