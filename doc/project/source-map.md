# Bản đồ source

Tài liệu này là điểm vào nhanh để đọc đúng thư mục sau khi cây menu được tổ chức lại theo cấu trúc hiện hành.

## Vị trí chính

- `src/Vnta.HRM2026/Vnta.Hrm.slnx`: solution HRM hiện hành của repo.
- `src/Vnta.HRM2026/Vnta.Hrm.Domain/`: domain layer cho entity, value object và rule nghiệp vụ.
- `src/Vnta.HRM2026/Vnta.Hrm.Application/`: application layer cho use case, DTO, validation và orchestration.
- `src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/`: infrastructure layer cho DI, persistence, identity và integration.
- `src/Vnta.HRM2026/Vnta.Hrm.Web/Program.cs`: composition root của host.
- `src/Vnta.HRM2026/Vnta.Hrm.Web/Components/App.razor`: shell gốc của ứng dụng.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Program.cs`: đăng ký service phía client.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Routes.razor`: router phía client.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/`: layout, shell và điều hướng chính của UI.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs`: nguồn khai báo cây menu.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Services/`: service helpers, data providers, theme, search và HTTP/API client.

## Components theo menu

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/UiDemo/`: source cho root `UI DEMO`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/DangTrienKhai/`: placeholder bucket cho root `Đang triển khai`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/DangKyPheDuyet/`: feature bucket cho các màn workflow đăng ký/phê duyệt đang cần tách source sớm trước khi chốt root menu dài hạn.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/TongQuan/`: source cho root `Tổng quan`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/TongQuan/ChamCongHangNgay/`: source màn `Chấm công hằng ngày` dưới nhóm `Tổng quan`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/`: source cho root `Nhân sự`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/CaKip/`: source cho root `Ca kíp`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/ChamCong/`: source cho root `Chấm công`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/`: source cho root `Phụ cấp`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/TinhLuong/`: source cho root `Tính lương`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/KhauTru/`: source cho root `Khấu trừ`.
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/`: source cho root `Quản trị`.

## Mapping màn hình hiện tại

- `UI DEMO > Contacts > Contact List`
  Route: `/ContactList`
  Source: `Components/UiDemo/Contacts/ContactList/`
- `UI DEMO > Contacts > Contact Details`
  Route: `/ContactDetails`
  Source: `Components/UiDemo/Contacts/ContactDetails/`
- `UI DEMO > Planning > Task List`
  Route: `/TaskList`
  Source: `Components/UiDemo/Planning/TaskList/`
- `UI DEMO > Planning > Scheduler`
  Route: `/Scheduler`
  Source: `Components/UiDemo/Planning/Scheduler/`
- `UI DEMO > Analytics > Dashboard`
  Route: `/Dashboard`
  Source: `Components/UiDemo/Analytics/Dashboard/`
- `UI DEMO > Analytics > Sales Analysis`
  Route: `/SalesAnalysis`
  Source: `Components/UiDemo/Analytics/SalesAnalysis/`
- `Tổng quan > Chấm công`
  Route: `/overview/daily-attendance`
  Source: `Components/TongQuan/ChamCongHangNgay/`
  Notes: màn tổng hợp attendance hằng ngày với KPI + TreeList phòng ban. Route gốc, fallback sau đăng nhập không có `ReturnUrl`, các luồng `2FA`/`Recovery Code` không có `ReturnUrl` và launch profile dev đều hội tụ về `Phụ cấp > Phụ cấp thâm niên` (`/payroll/seniority-allowance`), nên màn này được mở qua menu `Tổng quan` hoặc route riêng.
- `Đang triển khai > Đăng ký tăng ca`
  Route: `/approval/overtime-registrations`
  Source: `Components/DangKyPheDuyet/DangKyTangCa/`
  Notes: runtime đã nối provider/API/persistence thật cho workflow OT; source đã tách `DangKyTangCa*` cho màn danh sách và `DangKyTangCaPopup*` cho popup; menu đang neo dưới `Đang triển khai`; persistence hiện dùng các bảng `attendance_overtime_registration_requests`, `attendance_overtime_registration_details`, `attendance_overtime_registration_histories`; route gốc `/`, fallback sau đăng nhập không có `ReturnUrl`, luồng `2FA`/`Recovery Code` không có `ReturnUrl`; trên nhánh hiện tại default entry của ứng dụng là `Phụ cấp > Phụ cấp thâm niên` (`/payroll/seniority-allowance`).
- `Nhân sự > Nhân viên > Chi tiết nhân viên`
  Route: `/attendance/employees/details`
  Source: `Components/DangTrienKhai/ChiTietNhanVien/`
- `Ca kíp > Lịch làm việc`
  Route: `/attendance/work-calendar`
  Source: `Components/DangTrienKhai/LichLamViec/`
- `Ca kíp > Cài đặt xếp ca`
  Route: `/attendance/shift-scheduling-settings`
  Source: `Components/CaKip/CaiDatXepCa/`
- `Ca kíp > Bảng xếp ca`
  Route: `/attendance/shift-roster`
  Source: `Components/DangTrienKhai/BangXepCa/`
- `Ca kíp > Cài đặt ca`
  Route: `/attendance/shifts`
  Source: `Components/CaKip/CaiDatCa/`
- `Chấm công > Bảng công ngày`
  Route: `/attendance/workday-summaries`
  Source: `Components/DangTrienKhai/BangCongNgay/`
- `Chấm công > Bảng công tháng`
  Route: `/attendance/monthly-work-summaries`
  Source: `Components/ChamCong/BangCongThang/`
- `Chấm công > Code kết quả tính công`
  Route: `/attendance/result-codes`
  Source: `Components/ChamCong/CodeKetQuaTinhCong/`
  Notes: màn read-only đọc từ `attendance_status_codes`, grid hiện hiển thị trực tiếp `8` cờ boolean nghiệp vụ và không còn cột tóm tắt `Áp dụng`.
- `Chấm công > Dữ liệu thô`
  Route: `/attendance/logs`
  Source: `Components/ChamCong/DuLieuTho/`
- `Phụ cấp > Trách nhiệm > Tổng quan`
  Route: `/payroll/responsibility-allowances`
  Source: `Components/PhuCap/PhuCapTrachNhiem/`
  Notes: màn đang dùng workflow 4 bảng `grades`, `grade_positions`, `employee_assignments`, `abc`; toolbar runtime đã được chuẩn hóa theo mẫu `KhauTruBHXHYT`, dùng mô hình `requested period` và `loaded period`; từ nhánh hiện tại menu `Trách nhiệm` đã được tách thành submenu, trong đó route này là màn tổng quan workflow.
- `Phụ cấp > Trách nhiệm > Cấp bậc`
  Route: `/payroll/responsibility-allowances/grades`
  Source: `Components/PhuCap/PhuCapTrachNhiemCapBac/`
  Notes: màn con chuyên quản lý bảng `payroll_monthly_responsibility_allowance_grades`; runtime hiện tự tải kỳ mặc định `06/2026`, toolbar đã rút gọn còn `Mới`, `Làm mới`, `Xuất dữ liệu`, `Chọn cột`; search lọc local trên dataset đã tải; row action dùng icon `Sửa / Xóa`, trong đó `Xóa` đi theo semantics ngừng dùng (`IsActive = false`), không xóa vật lý; route gốc `/` hội tụ về `Phụ cấp > Phụ cấp thâm niên`, không trỏ trực tiếp đến màn con này.
- `Phụ cấp > Trách nhiệm > Gán chức vụ`
  Route: `/payroll/responsibility-allowances/position-assignments`
  Source: `Components/PhuCap/PhuCapTrachNhiem_GanChucVu/`
  Notes: màn con chuyên quản lý bảng `payroll_monthly_responsibility_allowance_grade_positions`; runtime dùng toolbar `Năm/Tháng` theo mô hình `requested period` và `loaded period`, có `Xem`, `Lấy từ tháng trước`, `Mới`, `Làm mới`, `Xuất dữ liệu`, `Chọn cột`; grid hiển thị `Tên chức vụ`, `Cấp bậc trách nhiệm`, `Trạng thái`, `Ghi chú`; popup edit dùng `DxDropDownBox` cho lookup `Chức vụ` và `Cấp bậc`; trên nhánh hiện tại đây là màn con độc lập, không phải default entry của ứng dụng.
- `Phụ cấp > Phụ cấp khác`
  Route: `/payroll/other-responsibility-allowance`
  Source: `Components/PhuCap/PhuCapTrachNhiemKhac/`
  Notes: màn đã dùng read path thật qua `OtherResponsibilityAllowanceDataProvider`, endpoint search và persistence riêng; runtime bám shell `responsibility-*`, dùng mô hình `kỳ nháp` và `kỳ đang áp dụng`, chỉ tải dữ liệu khi người dùng nhấn `Xem`; đã mở `Tính lại` theo workflow `Phụ cấp trách nhiệm`, `Khóa/Mở khóa` theo các dòng được chọn ở summary row, `Xuất dữ liệu` Excel/PDF và `Chọn cột`; các việc còn mở chủ yếu là write path thủ công, `Lấy từ tháng trước` và sync summary downstream; nhánh hiện tại không dùng màn này làm landing mặc định của ứng dụng.
- `Phụ cấp > Phụ cấp thâm niên`
  Route: `/payroll/seniority-allowance`
  Source: `Components/PhuCap/PhuCapThamNien/`
  Notes: đây là default entry của ứng dụng: route gốc `/`, fallback sau đăng nhập không có `ReturnUrl`, các luồng `2FA`/`Recovery Code` không có `ReturnUrl` và launch profile dev đều hội tụ về route này.
- `Phụ cấp > Phụ cấp chuyên cần`
  Route: `/payroll/attendance-allowance`
  Source: `Components/PhuCap/PhuCapChuyenCan/`
- `Phụ cấp > Phụ cấp cơm`
  Route: `/payroll/meal-allowance`
  Source: `Components/PhuCap/PhuCapCom/`
- `Phụ cấp > Phụ cấp độc hại`
  Route: `/payroll/hazard-allowance`
  Source: `Components/PhuCap/PhuCapDocHai/`
  Notes: runtime dùng `HazardAllowanceDataProvider`, boundary `payroll_allowance_summary_records` + `payroll_allowance_hazard_records`, kỳ toolbar chỉ commit khi bấm `Xem`, summary badge lọc cục bộ trên snapshot đã tải, batch `Khóa/Mở khóa` hiện chạy trên các dòng đang chọn và bị chặn khi toolbar đang giữ kỳ chưa áp; route gốc `/`, fallback sau đăng nhập không có `ReturnUrl` (kể cả 2FA/Recovery Code) và launch profile dev hiện hội tụ về `Phụ cấp > Phụ cấp thâm niên` (`/payroll/seniority-allowance`).
- `Phụ cấp > Phép - Lễ`
  Route: `/payroll/leave-holiday-allowance`
  Source: `Components/PhuCap/PhuCapPhepLe/`
  Notes: màn đã có runtime riêng với bảng detail `payroll_allowance_summary_leave_holiday_records`, đồng thời sync tổng tiền xuống `payroll_allowance_summary_records`; các batch action `Xóa`, `Lấy từ tháng trước`, `Tính lại` hiện đã đi qua command server-side riêng thay vì để UI tự orchestration; ứng dụng hiện không dùng màn này làm landing mặc định, nên route gốc `/` và fallback sau đăng nhập hội tụ về `Phụ cấp > Phụ cấp thâm niên` (`/payroll/seniority-allowance`).
- `Phụ cấp > Tổng hợp`
  Route: `/payroll/allowance-summary`
  Source: `Components/PhuCap/PhuCapTongHop/`
  Notes: màn này giữ route runtime, menu thật và primary data surface riêng; action `Lấy từ tháng trước` hỗ trợ từ `06/2026`, lấy tập nhân viên kỳ đích từ `attendance_workday_summaries`, seed đầy đủ record phụ và xóa record thừa kể cả khi đã khóa; đây không còn là landing mặc định của ứng dụng trên nhánh hiện tại.
- `Tính lương > Lương căn bản`
  Route: `/payroll/basic-salaries`
  Source: `Components/DangTrienKhai/LuongCanBan/`
- `Khấu trừ > Tạm ứng`
  Route: `/payroll/advance-deductions`
  Source: `Components/KhauTru/KhauTruTamUng/`
  Notes: shell runtime đã bám skeleton `KhauTruBHXHYT`; ô tìm kiếm được dời xuống header của `DxGrid` theo pattern `NhanVien`, nhưng đây không còn là landing mặc định của ứng dụng.
- `Khấu trừ > Tổng kết khấu trừ`
  Route: `/payroll/deduction-summary`
  Source: `Components/KhauTru/KhauTruTongHop/`
  Notes: màn này giữ route runtime và primary data surface riêng; đây không phải landing mặc định của ứng dụng trên nhánh hiện tại.
- `Khấu trừ > Thuế TNCN`
  Route: `/payroll/personal-income-tax-deductions`
  Source: `Components/KhauTru/KhauTruThueTNCN/`
  Notes: màn đã có menu runtime, route runtime, source UI, popup `Quy tắc` và shell UI refactor theo pattern `KhauTruBHXHYT`; search nằm ở header grid, flow có `Xem` trước rồi mới auto reload filter phụ, nhưng runtime vẫn chưa có boundary dữ liệu Thuế TNCN riêng và hiện còn dùng shared pipeline `attendance-allowance`.
- `Khấu trừ > Phí công đoàn`
  Route: `/payroll/union-fee-deductions`
  Source: `Components/KhauTru/KhauTruPhiCongDoan/`
- `Khấu trừ > BHXH-YT`
  Route: `/payroll/social-health-insurance-deductions`
  Source: `Components/KhauTru/KhauTruBHXHYT/`
  Notes: màn đã có boundary dữ liệu payroll insurance riêng; runtime dùng `PayrollInsuranceDeductionDataProvider`, endpoint `/api/payroll/social-health-insurance-deductions/*` và bảng `payroll_decuction_summary_insurance_details`; nhánh hiện tại không dùng màn này làm landing mặc định của ứng dụng.
- `Khấu trừ > Giảm trừ gia cảnh`
  Route: `/payroll/family-deductions`
  Source: `Components/KhauTru/GiamTruGiaCanh/`
  Notes: màn quản lý hồ sơ từng người phụ thuộc qua `EmployeeTaxDependentDataProvider`, endpoint `/api/payroll/tax-dependents/*` và bảng `payroll_employee_tax_dependents`; Điều chỉnh dùng concurrency token bắt buộc, kiểm tra kỳ khóa theo hiệu lực cũ/mới và audit che MST/CCCD.
- `Nhân sự > Phòng ban`
  Route: `/attendance/departments`
  Source: `Components/NhanSu/PhongBan/`
- `Nhân sự > Chức vụ`
  Route: `/attendance/positions`
  Source: `Components/NhanSu/ChucVu/`
- `Nhân sự > Nhân viên > Danh sách`
  Route: `/attendance/employees`
  Source: `Components/NhanSu/NhanVien/`
- `Quản trị > Sinh trắc học`
  Route: `/attendance/biometric-data`
  Source: `Components/DangTrienKhai/DuLieuSinhTracHoc/`
- `Quản trị > Máy chấm công`
  Route: `/attendance/devices`
  Source: `Components/QuanTri/MayChamCong/`
- `Quản trị > Giám sát ADMS`
  Route: `/Adms`
  Source: `Components/QuanTri/GiamSatAdms/`
- `Quản trị > Lệnh máy chấm công`
  Route: `/adms/device-commands`
  Source: `Components/QuanTri/LenhMayChamCong/`

## Điểm cần chú ý

- `src/Vnta.HRM` không còn tồn tại trong repo; nếu tài liệu cũ nhắc tới đường dẫn này thì đó là legacy reference.
- `Dữ liệu thô` hiện là màn HRM thật đọc dữ liệu gateway qua boundary `Application` và `Infrastructure`, đã được di dời vào `Chấm công > Dữ liệu thô`.
- `Bảng công tháng` là surface refactor hiện hành dưới `Chấm công`; màn mở lần đầu không gọi database và chỉ tải dữ liệu kỳ khi người dùng bấm `Xem`.
- `Nhân viên` đã được di dời vào `Nhân sự > Nhân viên > Danh sách`, source `Components/NhanSu/NhanVien/`, route vẫn giữ `/attendance/employees`.
- `Nhân viên` hiện là màn tham chiếu chuẩn cho `Operational List Page`; mở màn không query, người dùng bấm `Xem` mới tải page nhân viên từ server. Khi triển khai các màn vận hành tương tự, đọc thêm `doc/checklists/operational-list-screen-checklist.md` và `doc/checklists/operational-list-data-processing-standard.md`.
- `Đang triển khai` hiện là bucket cho các màn chưa chốt information architecture dài hạn; không giữ `Chi tiết nhân viên` trong root này vì màn đã được đưa sang `Nhân sự`.
- Một số màn workflow có thể tạm neo menu ở `Đang triển khai` nhưng source lại nằm ở bucket feature riêng như `Components/DangKyPheDuyet/`; khi chốt IA dài hạn cần dời cả menu, tài liệu và source-map cùng lượt.
- `Tạm ứng` và `Thuế TNCN` hiện đã được dời cả menu runtime lẫn source UI sang root `Khấu trừ`, nhưng route kỹ thuật vẫn giữ nguyên để tránh gãy link đang dùng; cả hai màn đều đang theo hướng chuẩn hóa shell UI trước khi tách boundary dữ liệu thật.
- `PhuCapTrachNhiemCapBac` hiện đã có source, route và boundary runtime riêng dưới root `Phụ cấp`; landing mặc định hiện hành của toàn app không trỏ về màn này.
- `PhuCapTrachNhiemGanChucVu` hiện đã có source, route và boundary runtime riêng dưới root `Phụ cấp`; landing mặc định hiện hành của toàn app không trỏ về màn này.
- `PhuCapTrachNhiemKhac` hiện đã có source, route và boundary runtime riêng dưới root `Phụ cấp`; landing mặc định hiện hành của toàn app không trỏ về màn này.
- `PhuCapDocHai` hiện đã có source, route và boundary runtime riêng dưới root `Phụ cấp`; đây không phải landing mặc định hiện hành của toàn app.
- `PhuCapPhepLe` hiện đã có source, route và bảng detail runtime riêng dưới root `Phụ cấp`; landing mặc định hiện hành của toàn app không trỏ về màn này.
- `PhuCapTongHop` hiện đã có menu runtime, route chính thức và primary data surface thật; đây không còn là landing mặc định hiện hành của toàn app.
- `PhuCapTongHop` hiện đã có menu runtime, route chính thức và primary data surface thật; các việc còn mở của màn này nằm ở boundary dữ liệu cho `Thâm niên`, `Khác`, `Phép/Lễ`, không còn ở mức scaffold UI/menu.
- Route gốc `/` hiện tự redirect về `Phụ cấp > Phụ cấp thâm niên` (`/payroll/seniority-allowance`) để bám landing mặc định của nhánh hiện tại.
- Route hiện tại được giữ ổn định theo màn hình đang chạy; việc đổi route nên được làm như một bước refactor riêng để tránh gãy link đang dùng.
- Khi một màn được dời sang root menu khác, source folder và `doc/screens/` của nó cũng phải dời theo.

## Thứ tự đọc khi nhận task UI

1. Đọc `Navigation/VntaNavMenuCatalog.cs` để xác định menu path và route.
2. Đọc component trong thư mục đúng theo menu.
3. Đọc `Vnta.Hrm.Web.Client/Program.cs` nếu task liên quan DI hoặc data provider.
4. Đọc `Components/Layout/` nếu task liên quan shell hoặc điều hướng.
5. Đọc đọc trong `doc/screens/` cùng nhóm menu trước khi sửa.

## Thứ tự đọc khi nhận task kiến trúc

1. Đọc `src/Vnta.HRM2026/Vnta.Hrm.slnx` để thấy toàn bộ dependency ở mức solution.
2. Đọc `Vnta.Hrm.Domain`, `Vnta.Hrm.Application`, `Vnta.Hrm.Infrastructure` để giữ đúng ranh giới layer.
3. Đọc `Vnta.Hrm.Web/Program.cs`, `Vnta.Hrm.Infrastructure/` và `Vnta.Hrm.Web/Data/` để biết phần nào đã được bóc ra và phần nào còn ở host.
4. Đọc `src/Vnta.PostgresSync/` và `doc/setup/postgres-sync-console.md` nếu task liên quan luồng đồng bộ PostgreSQL.
5. Đọc `doc/project/target-solution-structure.md` và `doc/project/refactor-roadmap.md`.
