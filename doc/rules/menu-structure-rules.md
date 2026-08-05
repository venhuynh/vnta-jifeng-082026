# Quy tắc cấu trúc menu

Tài liệu này chốt quy tắc tổ chức menu, source folder, namespace, icon và tài liệu màn hình cho UI HRM.

## Nguồn chuẩn

- Cây menu chuẩn nằm tại `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs`.
- Bản đồ source hiện hành nằm tại `doc/project/source-map.md`.
- Tài liệu đồng bộ branch nằm tại `doc/project/menu-sync-20260701.md`.
- Prompt đồng bộ branch bằng Codex nằm tại `doc/project/codex-branch-sync-prompt-20260701.md`.

## Root menu hiện hành

- `UI DEMO`
- `Đang triển khai`
- `Tổng quan`
- `Nhân sự`
- `Ca kíp`
- `Chấm công`
- `Phụ cấp`
- `Tính lương`
- `Quản trị`

## Quy tắc bắt buộc

- Khi tạo menu mới mà chưa đủ điều kiện tách thành nhóm nghiệp vụ chính thức, mặc định đặt dưới root `Đang triển khai`.
- Cấu trúc folder trong `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/` phải phản chiếu đúng cấu trúc menu hiện hành.
- Tên folder dùng `PascalCase`, không dấu, không khoảng trắng.
- Mỗi màn hình leaf nên có một folder riêng cùng tên với node menu đã chuẩn hóa.
- File component chính của màn hình phải cùng tên với folder màn hình.
- Namespace của component phải phản chiếu đúng đường dẫn folder.
- Tài liệu trong `doc/screens/` phải phản chiếu cùng cấu trúc menu.
- Màn đã nằm trong nhóm nghiệp vụ nhưng chưa hoàn thiện phải đặt `IsInProgress = true` trong `VntaNavMenuCatalog` để hiển thị badge trạng thái.
- Route chỉ được đổi trong một lượt refactor có chủ đích; không đổi route chỉ vì đổi folder hoặc đổi tên file.
- Khi di dời source, ưu tiên `git mv` để giữ lịch sử file.

## Quy tắc icon DevExpress bắt buộc

- Tất cả node menu chuẩn phải lấy `IconUrl` đúng theo `VntaNavMenuCatalog.cs` và `VntaDevExpressIcons`.
- Không được giữ icon placeholder, icon xám mặc định, icon vuông trống hoặc node không có icon nếu `main` đã có icon chuẩn.
- Các branch đang thiếu icon, còn Bootstrap icon hoặc chỉ áp dụng một phần DevExpress icon bắt buộc phải đồng bộ lại đầy đủ trước khi tiếp tục feature work.
- Khi thêm menu mới, phải chọn DevExpress icon phù hợp với ngữ cảnh nghiệp vụ, cập nhật `VntaNavMenuCatalog.cs` và bổ sung mapping vào `VntaDevExpressIcons` nếu cần.
- Không dùng Bootstrap Icons, class `bi`, class `bi-*` hoặc CDN `bootstrap-icons` cho menu.

## Phân nhóm source

- Mọi màn demo hoặc baseline từ sample CRM phải nằm dưới `Components/UiDemo/`.
- Mọi màn nghiệp vụ HRM thật đã chốt IA dài hạn phải nằm dưới root business tương ứng như `NhanSu`, `ChamCong`, `QuanTri`.
- Màn nghiệp vụ thật nhưng còn trong pha triển khai hoặc chưa chốt IA có thể tạm neo tại `Components/DangTrienKhai/`.
- Không trộn source demo vào các root nghiệp vụ thật.

## Root legacy không được dùng cho source mới

Không tạo source mới trong các root cũ sau:

- `Components/Contacts/`
- `Components/Planning/`
- `Components/Analytics/`
- `Components/Attendance/`
- `Components/Implementation/`

Nếu cần sửa màn đang sống trong các root cũ, phải di dời về root mới trước hoặc trong cùng lượt refactor.

## Quy tắc đặt tên chuẩn hóa theo menu

- `UI DEMO > Contacts > Contact List` tương ứng `Components/UiDemo/Contacts/ContactList/`.
- `Chấm công > Dữ liệu thô` tương ứng `Components/ChamCong/DuLieuTho/`.
- `Nhân sự > Phòng ban` tương ứng `Components/NhanSu/PhongBan/`.
- `Nhân sự > Chức vụ` tương ứng `Components/NhanSu/ChucVu/`.
- `Nhân sự > Nhân viên > Danh sách` tương ứng `Components/NhanSu/NhanVien/`.
- `Nhân sự > Nhân viên > Chi tiết nhân viên` tương ứng `Components/NhanSu/ChiTietNhanVien/`.
- `Ca kíp > Cài đặt ca` tương ứng `Components/CaKip/CaiDatCa/`.
- `Ca kíp > Lịch làm việc` tương ứng `Components/DangTrienKhai/LichLamViec/`.
- `Ca kíp > Cài đặt xếp ca` tương ứng `Components/CaKip/CaiDatXepCa/`.
- `Ca kíp > Bảng xếp ca` tương ứng `Components/DangTrienKhai/BangXepCa/`.
- `Chấm công > Bảng công ngày` tương ứng `Components/DangTrienKhai/BangCongNgay/`.
- `Chấm công > Bảng công tháng` tương ứng `Components/ChamCong/BangCongThang/`.
- `Chấm công > Code kết quả tính công` tương ứng `Components/DangTrienKhai/KetQuaTinhCong/`.
- `Quản trị > Máy chấm công` tương ứng `Components/QuanTri/MayChamCong/`.
- `Quản trị > Sinh trắc học` hiện dùng source `Components/DangTrienKhai/DuLieuSinhTracHoc/`.
- `Quản trị > Giám sát ADMS` tương ứng `Components/QuanTri/GiamSatAdms/`.
- `Quản trị > Lệnh máy chấm công` tương ứng `Components/QuanTri/LenhMayChamCong/`.

## Checklist trước khi commit

- [ ] Menu node mới đã được đặt đúng root.
- [ ] Folder mới phản chiếu đúng menu path.
- [ ] File màn hình chính cùng tên với folder leaf.
- [ ] Namespace đã đổi theo folder mới.
- [ ] Menu chưa hoàn thiện đã có badge trạng thái `IsInProgress`.
- [ ] Icon DevExpress đã khớp `VntaNavMenuCatalog.cs` và `VntaDevExpressIcons`.
- [ ] Tài liệu `doc/screens/` và `doc/project/source-map.md` đã cập nhật.
- [ ] Không còn source mới nằm ở root legacy.
- [ ] Build của `src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj` chạy thành công.

## Anti-pattern cần tránh

- Đưa menu mới thẳng ra top-level business khi chưa chốt information architecture.
- Đổi tên file nhưng giữ namespace cũ.
- Di dời file mà quên cập nhật đọc hoặc source-map.
- Tạo một menu path nhưng source lại nằm ở folder không cùng cấu trúc.
- Tiếp tục thêm source vào `Contacts`, `Planning`, `Analytics`, `Attendance`, `Implementation`.


