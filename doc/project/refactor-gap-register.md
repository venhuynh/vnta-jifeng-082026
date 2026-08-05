# Refactor Gap Register

Tài liệu này ghi lại các khoảng cách giữa source hiện trạng và chuẩn đích trong
`doc/project/hrm-refactor-standard.md`.

Mục tiêu:

- gom nợ kỹ thuật kiến trúc vào một chỗ
- ưu tiên hóa theo mức ảnh hưởng
- giúp sprint refactor sau này bám cùng một backlog

Trạng thái mặc định:

- `Open`: chưa xử lý
- `In Progress`: đang có branch hoặc sprint xử lý
- `Closed`: đã đóng gap trong source và tài liệu

Mức ưu tiên:

- `P0`: cần đóng sớm vì ảnh hưởng trực tiếp đến data integrity hoặc rollout
- `P1`: quan trọng, nên đưa vào sprint refactor gần nhất
- `P2`: nên xử lý sau khi boundary chính đã ổn định

## GAP-001 - Chưa thống nhất boundary HTTP cho các màn `InteractiveServer`

- Ưu tiên: `P1`
- Trạng thái: `Open`
- Hiện trạng:
  - một số màn cũ vẫn đi theo luồng:
    `UI -> provider -> application service -> database`
  - chưa có endpoint HTTP riêng cho từng workflow ở toàn bộ nhóm màn `InteractiveServer`
  - `NhanVien` đã hội tụ về:
    `UI -> provider -> typed client -> endpoint -> service -> database`
- Ảnh hưởng:
  - contract UI/server khó đối chiếu đồng nhất giữa các màn
  - khó chia sẻ pattern chung với các màn payroll đã đi qua endpoint
- Ví dụ tham chiếu:
  - `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
- Hướng đóng gap:
  - giữ pattern chuyển tiếp cho màn đang ổn định
  - mỗi màn mới ưu tiên đi qua endpoint rõ ràng
  - khi refactor màn cũ, quyết định rõ màn nào sẽ hội tụ về HTTP boundary

## GAP-002 - Search, filter, paging chưa đẩy lên server ở một số list screen

- Ưu tiên: `P1`
- Trạng thái: `Open`
- Hiện trạng:
  - một số list screen cũ vẫn còn search hoặc filter cục bộ trên tập dữ liệu đã tải về
  - `NhanVien` đã chuyển sang flow `DxSearchBox -> EmployeeFilter -> summary/list query server-side`
  - chưa phải tất cả list screen đều có filter contract server-side tương đương
- Ảnh hưởng:
  - tăng tải dữ liệu UI khi tập dữ liệu lớn
  - khó mở rộng filter nghiệp vụ sau này
- Ví dụ tham chiếu:
  - `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
- Hướng đóng gap:
  - tạo filter contract theo từng feature thay vì để grid tự search
  - đưa search/filter/paging sang service và contract server-side
  - giữ `DxSearchBox` là trigger UI, không giữ search engine ở component

## GAP-003 - Schema guard runtime đang chạy trong service persistence

- Ưu tiên: `P0`
- Trạng thái: `Open`
- Hiện trạng:
  - `DatabaseEmployeeService` và `DatabaseEmployeeRefreshService` đang gọi
    `EnsureSoftDeleteColumnsAsync(...)`
  - `DatabaseMealAllowanceService` cũng có logic guard/cập nhật schema runtime
- Ảnh hưởng:
  - trộn boundary giữa migration và runtime request
  - tăng rủi rõ vận hành và làm mờ ownership của `Infrastructure`
- Ví dụ tham chiếu:
  - `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
  - `doc/project/hrm-refactor-standard.md`
- Hướng đóng gap:
  - đưa thay đổi schema về migration được review
  - để service chỉ còn validate data và persistence
  - giữ runtime guard chỉ nếu có lý do vận hành đặc biệt và có deadline loại bỏ

### Cập nhật 2026-07-28 — Phụ cấp thâm niên

- `DatabasePayrollEmployeeSeniorityAllowanceService` không còn gọi compatibility schema guard từ prepare, query hoặc mutation runtime; migration `20260728092713_AddSeniorityAllowanceWorkdaySnapshots` là owner schema cho feature này.
- Khối compatibility SQL cũ và private handler `Legacy*` endpoint chưa được xóa trong lượt này để không che giấu nhu cầu xác nhận rollout migration; theo dõi cleanup này dưới GAP-003/GAP-008.

## GAP-004 - Chưa có chiến lược concurrency nhất quán cho workflow update

- Ưu tiên: `P1`
- Trạng thái: `Open`
- Hiện trạng:
  - `NhanVien` đã mở luồng `Điều chỉnh` và đang dùng optimistic concurrency theo `OriginalUpdatedAtUtc`
  - nhiều màn khác chưa ghi rõ có cần `RowVersion` hay optimistic concurrency không
  - chuẩn `RowVersion` native vẫn chưa được rollout nhất quán toàn repo
- Ảnh hưởng:
  - dễ phát sinh ghi đè khi nhiều người sửa cùng lúc
  - khó review PR vì thiếu tiêu chí conflict handling
- Ví dụ tham chiếu:
  - `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
- Hướng đóng gap:
  - với mỗi màn có update, quyết định rõ token concurrency
  - cập nhật contract request/response và thông điệp UI khi xảy ra conflict
  - nếu cần cứng hơn chuẩn hiện tại của `NhanVien`, nâng dần từ `UpdatedAtUtc` lên `RowVersion`

## GAP-005 - Unique constraint quan trọng chưa đóng ở database

- Ưu tiên: `P0`
- Trạng thái: `Open`
- Hiện trạng:
  - duplicate `EmployeeCode` hiện đang chặn chủ yếu ở service level
  - chưa thấy unique constraint database rõ ràng cho `employees`
- Ảnh hưởng:
  - service check không đủ để bảo đảm toàn vẹn dữ liệu trong mọi tình huống
  - dễ phát sinh race condition khi có thao tác song song
- Ví dụ tham chiếu:
  - `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
- Hướng đóng gap:
  - rà soát lại entity configuration và migration
  - bổ sung unique index hoặc unique constraint phù hợp với soft delete policy

## GAP-006 - Chain command payroll snapshot chưa khép kín

- Ưu tiên: `P1`
- Trạng thái: `Resolved`
- Hiện trạng:
  - `PhuCapCom` đã tách snapshot table riêng
  - command `refresh` từ attendance cập nhật projection tổng hợp trong cùng transaction;
    hai action sync riêng đã được loại khỏi UI và API vì không còn nằm trong workflow hiện hành
- Ảnh hưởng:
  - chain snapshot được khép kín ở source; vẫn cần smoke test trên dữ liệu attendance thật
- Ví dụ tham chiếu:
  - `doc/screens/phu-cap/phu-cap-com.md`
  - `doc/sprints/PhuCap/sprint-022-phu-cap-com-backend-refactor/sprint-plan.md`
- Hướng đóng gap:
  - thực hiện smoke test với dữ liệu attendance thật trước khi xem là chứng cứ vận hành hoàn tất

## GAP-007 - Bộ screen implementation đọc chưa rollout hết các màn quan trọng

- Ưu tiên: `P2`
- Trạng thái: `In Progress`
- Hiện trạng:
  - đã có template
  - đã có example điền sẵn cho `NhanVien`
  - nhiều màn khác chưa có đọc cùng mức chi tiết
- Ảnh hưởng:
  - review refactor giữa các màn chưa đồng đều
  - khó so sánh hiện trạng và chuẩn đích trên phạm vi rộng
- Ví dụ tham chiếu:
  - `doc/templates/screen-implementation-template.md`
  - `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
- Hướng đóng gap:
  - rollout tiếp cho các màn ưu tiên:
    - `Phòng ban`
    - `Chức vụ`
    - `PhuCapTongHop`
    - `MayChamCong`

## GAP-008 - Folder và naming chưa thống nhất theo cùng context key

- Ưu tiên: `P1`
- Trạng thái: `Open`
- Hiện trạng:
  - UI, provider, endpoint và service của cùng một feature chưa luôn dùng cùng một
    tên ngữ cảnh
  - `NhanVien` và `Employee` đang song song
  - `PhuCapCom` và `MealAllowance` đang song song
  - endpoint vẫn bị dồn vào file lớn theo module như `PayrollEndpoints.cs`
  - `KhauTruKhac` đã tách endpoint `/other-deductions/*` sang
    `Endpoints/KhauTru/KhauTruKhac/KhauTruKhacEndpoints.cs`; các feature payroll
    còn lại vẫn cần rollout theo cùng hướng.
- Ảnh hưởng:
  - khó tìm toàn bộ file liên quan của một feature
  - khó refactor và review ownership theo SOLID
  - team mới vào repo mất thêm thời gian để học mapping tên
- Ví dụ tham chiếu:
  - `doc/project/feature-folder-standard.md`
  - `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
  - `doc/screens/phu-cap/phu-cap-com.md`
- Hướng đóng gap:
  - chốt `context key` bằng tiếng Việt không dấu cho từng feature quan trọng
  - xem `Employee`, `MealAllowance` là `technical alias` thay vì tên quản lý chính
  - đưa feature mới vào folder theo context key ngay từ đầu
  - với feature refactor lớn, đổi tên folder/file theo đợt thay vì để lại half-old
    half-new
  - tách endpoint về file riêng theo feature context

## GAP-009 - Backend security baseline chưa được đóng theo policy và trust boundary

- Ưu tiên: `P0`.
- Trạng thái: `In Progress`.
- Hiện trạng:
  - Có các phát hiện `SEC-001` đến `SEC-007` về secret/bootstrap admin, gateway inbound, SignalR monitor, authorization boundary, brute-force protection và security test.
  - Payroll/Attendance đang có route group chỉ yêu cầu người dùng đã đăng nhập thay vì capability nghiệp vụ cụ thể.
- Ảnh hưởng:
  - Có nguy cơ chiếm quyền quản trị, lộ dữ liệu chấm công/lương hoặc thao tác ngoài quyền khi endpoint bị gọi trực tiếp.
- Hướng đóng gap:
  - Thực hiện `doc/sprints/Security/sprint-023-backend-security-refactor/` theo P0 rồi P1.
  - Chỉ chuyển sang `Closed` khi toàn bộ phát hiện liên quan được `Verified` sau test và credential cũ đã được xoay vòng.

## GAP-010 - Quyết định search tiếng Việt cho Phụ cấp trách nhiệm

- Ưu tiên: `P1`.
- Trạng thái: `Needs Decision`.
- Hiện trạng:
  - Màn `/payroll/responsibility-allowances` đã dùng query/paging server-side qua
    `PhuCapTrachNhiemEndpoints.cs` và provider hẹp theo workflow.
  - Màn `/payroll/responsibility-allowances/employee-assignments` cũng đã dùng
    query/paging server-side với cùng boundary và vẫn phụ thuộc collation của DB.
  - Toàn bộ route configuration/query/command/export đã hội tụ vào
    `PhuCapTrachNhiemEndpoints.cs`; `PayrollEndpoints.cs` chỉ gọi mapper context.
  - Batch lock có concurrency token; export có server-side allowlist và audit.
  - Search server-side chưa có bằng chứng collation hoặc `unaccent` tương đương
    normalize không dấu trước đây của UI.
- Ảnh hưởng:
  - Người dùng có thể nhận trải nghiệm tìm kiếm khác khi gõ không dấu nếu database
    chưa có collation phù hợp.
- Hướng đóng gap:
  - Xác nhận giải pháp search tiếng Việt tại database; chỉ thêm extension/migration
    khi có quyết định vận hành, kiểm thử query plan và quyền cài extension phù hợp.

## Cách dùng trong sprint sau

Khi mở sprint refactor:

1. chọn gap cần đóng
2. link gap vào sprint plan
3. cập nhật screen implementation đọc của màn liên quan
4. chỉ đóng gap khi source, tài liệu và implementation log đều khớp

## Nguồn liên quan

- `doc/project/hrm-refactor-standard.md`
- `doc/project/refactor-standard-document-plan.md`
- `doc/checklists/screen-implementation-principles.md`
- `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`



