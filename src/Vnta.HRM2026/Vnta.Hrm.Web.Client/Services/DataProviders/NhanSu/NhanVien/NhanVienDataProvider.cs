using Vnta.Hrm.Web.Client.Models.Employees;
using Vnta.Hrm.Web.Client.Models.NhanSu.NhanVien;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.NhanVien;

// Adapter của màn Interactive Server: đọc theo page qua Application contract và chỉ map DTO thành view model.
public sealed class NhanVienDataProvider(
    INhanVienListReadService nhanVienListReadService,
    INhanVienSummaryReadService nhanVienSummaryReadService,
    INhanVienCreateService nhanVienCreateService,
    INhanVienEditService nhanVienEditService,
    INhanVienDeleteService nhanVienDeleteService,
    INhanVienStatusService nhanVienStatusService,
    INhanVienExportReadService nhanVienExportReadService,
    INhanVienRefreshService nhanVienRefreshService,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory)
{
    public async Task<NhanVienListLoadResult> LoadPageAsync(
        NhanVienListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await nhanVienListReadService.SearchPageAsync(query, cancellationToken);
        return new NhanVienListLoadResult(
            page.Rows.Select(MapRecord).ToArray(),
            page.TotalCount);
    }

    public Task<EmployeeSummaryDto> GetSummaryAsync(
        string? searchText,
        CancellationToken cancellationToken = default) =>
        nhanVienSummaryReadService.GetSummaryAsync(new EmployeeFilter(searchText), cancellationToken);

    public async Task<IReadOnlyList<EmployeeRecord>> LoadAllForExportAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await nhanVienExportReadService.ExportAllAsync(cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public async Task<EmployeeRecord> CreateAsync(
        CreateEmployeeFormModel model,
        CancellationToken cancellationToken = default)
    {
        var row = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.NhanVien.Create,
            token => nhanVienCreateService.CreateAsync(
                new CreateEmployeeRequest(
                    model.EmployeeCode ?? string.Empty,
                    model.FullName ?? string.Empty,
                    model.DepartmentId ?? Guid.Empty,
                    model.PositionId ?? Guid.Empty,
                    (int)model.Status,
                    model.HireDate),
                token),
            cancellationToken: cancellationToken);
        return MapRecord(row);
    }

    public Task<EmployeeRefreshResult> RefreshFromDeviceUserProfilesAsync(
        CancellationToken cancellationToken = default) =>
        auditCommandScopeFactory.ExecuteAsync(
            AuditActions.NhanVien.RefreshFromAttendance,
            token => nhanVienRefreshService.RefreshFromDeviceUserProfilesAsync(token),
            cancellationToken: cancellationToken);

    public async Task<EmployeeRecord> UpdateAsync(
        EmployeeRecord employee,
        CreateEmployeeFormModel model,
        CancellationToken cancellationToken = default)
    {
        var row = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.NhanVien.Update,
            token => nhanVienEditService.UpdateAsync(
                new UpdateEmployeeRequest(
                    employee.Id,
                    model.EmployeeCode ?? string.Empty,
                    model.FullName ?? string.Empty,
                    model.DepartmentId ?? Guid.Empty,
                    model.PositionId ?? Guid.Empty,
                    employee.Status,
                    model.HireDate,
                    employee.UpdatedAtUtc ?? employee.CreatedAtUtc),
                token),
            cancellationToken: cancellationToken);
        return MapRecord(row);
    }

    public Task DeleteAsync(EmployeeRecord employee, CancellationToken cancellationToken = default) =>
        auditCommandScopeFactory.ExecuteAsync(
            AuditActions.NhanVien.Delete,
            async token =>
            {
                await nhanVienDeleteService.DeleteAsync([employee.Id], token);
                return true;
            },
            cancellationToken: cancellationToken);

    public async Task<EmployeeRecord> ChangeStatusAsync(
        EmployeeRecord employee,
        EmployeeEmploymentStatus status,
        DateTime? seniorityStartDate,
        DateTime? resignedDate,
        CancellationToken cancellationToken = default)
    {
        var row = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.NhanVien.ChangeStatus,
            token => nhanVienStatusService.ChangeStatusAsync(
                new ChangeEmployeeStatusRequest(
                    employee.Id,
                    (int)status,
                    seniorityStartDate,
                    resignedDate,
                    employee.UpdatedAtUtc ?? employee.CreatedAtUtc),
                token),
            cancellationToken: cancellationToken);
        return MapRecord(row);
    }

    private static EmployeeRecord MapRecord(EmployeeListItemDto source) =>
        new()
        {
            Id = source.Id,
            EmployeeCode = source.EmployeeCode,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Email = source.Email,
            PhoneNumber = source.PhoneNumber,
            AvatarDataUrl = source.AvatarDataUrl,
            HireDate = source.HireDate,
            DepartmentId = source.DepartmentId,
            DepartmentCode = source.DepartmentCode,
            DepartmentName = source.DepartmentName,
            DepartmentPath = source.DepartmentPath,
            PositionId = source.PositionId,
            PositionCode = source.PositionCode,
            PositionName = source.PositionName,
            Status = source.Status,
            SeniorityStartDate = source.SeniorityStartDate,
            ResignedDate = source.ResignedDate,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
