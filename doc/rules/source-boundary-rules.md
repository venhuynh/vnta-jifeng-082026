# Quy Tắc Ranh Giới Source Và Kiến Trúc

Áp dụng cho mọi thay đổi code, tài liệu và quyết định đặt file trong repo HRM sau
khi source chính được chuyển sang `src/Vnta.HRM2026`.

## 1. Source chính của HRM

- Source root đang hoạt động của HRM là `src/Vnta.HRM2026`.
- Solution HRM đang hoạt động là `src/Vnta.HRM2026/Jifeng.Hrm.slnx`.
- Mọi code HRM mới phải bám theo cây source này.
- `src/Vnta.HRM` không còn là source hoạt động; nếu xuất hiện trong tài liệu cũ thì
  xem đó là tham chiếu lịch sử.

## 2. Không viết mới vào đường dẫn legacy

- Không tạo file mới, hướng dẫn mới hoặc implementation note mới trỏ về
  `src/Vnta.HRM/...`.
- Khi cập nhật tài liệu, dùng đường dẫn `src/Vnta.HRM2026/...`.
- Nếu cần nhắc lại đường dẫn cũ để giải thích lịch sử, phải ghi rõ đó là
  `legacy reference` hoặc `tham chiếu lịch sử`.

## 3. Tôn trọng solution `.slnx`

- Khi tài liệu nói về solution HRM, dùng `Jifeng.Hrm.slnx` làm mốc chính.
- Không viết rule, checklist hay hướng dẫn mới giả định repo đang dùng `.sln`
  cũ nếu source hiện tại không còn như vậy.

## 4. Ranh giới trách nhiệm giữa Web và Web.Client

`Vnta.Hrm.Web`

- giữ `Program.cs`, composition root, HTTP pipeline, Identity, host config và
  server-side integration
- là nơi map Razor Components và nạp thêm assembly UI client

`Vnta.Hrm.Web.Client`

- giữ page interactive, layout, component UI, client service và helper phục vụ UI
- không nên trở thành nơi chứa logic persistence, hạ tầng server hoặc rule auth
  cookie của host

Khi sửa một hành vi, phải xác định trước nó thuộc host hay client.

## 5. Ranh giới giữa các layer nghiệp vụ

- `Vnta.Hrm.Domain`: entity, value object, enum và rule nghiệp vụ thuần
- `Vnta.Hrm.Application`: use case, DTO, validation, interface và orchestration
- `Vnta.Hrm.Infrastructure`: persistence, Identity implementation, repository và
  external integration
- `Vnta.Hrm.Web` và `Vnta.Hrm.Web.Client`: UI, route, layout, component và wiring

Không đẩy thêm nghiệp vụ dài hạn vào host/client chỉ vì hiện tại skeleton layer
đã có nhưng chưa dùng hết.

## 6. Module demo không phải đích đến của HRM

- `Analytics`, `Contacts`, `Planning` trong `Vnta.Hrm.Web.Client/Components/`
  hiện là baseline demo.
- Không thêm nghiệp vụ HRM mới bằng cách tiếp tục mở rộng trực tiếp các module
  demo này rồi chỉ đổi caption.
- Khi tạo module HRM thật, ưu tiên đặt theo ngữ cảnh nghiệp vụ đã chốt, ví dụ:
  `Employees`, `Organizations`, `Attendance`, `Leave`, `Payroll`, `Contracts`,
  `Security`.

## 7. Hướng đặt feature mới

- Feature HRM mới phải theo thư mục phản ánh ngữ cảnh nghiệp vụ thật, không theo
  tên màn hình mẫu từ demo CRM.
- Nếu đang ở giai đoạn chuyển tiếp, vẫn phải viết tài liệu và naming như thể
  feature đó sẽ sống lâu dài trong cây HRM thật.
- Không copy nguyên module `Contacts` hoặc `Planning` rồi xem đó là kiến trúc
  chuẩn cho HRM.

## 8. Runtime demo hiện tại không phải kiến trúc dữ liệu đích

- `UseInMemoryDatabase(...)` trong `Vnta.Hrm.Web/Program.cs` phản ánh runtime
  demo hiện tại.
- Cấu hình `DefaultConnection` và dấu vết SQL Server trong template không mặc
  định trở thành hướng phát triển dài hạn của HRM.
- Khi viết rule dữ liệu dài hạn cho HRM, vẫn bám hướng PostgreSQL đã chốt trong
  tài liệu dự án.

## 9. Attendance gateway là source riêng

- `src/zkteco-adms-gateway/` là source độc lập với web HRM.
- Không trộn rule host web HRM với rule gateway thiết bị nếu chưa có yêu cầu nối
  xuyên biên rõ ràng.
- Tài liệu tích hợp phải nói rõ thay đổi diễn ra ở HRM web, gateway, hay cả hai.

## 10. Quy tắc cập nhật tài liệu sau các lần pull lớn

- Sau khi pull thay đổi kiến trúc, phải rà lại:
  - `doc/project/source-map.md`
  - `doc/project/architecture.md`
  - `doc/project/target-solution-structure.md`
  - `doc/rules/`
  - `doc/checklists/`
- Nếu source thực tế và rule không còn khớp, cập nhật rule trước khi mở rộng
  feature mới.


