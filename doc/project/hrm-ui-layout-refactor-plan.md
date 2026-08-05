# Kế Hoạch Refactor Layout UI HRM

Tài liệu này dùng để triển khai refactor các màn UI HRM theo chuẩn layout đã
chốt từ màn `Máy chấm công`.

## Mục tiêu

- Đưa các màn list CRUD tiêu chuẩn về cùng skeleton layout:
  `content-root -> card toolbar -> screen-root -> HrmLoadingPanel -> DxGrid/DxTreeList`.
- Giữ custom layout cho các màn có nghiệp vụ đặc thù như realtime dashboard,
  master-detail hoặc operational review, nhưng vẫn căn shell, toolbar và CSS
  theo chuẩn chung khi phù hợp.
- Không refactor các màn trong `Components/UiDemo`.
- Không trộn thêm style bo tròn/gradient hoặc shared layout khác chuẩn vào màn
  production.

## Chuẩn tham chiếu

- Source mẫu: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor`
- CSS mẫu: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor.css`
- Rule chính: `doc/project/hrm-list-screen-blueprint.md`
- Checklist: `doc/checklists/ui-screen-checklist.md`
- DevExpress rule: `doc/rules/blazor-devexpress-rules.md`

## Thứ tự triển khai

### Trạng thái triển khai

#### 2026-07-03

- Đã tạo tài liệu triển khai refactor layout UI.
- Đã refactor `NhanSu/ChucVu/ChucVu.razor` khỏi `VntaDataListPageLayout` về
  explicit skeleton chuẩn.
- Đã refactor `CaKip/CaiDatCa/CaiDatCa.razor` khỏi `VntaDataListPageLayout`
  về explicit skeleton chuẩn.
- Đã refactor `QuanTri/LenhMayChamCong/LenhMayChamCong.razor` khỏi
  `VntaDataListPageLayout` và `VntaMasterDataListToolbar`.
- Sau lát này, không còn màn production ngoài `UiDemo` tham chiếu
  `VntaDataListPageLayout` hoặc `VntaMasterDataListToolbar`.
- Đã build `src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj` thành công.

#### 2026-07-15

- Đã refactor `PhuCap/PhuCapTrachNhiemCapBac/PhuCapTrachNhiemCapBac.razor`
  về shell rõ ràng theo route `/payroll/responsibility-allowances/grades`.
- Đã bỏ toolbar workflow cũ không còn phù hợp với màn config grade, giữ lại
  toolbar tối thiểu `Mới`, `Làm mới`, `Xuất dữ liệu`, `Chọn cột`.
- Đã chuẩn hóa command column icon `Sửa / Xóa`, popup edit form và loading /
  empty / error state.
- Đã build `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Vnta.Hrm.Web.Client.csproj`
  thành công với `0 warning`, `0 error`.

#### 2026-07-16

- Đã refactor `PhuCap/PhuCapTrachNhiem_GanChucVu/PhuCapTrachNhiemGanChucVu.razor`
  thành owner screen độc lập theo route
  `/payroll/responsibility-allowances/position-assignments`.
- Đã chuẩn hóa toolbar theo pattern `Năm/Tháng + Xem`, giữ các action đúng
  phạm vi màn `grade_positions` gồm `Lấy từ tháng trước`, `Mới`, `Làm mới`,
  `Xuất dữ liệu`, `Chọn cột`.
- Đã thay khối table cũ bằng `DxGrid`, đưa popup edit ra ngoài `content-root`
  và tách form con dùng `DxDropDownBox` cho lookup `Chức vụ` và `Cấp bậc`.
- Đã build `src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj` thành công với
  `0 warning`, `0 error`.

### P0 - Dọn phụ thuộc shared layout cũ

- Không mở rộng thêm `VntaDataListPageLayout` cho màn production chuẩn.
- Các màn đang dùng `VntaDataListPageLayout` sẽ được refactor về explicit
  skeleton giống `MayChamCong`.
- Sau khi không còn màn production phụ thuộc, rà lại `SharedUi/Layout` và
  `SharedUi/MasterData` để quyết định giữ, rewrite hoặc loại bỏ.

### P1 - Màn list CRUD cần refactor ngay

1. `NhanSu/ChucVu/ChucVu.razor`
2. `CaKip/CaiDatCa/CaiDatCa.razor`
3. `QuanTri/LenhMayChamCong/LenhMayChamCong.razor`

### P2 - Màn gần chuẩn, chỉ căn lại

1. `NhanSu/PhongBan/PhongBan.razor`
2. `QuanTri/MayChamCong/MayChamCong.razor`

`MayChamCong` là baseline nên chỉ rà không đổi layout nếu không cần.

### P3 - Màn operational custom

1. `DangTrienKhai/DuLieuTho/DuLieuTho.razor`
2. `DangTrienKhai/CodeKetQuaTinhCong/CodeKetQuaTinhCong.razor`
3. `DangTrienKhai/NhanVien/NhanVien.razor`

Các màn này được phép giữ filter, detail popup hoặc master-detail riêng, nhưng:

- toolbar vẫn phải là `card toolbar`
- content shell vẫn phải có giãn ổn định
- popup độc lập nên tách component và render ngoài `content-root`
- không bọc card lồng card nếu không cần

### P4 - Shell và màn không thuộc list standard

1. `Layout/MainLayout.razor`
2. `Index.razor`
3. placeholder group pages
4. `QuanTri/GiamSatAdms/GiamSatAdms.razor`

`GiamSatAdms` là realtime dashboard, không ép theo list CRUD skeleton. Chỉ dọn
những điểm lệch rõ như caption demo, toolbar text hoặc CSS shell nếu cần.

## Tiêu chí hoàn tất cho mỗi màn

- File chính dùng skeleton chuẩn hoặc có lý do custom rõ ràng.
- Toolbar action đúng thứ tự theo rule.
- File edit form hoặc popup được tách riêng.
- CSS page có class riêng theo màn, không copy class nghiệp vụ của màn khác.
- Grid hoặc tree có loading, empty, error state rõ ràng.
- Không còn dùng `VntaDataListPageLayout` cho màn list production chuẩn.
- Sau mỗi lát refactor, chạy kiểm tra phù hợp hoặc ghi rõ chưa chạy build/test.


