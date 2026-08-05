namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public interface INhanVienDeleteService
{
    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
