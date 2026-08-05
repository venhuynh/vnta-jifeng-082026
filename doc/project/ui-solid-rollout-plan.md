# Checklist và kế hoạch chuẩn hóa SOLID cho UI

## 1. Mục tiêu và phạm vi

Tài liệu này chuyển các quy định hiện có thành một backlog triển khai có thể
kiểm soát theo từng UI. Nó không thay thế đặc tả nghiệp vụ của từng màn.

Nguồn quy định đã đối chiếu:

- `doc/project/hrm-refactor-standard.md`
- `doc/project/feature-folder-standard.md`
- `doc/checklists/screen-implementation-principles.md`
- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/operational-list-data-processing-standard.md`
- `doc/rules/source-boundary-rules.md`
- `doc/rules/blazor-devexpress-rules.md`

Inventory tại thời điểm lập kế hoạch (2026-07-27), loại trừ `ReferenceCode/` và
`_Imports.razor`:

- 216 Razor UI: 48 client page, 70 dialog, layout/shared/component và 41 UI host/
  Identity ở server.
- Backlog chính gồm 48 client page. Tất cả `*.razor` con trong cùng folder của
  page là phạm vi bắt buộc của cùng một work item; không tạo ticket riêng chỉ để
  tách một popup khỏi workflow mà nó phục vụ.
- `UiDemo/` và server Identity/Account được giữ thành lane riêng. Chúng không
  được dùng làm mẫu nghiệp vụ HRM và không chặn rollout cho các UI HRM.

`KhauTruKhac` là mẫu cho **boundary, luồng trạng thái và trải nghiệm màn danh
sách vận hành**, không phải template để sao chép nguyên xi một file code-behind
lớn. Khi tái sử dụng, tách popup/form/record/provider theo một trách nhiệm.

## 2. Chuẩn SOLID được áp dụng cho mọi work item

|Nguyên tắc|Quy ước áp dụng ở UI|
|---|---|
|S — Single Responsibility|Page giữ markup, view-state và điều phối; form/popup phức tạp là component riêng; provider chỉ map/gọi API; service giữ rule và persistence.|
|O — Open/Closed|Mở rộng bằng context folder, contract và command theo feature; không tăng thêm nhánh feature vào mega page, mega provider hoặc `PayrollEndpoints.cs`.|
|L — Liskov Substitution|UI chỉ biết provider/abstraction và DTO/contract; typed HTTP client hay implementation database thay thế được mà không đổi UI contract.|
|I — Interface Segregation|Tách contract theo workflow: query/list, save, refresh/sync, lock/unlock, export. Không dùng một interface “do mọi thứ” nếu command có rule khác nhau.|
|D — Dependency Inversion|Razor không inject `ApplicationDbContext`, EF Core hoặc SQL; UI -> provider/typed client -> endpoint -> application interface -> infrastructure.|

## 3. Mẫu tham chiếu: `KhauTruKhac`

Phạm vi mẫu:

- Page: `Components/KhauTru/KhauTruKhac/KhauTruKhac.razor`.
- Child UI: `KhauTruKhacEditPopup`, `KhauTruKhacLockActionPopup`,
  `KhauTruKhacMonthlyWorkPopup`, `KhauTruKhacRecalculateConfirmPopup` và
  `KhauTruKhacRulesPopup`.
- Boundary hiện có: page -> `PayrollEmployeeOtherDeductionAllowanceDataProvider`
  -> typed HTTP service -> endpoint ->
  `IPayrollEmployeeOtherDeductionAllowanceService` -> infrastructure.

Các điểm cần nhân rộng theo đúng ngữ cảnh nghiệp vụ:

- State, loading, selection, error/retry, popup visibility và derived
  permission/gate sống ở `.razor.cs`.
- Grid có `KeyFieldName`, search debounce, selection được làm sạch khi reload,
  empty/error state và loading panel rõ ràng.
- Các command `refresh`, điều chỉnh, recalculation, lock/unlock và export có
  feedback, confirmation và contract riêng; UI disable chỉ là UX, server vẫn là
  nơi quyết định lock và validation cuối cùng.
- Edit popup có form/model riêng và optimistic-concurrency value
  (`OriginalUpdatedAtUtc`); save lỗi không đóng popup.
- `CancellationTokenSource` theo vòng đời và `SemaphoreSlim`/request version
  ngăn reload chồng chéo.

Không sao chép máy móc:

- Chỉ thêm command khi màn có rule nghiệp vụ tương ứng; không phải màn nào cũng
  có kỳ lương, khóa, recalculation hay export.
- Không đưa tất cả handler vào page. Khi một workflow độc lập đủ phức tạp, tách
  controller/view-model UI hoặc component theo trách nhiệm trước khi code-behind
  trở thành mega class.
- Duy trì context key `KhauTruKhac` ở folder/UI/model mới; các tên
  `PayrollEmployeeOtherDeductionAllowance*` là technical alias đang tồn tại và
  phải được ghi rõ khi refactor xuyên layer.

## 4. Checklist bắt buộc cho một UI

### 4.1 Trước khi sửa

- [ ] Xác định loại UI: master-data, operational list, dashboard, detail,
  workflow, popup hay shared/layout.
- [ ] Chốt tên nghiệp vụ, `ContextKey` tiếng Việt không dấu và technical alias
  (nếu có); tất cả file mới trong feature dùng cùng context.
- [ ] Vẽ boundary hiện tại và boundary đích `UI -> provider/typed client ->
  endpoint -> application service -> infrastructure`.
- [ ] Liệt kê view-state của UI, source of truth, command riêng, quyền,
  transaction, audit, lock và concurrency cần có.
- [ ] Đối chiếu screen spec hiện có; tạo/cập nhật screen implementation doc khi
  màn có persistence hoặc workflow đáng kể.

### 4.2 Thực thi SOLID và data boundary

- [ ] `.razor` chỉ có markup/binding ngắn; state và handler nằm ở `.razor.cs`;
  CSS cô lập nằm ở `.razor.css`.
- [ ] Popup/form/detail có trách nhiệm riêng và không sửa trực tiếp parent state
  ngoài callback/parameter contract rõ ràng.
- [ ] Không có `ApplicationDbContext`, EF entity, SQL, migration hay schema
  guard trong Razor/client component.
- [ ] UI dùng DTO/request/response/view model; không truyền persistence row hay
  EF entity sống lâu trong form.
- [ ] Provider/typed client không chứa business-rule cuối cùng; endpoint gọi
  application abstraction; Infrastructure sở hữu EF/transaction/integration.
- [ ] Interface tách theo command có rule khác nhau; request/response có tên
  nghiệp vụ, không dùng `object` generic.
- [ ] Folder/file mới theo `{NhomNghiepVu}/{ContextKey}`; technical alias chỉ
  xuất hiện ở adapter/schema/integration khi cần.

### 4.3 Checklist màn danh sách và workflow

- [ ] Mọi trigger load (first load, search/filter, retry, save/command thành
  công) hội tụ qua một `ReloadAsync()` hoặc entry point tương đương.
- [ ] Search/filter/paging/data shaping xử lý server-side khi dữ liệu có thể
  lớn; search client-side còn lại phải được ghi là nợ kỹ thuật.
- [ ] Search debounce 300–500 ms, normalize rỗng thành `null`, có cancellation
  và cơ chế chống reload chồng chéo.
- [ ] Summary và list dùng cùng filter semantics; selection/focus được kiểm soát
  sau reload.
- [ ] `DxGrid`/`DxTreeList` có key ổn định; toolbar, loading, empty/error/retry,
  disabled state và toast tiếng Việt được review.
- [ ] Create/update form hiện validation gần field; toast không thay validation;
  save fail không đóng form.
- [ ] Command refresh/sync/approve/lock/unlock/import/export tách khỏi CRUD,
  có confirmation, error mapping và feedback riêng.
- [ ] Multi-table workflow có transaction server-side; lock/concurrency được
  server kiểm tra lại; UI mô tả được cách xử lý conflict.

### 4.4 Verification và tài liệu

- [ ] Test/kiểm tra manual: first load, search/filter, empty, retry, command
  fail/success, popup cancel/save fail, selection, permission và concurrency.
- [ ] Build/test đúng project bị ảnh hưởng; không có DbContext/SQL injection vào
  Components.
- [ ] Cập nhật screen doc, implementation log và gap register nếu còn pattern
  chuyển tiếp hoặc nợ kỹ thuật.
- [ ] Đối chiếu `doc/checklists/done-checklist.md` trước khi đóng work item.

## 5. Kế hoạch theo từng client page

Mã ưu tiên: **P0** = rủi ro dữ liệu/payroll hoặc nguồn ngoài; **P1** = workflow
nghiệp vụ; **P2** = danh mục/read-only/dashboard; **L** = lane tách riêng.

Mỗi dòng bao gồm toàn bộ child UI cùng folder. “Áp dụng” là loại chuẩn sẽ dùng,
không phải kết luận màn đã đạt chuẩn; audit chi tiết phải hoàn thành ở bước 1
của work item.

|Nhóm / UI route|Loại và ưu tiên|Kế hoạch hành động SOLID|
|---|---|---|
|`/` — `Index`|Shell, P2|Giữ điều hướng mỏng; không thêm rule nghiệp vụ hoặc data access.|
|`/attendance/shift-roster` — `BangXepCa`|Operational + popup, P1|Tách query roster, edit-ca command và validation xung đột lịch; áp dụng reload/gate như mẫu.|
|`/attendance/shifts` — `CaiDatCa`|Master data + form, P1|Chuẩn hóa CRUD contract, form validation và concurrency; giữ cấu hình ca ở service.|
|`/attendance/shift-scheduling-settings` — `CaiDatXepCa`|Configuration + child views, P1|Tách policy xếp ca theo nhân viên/phòng ban thành query/command riêng; tránh logic phân ca trong UI.|
|`/attendance/work-calendar` — `LichLamViec`|Master/workflow + form, P1|Tách calendar query và update lịch; server kiểm tra overlap/holiday và transaction nếu cập nhật hàng loạt.|
|`/attendance/workday-summaries` — `BangCongNgay`|Operational + detail, P0|Server-side date/employee filter, detail DTO riêng, refresh/sync command độc lập và audit nguồn dữ liệu.|
|`/attendance/monthly-work-summaries` — `BangCongThang`|Operational, P0|Tách snapshot kỳ công, recalculation/lock nếu có, paging server-side và concurrency theo kỳ.|
|`/attendance/result-codes` — `CodeKetQuaTinhCong`|Master data + detail popup, P1|Tách list/detail/update contract; đảm bảo code unique và validation server-side.|
|`/attendance/logs` — `DuLieuTho`|Read-heavy operational + detail, P0|Thiết kế filter/paging server-side, query DTO nhỏ, không tải raw log toàn bộ vào circuit; read-only detail.|
|`/approval/overtime-registrations` — `DangKyTangCa`|Workflow + popup, P1|Tách create/edit/approve/reject command, trạng thái transition và permission tại server; form chỉ giữ draft.|
|`/payroll/family-deductions` — `GiamTruGiaCanh`|Payroll operational + popup, P0|Tách period query, edit/rule command, validation người phụ thuộc/hiệu lực và concurrency.|
|`/payroll/social-health-insurance-deductions` — `KhauTruBHXHYT`|Payroll operational + dialogs, P0|Dùng checklist `KhauTruKhac`: query kỳ, recalculation, sync, lock, edit/delete; server là source of truth.|
|`/payroll/other-deductions` — `KhauTruKhac`|Mẫu operational, P0|Giữ làm reference; tách dần endpoint mega `PayrollEndpoints.cs` theo context và theo dõi giới hạn trách nhiệm code-behind.|
|`/payroll/union-fee-deductions` — `KhauTruPhiCongDoan`|Payroll operational + dialogs, P0|Hội tụ với mẫu: query kỳ, refresh/recalculate, edit, detail công tháng và validation lock phía server.|
|`/payroll/advance-deductions` — `KhauTruTamUng`|Payroll master/operational + form, P0|Tách request điều chỉnh tạm ứng khỏi query; kiểm tra kỳ lương/lock/concurrency tại service.|
|`/payroll/personal-income-tax-deductions` — `KhauTruThueTNCN`|Payroll operational + dialogs, P0|Tách calculation, manual edit và rule lookup; xử lý conflict/lock tại server, form hiển thị validation.|
|`/payroll/deduction-summary` — `KhauTruTongKet`|Payroll aggregate + dialogs, P0|Tách summary read model khỏi sync/recalculate/lock command; transaction và source-of-truth cho snapshot phải được ghi rõ.|
|`/attendance/employees/details` — `ChiTietNhanVien`|Detail + forms/popup, P1|Tách profile query, update personal info và lookup; không chia sẻ entity giữa profile form và persistence.|
|`/attendance/positions` — `ChucVu`|Master data + form, P2|Chuẩn hóa CRUD/provider, duplicate validation, empty/error/form validation và optimistic concurrency.|
|`/attendance/employees` — `NhanVien`|Operational list + popup, P1|Duy trì pilot chuẩn: summary/list chung filter, reload entry point, server-side search, refresh attendance và concurrency.|
|`/attendance/departments` — `PhongBan`|Master data + form, P2|Tách hierarchy/query nếu có, kiểm tra duplicate/parent-cycle ở server và dùng form component riêng.|
|`/payroll/attendance-allowance` — `PhuCapChuyenCan`|Payroll operational + dialogs, P0|Áp dụng query kỳ, recalculation, lock, edit và rules command như mẫu; rule chuyên cần nằm server-side.|
|`/payroll/meal-allowance` — `PhuCapCom`|Payroll operational + dialogs, P0|Giữ pilot HTTP; hoàn thiện refresh/sync summary, snapshot ownership, lock/concurrency và server-side paging.|
|`/payroll/allowance-dashboard` — `PhuCapDashboard`|Dashboard, P2|Chỉ dùng read-model query/metric DTO; không đưa phép tính payroll cuối cùng vào component metric.|
|`/payroll/hazard-allowance` — `PhuCapDocHai`|Payroll operational + dialogs, P0|Tách exceptions, monthly-work detail, recalculation/edit/rules thành contracts; transaction nếu refresh ảnh hưởng snapshot.|
|`/payroll/leave-holiday-allowance` — `PhuCapPhepLe`|Payroll operational + dialogs, P0|Tách policy/rule, lock và manual edit; server kiểm tra dữ liệu nghỉ/lễ và kỳ lương.|
|`/payroll/seniority-allowance` — `PhuCapThamNien`|Payroll operational + dialogs, P0|Tách calculation source, monthly-work detail, recalculation, lock; bảo vệ optimistic concurrency.|
|`/payroll/allowance-summary` — `PhuCapTongHop`|Payroll aggregate + dialogs, P0|Tách summary query khỏi manual-edit/refresh/lock; chốt source-of-truth và transaction snapshot.|
|`/payroll/responsibility-allowances` — `PhuCapTrachNhiem`|Payroll workflow + dialogs, P0|Tách assignment/configuration/calculation/adjustment/bonus thành command contract hẹp; popup không sở hữu rule.|
|`/payroll/responsibility-allowances/grades` — `PhuCapTrachNhiemCapBac`|Master data + form, P1|Tách grade CRUD và duplicate/range validation; không phụ thuộc UI responsibility allowance chính.|
|`/payroll/other-responsibility-allowance` — `PhuCapTrachNhiemKhac`|Master/operational, P1|Audit boundary; tách list/update contract theo context, form/validation nếu có workflow chỉnh sửa.|
|`/payroll/responsibility-allowances/position-assignments` — `PhuCapTrachNhiemGanChucVu`|Assignment + form, P1|Tách assignment query/upsert, kiểm tra hiệu lực/chồng lấn/duplicate phía server.|
|`/payroll/responsibility-allowances/employee-assignments` — `PhuCapTrachNhiemGanNhanVien`|Payroll assignment + dialogs, P0|Tách employee assignment, work-month detail, recalculation/rules; server kiểm tra kỳ/lock/concurrency.|
|`/admin/audit-trail` — `AuditTrail`|Read-heavy admin, P1|Server-side filter/paging/export, DTO che dữ liệu nhạy cảm, không expose entity audit trực tiếp.|
|`/attendance/biometric-data` — `DuLieuSinhTracHoc`|External-data workflow + popup, P0|Tách device action command khỏi raw-data query; authorization, retry/idempotency và gateway failure mapping ở server.|
|`/Adms` — `GiamSatAdms`|Realtime monitor, P0|Tách stream/read model/command, cancellation lifecycle và authorization; UI không quản lý kết nối gateway trực tiếp.|
|`/adms/device-commands` — `LenhMayChamCong`|External command + form, P0|Command contract hẹp, queue/idempotency/status query tách biệt; form không gửi dữ liệu thiết bị mơ hồ.|
|`/attendance/devices` — `MayChamCong`|Device master + popup, P0|Tách CRUD device, info query và remote action; secret/connection data không trả về UI.|
|`/admin/account-approvals` — `PheDuyetTaiKhoan`|Security workflow, P0|Tách approve/reject/provision command, authorization/audit/transaction ở server; không tin button disable.|
|`/admin/employee-accounts` — `TaiKhoanNhanVien`|Security master/workflow, P0|Tách account list, link/unlink/lock/reset command; DTO không lộ credential/security token.|
|`/payroll/basic-salaries` — `LuongCanBan`|Payroll master + dialogs, P0|Tách effective-date query/upsert, overlap/range validation, audit và optimistic concurrency.|
|`/overview/daily-attendance` — `ChamCongHangNgay`|Dashboard/read model, P1|Dùng server aggregated DTO/filter, loading/error/retry; không tính aggregate attendance cuối cùng trong client.|
|`/Dashboard` — `UiDemo/Analytics/Dashboard`|Demo, L|Giữ cô lập khỏi domain HRM; nếu product hóa, viết spec và áp dụng checklist từ đầu.|
|`/SalesAnalysis` — `UiDemo/Analytics/SalesAnalysis`|Demo, L|Không mở rộng contract CRM demo; thay bằng feature HRM context riêng khi cần.|
|`/ContactDetails`, `/ContactList` — `UiDemo/Contacts`|Demo, L|Không dùng model/contact provider làm dependency cho HRM; đưa vào backlog xóa/di trú riêng.|
|`/Scheduler`, `/TaskList` — `UiDemo/Planning`|Demo, L|Giữ demo độc lập; product hóa phải tạo bounded context, endpoint và authorization mới.|

## 6. UI không route: cách áp dụng theo từng loại

|Phạm vi UI|Kế hoạch hành động|
|---|---|
|70 dialog/form/detail component trong folder feature|Được triển khai cùng page owner ở mục 5: parameter/callback rõ ràng, validation trong form, không inject persistence, không duplicate business rule của parent/service.|
|`Layout/`, `Shared/`, `SharedUi/`|Chỉ giữ presentation/cross-cutting UI. API của component nhỏ, không biết feature payroll/attendance cụ thể; test contract render/loading/error.|
|`DangTrienKhai`, `NhanSuPlaceholder`, `TinhLuongPlaceholder`, `TongQuanPlaceholder`|Không gắn data/business logic tạm. Khi product hóa, thay bằng page work item có context key và screen spec.|
|`Vnta.Hrm.Web/Components/Account/**`, `Pages/Error.razor`, `App.razor`|Lane Identity/host: giữ boundary Identity, authorization và error handling; không nhập vào rollout HRM trừ khi có yêu cầu security UX riêng.|

## 7. Trình tự rollout

1. **Baseline và audit (mỗi UI):** chạy checklist mục 4, ghi current/target
   boundary, context key, technical alias, gap và test baseline vào screen doc.
2. **P0 payroll và external/admin:** làm theo từng feature folder, bắt đầu từ các
   màn cùng họ với `KhauTruKhac`; tách query/command/lock/concurrency trước khi
   đổi markup lớn.
3. **P1 attendance, approval, nhân sự:** chuẩn hóa provider/contract, reload
   flow, server-side filter và form validation.
4. **P2 master data/dashboard:** hội tụ naming, CRUD contract, error/empty
   state và concurrency phù hợp.
5. **Shared/UI demo/Identity:** chỉ refactor khi có ticket độc lập; không để
   thay đổi ở các lane này gây trễ nghiệp vụ P0/P1.

Definition of done cho một hàng kế hoạch là: checklist mục 4 hoàn tất, test
luồng chính/nhánh lỗi có bằng chứng, screen doc và implementation log cập nhật,
và nợ kỹ thuật chưa đóng được ghi vào `doc/project/refactor-gap-register.md`.
