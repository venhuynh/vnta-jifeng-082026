namespace Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;

public interface IBasicSalaryService
{
    Task<IReadOnlyList<BasicSalaryListItemDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<BasicSalaryListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BasicSalaryListItemDto>> SearchAsync(
        BasicSalaryFilter filter,
        CancellationToken cancellationToken = default);

    Task<string?> ValidateAsync(
        UpsertBasicSalaryRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<SyncBasicSalaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncBasicSalaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default);

    Task<BasicSalaryListItemDto> SaveAsync(
        UpsertBasicSalaryRecordRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
