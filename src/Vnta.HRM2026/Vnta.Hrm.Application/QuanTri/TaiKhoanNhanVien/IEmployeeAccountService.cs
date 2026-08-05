namespace Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;

public interface IEmployeeAccountService
{
    Task<IReadOnlyList<EmployeeAccountListItemDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<EmployeeAccountListItemDto> OpenAsync(
        OpenEmployeeAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeAccountListItemDto> ApproveAsync(
        ReviewEmployeeAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeAccountListItemDto> RejectAsync(
        ReviewEmployeeAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeAccountListItemDto> ResetPasswordAsync(
        ResetEmployeeAccountPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeAccountListItemDto> ActivateAsync(
        EmployeeAccountStateChangeRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeAccountListItemDto> DeactivateAsync(
        EmployeeAccountStateChangeRequest request,
        CancellationToken cancellationToken = default);
}
