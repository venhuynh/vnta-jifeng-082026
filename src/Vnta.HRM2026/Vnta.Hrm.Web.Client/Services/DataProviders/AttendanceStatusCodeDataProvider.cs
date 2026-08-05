using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class AttendanceStatusCodeDataProvider(IAttendanceStatusCodeService attendanceStatusCodeService)
{
    public async Task<IReadOnlyList<AttendanceStatusCodeRecord>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await attendanceStatusCodeService.GetAsync(cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public async Task<AttendanceStatusCodeRecord> UpdateFlagsAsync(
        AttendanceStatusCodeRecord source,
        AttendanceStatusCodeRecord editModel,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateAttendanceStatusCodeFlagsRequest(
            source.Id,
            source.UpdatedAtUtc ?? source.CreatedAtUtc,
            editModel.CongHanhChinh,
            editModel.PhuCapTrachNhiemTinhNangSuat,
            editModel.PhuCapDocHai,
            editModel.PhuCapTrachNhiemKhac,
            editModel.PhuCapPhepLe,
            editModel.PhuCapTrachNhiemKhongTinhNangSuat,
            editModel.PhuCapChuyenCan,
            editModel.PhuCapThamNien,
            editModel.KhauTruTamUng);

        var result = await attendanceStatusCodeService.UpdateFlagsAsync(request, cancellationToken);
        return MapRecord(result);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        attendanceStatusCodeService.DeleteAsync(id, cancellationToken);

    private static AttendanceStatusCodeRecord MapRecord(AttendanceStatusCodeListItemDto row) =>
        new()
        {
            Id = row.Id,
            Code = row.Code,
            Name = row.Name,
            Kind = row.Kind,
            CongTangCa = row.CongTangCa,
            CongHanhChinh = row.CongHanhChinh,
            PhuCapTrachNhiemTinhNangSuat = row.PhuCapTrachNhiemTinhNangSuat,
            PhuCapDocHai = row.PhuCapDocHai,
            PhuCapTrachNhiemKhac = row.PhuCapTrachNhiemKhac,
            PhuCapPhepLe = row.PhuCapPhepLe,
            PhuCapTrachNhiemKhongTinhNangSuat = row.PhuCapTrachNhiemKhongTinhNangSuat,
            PhuCapChuyenCan = row.PhuCapChuyenCan,
            PhuCapThamNien = row.PhuCapThamNien,
            KhauTruTamUng = row.KhauTruTamUng,
            IsActive = row.IsActive,
            Note = row.Note,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc
        };
}
