# Lộ Trình Refactor Solution HRM

Tài liệu này chốt kế hoạch refactor `src/Vnta.HRM2026` từ baseline demo hiện tại sang solution HRM có các project thành phần rõ ràng, bám theo hướng kiến trúc đã mô tả trong [`target-solution-structure.md`](./target-solution-structure.md).

## Mục tiêu của roadmap

- Biến `Vnta.HRM2026` từ source demo CRM thành source HRM có thể mở rộng lâu dài.
- Tách dần UI, nghiệp vụ, persistence và tích hợp ngoài ra khỏi cùng một project host.
- Tạo thứ tự triển khai an toàn để đội có thể refactor từng lượt mà không mất hướng.

## Baseline kỹ thuật chốt cho roadmap này

- Source chính thức của HRM: `src/Vnta.HRM2026`
- Runtime baseline: `.NET 10`
- UI framework baseline: `DevExpress 26.1.x` với package đang pin `26.1.3`
- Gateway attendance vẫn nằm riêng ở `src/zkteco-adms-gateway`

## Hiện trạng source cần xuất phát

Solution hiện tại mới có hai project:

- `Vnta.Hrm.Web`
  - đang gánh host, Identity, EF Core, migration và static assets
- `Vnta.Hrm.Web.Client`
  - đang gánh layout interactive, models demo, data providers và các module CRM mẫu

Những điểm cần xem là nợ kỹ thuật gốc:

- Naming solution và project chính đã chuyển sang HRM, nhưng nội dung feature và module vẫn còn nhiều dấu vết CRM demo.
- `ApplicationDbContext` hiện mới phục vụ Identity và demo runtime.
- `Analytics`, `Contacts`, `Planning` là module mẫu, không phải bounded context HRM.
- Chưa có project `Domain`, `Application`, `Infrastructure` để neo nghiệp vụ dài hạn.

## Bộ project đích cần có

### `Vnta.Hrm.Domain`

Chứa:

- entity nghiệp vụ HRM
- value object
- enum
- domain rule thuần

### `Vnta.Hrm.Application`

Chứa:

- use case
- DTO
- validation
- interface service/repository
- orchestration nghiệp vụ

### `Vnta.Hrm.Infrastructure`

Chứa:

- `DbContext` nghiệp vụ
- Identity implementation
- EF Core configuration và migration
- repository implementation
- integration với gateway, email, file storage và background job

### `Vnta.Hrm.Web`

Chứa:

- host ASP.NET Core
- composition root
- account pages
- shell, route và shared UI host

### `Vnta.Hrm.Web.Client`

Chứa:

- phần interactive WebAssembly nếu còn cần giữ mô hình hybrid
- feature components chạy client
- client-side services thuần UI

### `Vnta.Hrm.Tests.*`

Tách tối thiểu thành:

- `Vnta.Hrm.Tests.Unit`
- `Vnta.Hrm.Tests.Integration`
- `Vnta.Hrm.Tests.Web`

## Những project chưa cần tách ngay

Chưa nên tạo từ đầu nếu chưa có nhu cầu thật:

- `Vnta.Hrm.Api`
- `Vnta.Hrm.Worker`
- `Vnta.Hrm.Contracts`
- `Vnta.Hrm.SharedKernel`

## Mapping từ baseline hiện tại sang source đích

### Host và account

- `Vnta.Hrm.Web/Program.cs` tiếp tục là composition root của host
- `Vnta.Hrm.Web/Components/Account/` tiếp tục là vùng account và identity UI
- `Vnta.Hrm.Web/Components/App.razor` tiếp tục là điểm vào shell
- `Vnta.Hrm.Web/wwwroot/` tiếp tục chứa static assets của host

### Persistence và Identity

- `Vnta.Hrm.Web/Data/ApplicationDbContext.cs` -> `Vnta.Hrm.Infrastructure/Data/`
- `Vnta.Hrm.Web/Data/ApplicationUser.cs` -> `Vnta.Hrm.Infrastructure/Identity/`
- `Vnta.Hrm.Web/Data/Migrations/` -> `Vnta.Hrm.Infrastructure/Data/Migrations/`

### Interactive client

- `Vnta.Hrm.Web.Client/Components/Layout/` tiếp tục là shell interactive
- `Vnta.Hrm.Web.Client/Components/Routes.razor` tiếp tục là route map phía client
- `Vnta.Hrm.Web.Client/Services/` tiếp tục chứa service thuần UI

### Demo CRM modules

- `Analytics/`
- `Contacts/`
- `Planning/`

Ba module này không nên chuyển nguyên xi sang tên HRM. Chúng chỉ nên được:

- dùng làm tham chiếu UI/DevExpress pattern khi cần
- hoặc xóa dần khi module HRM thật thay thế xong

## Feature pilot nên dùng để kéo cấu trúc mới

Feature pilot khuyến nghị là `MayChamCong`.

Lý do:

- Repo đã có tài liệu phân tích nghiệp vụ và UI cho màn này.
- Repo đã có gateway attendance riêng để đối chiếu model tích hợp.
- Đây là một CRUD đủ thật để ép lộ ranh giới giữa `Web`, `Application`, `Infrastructure`.
- Màn này dùng được blueprint danh sách HRM chuẩn, nên thích hợp làm mẫu cho các module sau.

Tài liệu liên quan:

- [`attendance-device-management-screen.md`](./attendance-device-management-screen.md)
- [`hrm-list-screen-blueprint.md`](./hrm-list-screen-blueprint.md)

## Lộ trình thực thi đề xuất

## Phase 1: Rename kỹ thuật và chốt baseline HRM

Trạng thái hiện tại:

- đã đổi solution hiện hành sang `Jifeng.Hrm.slnx`
- đã đổi hai project hiện hành sang `Vnta.Hrm.Web` và `Vnta.Hrm.Web.Client`
- đã đổi namespace/usings chính sang ngữ cảnh HRM
- đã cập nhật docs/cấu hình chính để repo coi `src/Vnta.HRM2026` là source HRM chính thức

Mục tiêu:

- chốt naming HRM ở mức solution, project và namespace
- giữ nguyên baseline chạy hiện tại càng nhiều càng tốt
- cô lập các dấu vết CRM cũ vào tài liệu lịch sử hoặc roadmap nếu còn cần tham chiếu

Đầu việc:

- chuẩn hóa đường dẫn solution hiện hành là `src/Vnta.HRM2026/Jifeng.Hrm.slnx`
- chuẩn hóa host hiện hành là `src/Vnta.HRM2026/Vnta.Hrm.Web`
- chuẩn hóa interactive client hiện hành là `src/Vnta.HRM2026/Vnta.Hrm.Web.Client`
- dọn text, asset path và namespace còn gắn CRM

## Phase 2: Dựng skeleton project thành phần

Mục tiêu:

- tạo xương sống solution nhiều project nhưng chưa ép tách nghiệp vụ lớn ngay

Đầu việc:

- tạo `Vnta.Hrm.Domain`
- tạo `Vnta.Hrm.Application`
- tạo `Vnta.Hrm.Infrastructure`
- nối project reference đúng chiều phụ thuộc
- giữ `Vnta.Hrm.Web` là composition root duy nhất

## Phase 3: Bóc nền tảng ra khỏi Web

Mục tiêu:

- đẩy persistence và Identity về đúng chỗ trước khi làm module HRM thật

Đầu việc:

- chuyển `ApplicationDbContext`, `ApplicationUser`, migrations và Identity helper sang `Infrastructure`
- để `Web` chỉ còn đăng ký DI và endpoint
- tách các options/service cấu hình không thuần UI ra khỏi host page/component

## Phase 4: Làm feature pilot `MayChamCong`

Mục tiêu:

- chứng minh cấu trúc mới bằng một feature HRM thật

Đầu việc:

- tạo module `Attendance` theo cả `Application`, `Infrastructure`, `Web`
- tạo list screen + popup form theo blueprint HRM
- dùng gateway model như nguồn tham chiếu tích hợp, không copy nguyên semantics kỹ thuật vào UI

## Phase 5: Mở rộng các bounded context HRM

Thứ tự ưu tiên khuyến nghị:

1. `Organizations`
2. `Employees`
3. `Attendance`
4. `Leave`
5. `Contracts`
6. `Payroll`
7. `Security`

## Việc không nên làm trong roadmap này

- Không copy nguyên module demo `Contacts` rồi đổi caption thành `Nhân sự`.
- Không kéo `DbContext` nghiệp vụ mới tiếp tục nằm trong `Web`.
- Không tạo quá sớm `Api`, `Worker`, `SharedKernel` nếu chưa có nhu cầu thật.
- Không refactor tất cả module cùng lúc trong một lượt.

## Thứ tự triển khai khuyến nghị cho các lượt tiếp theo

1. Hoàn tất rename kỹ thuật và chốt `Vnta.HRM2026` là baseline HRM chính thức.
2. Tạo skeleton `Domain`, `Application`, `Infrastructure`.
3. Chuyển Identity và persistence nền sang `Infrastructure`.
4. Xây `MayChamCong` làm module pilot.
5. Sau khi pilot ổn, nhân mẫu sang `Organizations` và `Employees`.

## Kết quả mong đợi sau roadmap

Sau khi hoàn thành roadmap này:

- repo có naming HRM nhất quán
- source không còn phụ thuộc tư duy CRM demo
- mỗi feature mới có nơi đặt code rõ ràng
- đội có thể refactor và thêm nghiệp vụ HRM theo từng slice mà không phá cấu trúc



