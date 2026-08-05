using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed record EmployeeTaxDependentLoadResult(
    IReadOnlyList<EmployeeTaxDependentListItemDto> Items,
    int TotalCount);

public sealed class EmployeeTaxDependentDataProvider(IEmployeeTaxDependentService service)
{
    public async Task<EmployeeTaxDependentLoadResult> SearchAsync(
        string? searchText,
        bool? isFamilyDeductionRegistered,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var page = await service.SearchAsync(
            new EmployeeTaxDependentFilter(searchText, isFamilyDeductionRegistered, skip, take),
            cancellationToken);
        return new EmployeeTaxDependentLoadResult(page.Items, page.TotalCount);
    }

    public Task<EmployeeTaxDependentDto> SaveAsync(
        SaveEmployeeTaxDependentRequest request,
        CancellationToken cancellationToken = default) =>
        service.SaveAsync(request, cancellationToken);
}
