# Infrastructure Feature Folder Map

`Vnta.Hrm.Infrastructure` được tổ chức theo cùng nhóm nghiệp vụ và context key
với `Vnta.Hrm.Web.Client/Components` để tra cứu implementation theo màn hình.

## Quy ước áp dụng

```text
Vnta.Hrm.Web.Client/Components/{NhomNghiepVu}/{ContextKey}/
Vnta.Hrm.Infrastructure/{NhomNghiepVu}/{ContextKey}/
```

File/class legacy có thể vẫn giữ technical alias như `AttendanceGateway` hoặc
`Payroll` khi alias đó phản ánh schema hay external gateway. Physical folder là
điểm tra cứu chính; tên technical alias được đổi cùng đợt refactor nghiệp vụ,
không đổi chỉ vì move file.

## Root nghiệp vụ đã áp dụng

| Root Infrastructure | Context đã phân nhóm |
| --- | --- |
| `CaKip` | `BangXepCa`, `CaiDatCa`, `CaiDatXepCa`, `LichLamViec` |
| `ChamCong` | `BangCongThang`, `CodeKetQuaTinhCong`, `DuLieuTho` |
| `DangKyPheDuyet` | `DangKyTangCa` |
| `DangTrienKhai` | `BangCongNgay`, `DuLieuSinhTracHoc`, `LuongCanBan` |
| `KhauTru` | `KhauTruBHXHYT`, `KhauTruTongHop` |
| `NhanSu` | `ChiTietNhanVien`, `ChucVu`, `NhanVien`, `PhongBan` |
| `PhuCap` | `PhuCapChuyenCan`, `PhuCapCom`, `PhuCapDocHai`, `PhuCapPhepLe`, `PhuCapThamNien`, `PhuCapTongHop`, `PhuCapTrachNhiem`, `PhuCapTrachNhiemKhac` |
| `QuanTri` | `GiamSatAdms`, `LenhMayChamCong`, `MayChamCong`, `TaiKhoanNhanVien` |
| `TongQuan` | `ChamCongHangNgay` |

## Thành phần giữ root kỹ thuật

- `Data/`: `ApplicationDbContext`, EF configuration, migrations và schema guard.
- `Identity/`: mô hình Identity và email sender.
- `Integrations/AttendanceGateway/`: module DI, inbound gateway và adapter dùng
  chung không gắn một màn hình cụ thể.
- `Integrations/Payroll/`: module DI của Payroll.
- `DependencyInjection.cs`: composition root của Infrastructure.

`NhanSu/ChiTietNhanVien/DatabaseChiTietNhanVienService.cs` là mẫu đầu tiên có
namespace đồng thời mirror UI `Components/NhanSu/ChiTietNhanVien/`.
