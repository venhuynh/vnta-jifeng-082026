# Kế hoạch chuẩn hóa folder xuyên project theo UI

## Mục tiêu

Lấy `Vnta.Hrm.Web.Client/Components/{NhomNghiepVu}/{ContextKey}/` làm taxonomy
nguồn. Mỗi implementation gắn một feature phải có đường dẫn tương ứng trong các
project còn lại; technical alias chỉ dùng trong adapter, schema hoặc integration.

```text
Web.Client/Components/{NhomNghiepVu}/{ContextKey}/
Web.Client/Models/{NhomNghiepVu}/{ContextKey}/
Web.Client/Services/Api/{NhomNghiepVu}/{ContextKey}/
Web.Client/Services/DataProviders/{NhomNghiepVu}/{ContextKey}/
Web/Endpoints/{NhomNghiepVu}/{ContextKey}/
Web/Services/{NhomNghiepVu}/{ContextKey}/
Application/{NhomNghiepVu}/{ContextKey}/
Domain/{NhomNghiepVu}/{ContextKey}/
Infrastructure/{NhomNghiepVu}/{ContextKey}/
```

`Infrastructure` đã áp dụng physical folder theo map tại
`doc/project/infrastructure-feature-folder-map.md`.

## Phạm vi theo project

| Project | Di dời theo feature | Giữ root kỹ thuật |
| --- | --- | --- |
| `Vnta.Hrm.Web.Client` | `Models`, `Services/Api`, `Services/DataProviders`; `Components` tiếp tục là taxonomy nguồn | `Navigation`, `Tools`, `Utils`, `Shared`, `SharedUi`, `UiDemo` |
| `Vnta.Hrm.Web` | `Endpoints`, server API adapter trong `Services`, component host chỉ thuộc feature khi có | `Components/Account`, layout, `Data`, `Hubs`, `Properties`, `Program.cs`, `wwwroot` |
| `Vnta.Hrm.Application` | DTO, request, filter, interface, use case theo `{NhomNghiepVu}/{ContextKey}` | `Common`, `Integrations`, contract hệ thống dùng chung |
| `Vnta.Hrm.Domain` | entity, value object, rule chỉ thuộc một context | `Common`, contract nền tảng và quy tắc dùng chung |

Không di dời EF migrations, `ApplicationDbContext`, composition root, Identity,
gateway module hoặc file static asset chỉ để tạo cấu trúc giống UI.

## Mapping nghiệp vụ đích

| Nhóm | Context ưu tiên refactor |
| --- | --- |
| `NhanSu` | `ChiTietNhanVien`, `NhanVien`, `PhongBan`, `ChucVu` |
| `CaKip` | `BangXepCa`, `CaiDatCa`, `CaiDatXepCa`, `LichLamViec` |
| `ChamCong` | `BangCongThang`, `CodeKetQuaTinhCong`, `DuLieuTho` |
| `DangKyPheDuyet` | `DangKyTangCa` |
| `DangTrienKhai` | `BangCongNgay`, `DuLieuSinhTracHoc`, `LuongCanBan` |
| `KhauTru` | `KhauTruBHXHYT`, `KhauTruTongHop` |
| `PhuCap` | từng `PhuCap*` đang có UI thật |
| `QuanTri` | `GiamSatAdms`, `LenhMayChamCong`, `MayChamCong`, `TaiKhoanNhanVien` |
| `TongQuan` | `ChamCongHangNgay` |

`TinhLuong` chỉ tạo folder khi đã có implementation feature; Git không lưu folder
rỗng.

## Thứ tự thực hiện

1. **Chốt checkpoint hiện tại.** Rà toàn bộ rename đã staged/unstaged, tạo commit
   riêng cho chuẩn folder Infrastructure trước khi tiếp tục project khác.
2. **Hoàn tất vertical slice `NhanSu/ChiTietNhanVien`.** Di dời Application từ
   module technical `Employees` sang `Application/NhanSu/ChiTietNhanVien`; đổi
   namespace và toàn bộ using xuyên Client, Web, Infrastructure trong cùng lượt.
3. **Chuẩn hóa `NhanSu` dùng chung.** Refactor `NhanVien`, `PhongBan`, `ChucVu`
   qua Application, Web endpoint/server adapter, Client model/API/provider. Giữ
   `Employee` chỉ làm technical alias cho schema hoặc gateway.
4. **Theo feature có boundary độc lập.** Thực hiện lần lượt `CaKip`, `ChamCong`,
   `DangKyPheDuyet`, rồi `DangTrienKhai` và `TongQuan`. Mỗi lượt gồm move folder,
   namespace, DI, endpoint route owner, tài liệu màn hình và kiểm tra tĩnh.
5. **Payroll theo context UI.** Tách từng `PhuCap*` và `KhauTru*` khỏi module
   `Payroll` cũ ở Application/Web/Client, không đổi schema hoặc migration chỉ vì
   đổi folder.
6. **Domain sau cùng.** Chỉ đổi Domain khi ownership của entity/rule đã rõ;
   không ép entity dùng chung vào một màn hình.
7. **Dọn compatibility alias.** Sau khi các consumer đã dùng context key mới,
   xóa adapter/namespace compatibility và cập nhật source map, screen spec,
   checklist, implementation log.

## Quy trình bắt buộc cho mỗi context

1. Xác định owner UI, API route, contract, persistence và technical alias.
2. Dùng `git mv` để giữ lịch sử file; đổi namespace và using trong cùng thay đổi.
3. Cập nhật DI/composition root và tất cả endpoint/client registrations.
4. Tìm toàn repo để bảo đảm không còn tham chiếu path/namespace cũ ngoài tài liệu
   lịch sử được ghi rõ là historical.
5. Kiểm tra tĩnh project item include, route, DI, JSON config và `git diff --check`.
6. Chỉ chạy build hoặc smoke test khi có chỉ đạo riêng theo quy tắc verification.

## Tiêu chí hoàn thành

- Từ một context UI có thể tìm được model, provider, endpoint, contract và
  persistence qua cùng `{NhomNghiepVu}/{ContextKey}`.
- Không còn mega endpoint/service nhận thêm feature không cùng context.
- Root kỹ thuật chỉ chứa cross-cutting hoặc composition root, không chứa feature
  của một màn hình cụ thể.
- Tài liệu source map và screen spec chỉ đến đúng folder mới.
