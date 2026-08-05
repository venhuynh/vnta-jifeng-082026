# Kiến Trúc Dự Án

Tài liệu này mô tả hướng kiến trúc nên áp dụng khi dự án HRM phát triển lớn hơn, đồng thời ghi đúng hiện trạng của app đang hoạt động.

## Hiện trạng

Source HRM hiện hành là `src/Vnta.HRM2026` với solution `src/Vnta.HRM2026/Jifeng.Hrm.slnx`, gồm:

- `Vnta.Hrm.Web/`: startup host, `Program.cs`, account UI, static assets, host-side components và composition root.
- `Vnta.Hrm.Web.Client/`: UI interactive chạy trong browser, routes, layout, model demo, services và các module demo `Analytics`, `Contacts`, `Planning`.
- `Vnta.Hrm.Domain/`: skeleton domain layer, đã có module folder `Employees`, `Organizations`, `Attendance`, `Leave`, `Payroll`, `Contracts`, `Security`.
- `Vnta.Hrm.Application/`: skeleton application layer, đã có module folder theo các bounded context HRM tương ứng.
- `Vnta.Hrm.Infrastructure/`: DI hạ tầng, `ApplicationDbContext`, `ApplicationUser`, migrations, identity support, repository placeholder và integration placeholder.

## Trạng thái runtime hiện tại

- `Vnta.Hrm.Web` là startup project để debug.
- `Vnta.Hrm.Web.Client` không phải startup project độc lập; nó được `Vnta.Hrm.Web` host nạp thêm như client assembly.
- `Program.cs` của host đang gọi `AddInfrastructureServices(...)`.
- Runtime database ưu tiên `ConnectionStrings:Postgres`.
- Repo có thêm solution console độc lập `src/Vnta.PostgresSync` để chạy luồng đồng bộ PostgreSQL-to-PostgreSQL ngoài `Vnta.HRM2026`.
- Luồng sync này được vận hành bằng command console và có tài liệu riêng tại `doc/setup/postgres-sync-console.md`.
- HRM đang dùng chung PostgreSQL với gateway attendance tại database Jifeng `jifeng_hrm`.
- Baseline công nghệ chính thức của source hiện hành là `.NET 10` và DevExpress `26.1.x`, hiện đang pin package `26.1.3`.
- Toàn solution đã build thành công với `0 Warning(s)` và `0 Error(s)` ngày `2026-06-29`.

## Đánh giá hiện trạng

Source hiện tại là một baseline tốt để phát triển tiếp, nhưng vẫn còn một số khoản nợ kỹ thuật rõ ràng:

- Naming solution và project chính đã là HRM, nhưng nhiều module UI bên trong vẫn mang dấu vết demo CRM.
- `Domain` và `Application` đã có skeleton theo bounded context, nhưng chưa có nghiệp vụ HRM thật.
- Runtime không còn bootstrap tài khoản demo; các helper template còn lại phải không tạo hoặc thay đổi dữ liệu bảo mật khi xử lý request.
- Chưa có project test độc lập cho domain, integration và web.
- Chưa chốt dài hạn việc giữ mô hình `Web` + `Web.Client` hay gom thành một host duy nhất.

## Hướng Clean Architecture khuyến nghị

Khi nghiệp vụ tăng lên, tiếp tục tách bạch theo các lớp:

```text
Domain
  Entity, value object, enum, rule nghiệp vụ thuần

Application
  Use case, DTO, validation, abstraction và orchestration

Infrastructure
  EF Core, Identity, repository, migration, external integration

Web / Web.Client
  Host, route, page, component, layout, view-state và DevExpress UI
```

## Tài liệu kiến trúc đích

Khi cần quyết định nên tổ chức solution như thế nào ở từng giai đoạn, dùng tài liệu chính:

- [`target-solution-structure.md`](./target-solution-structure.md)

Tài liệu này mô tả rõ:

- trạng thái hiện tại của baseline HRM2026
- bước refactor tiếp theo trong cùng solution
- cấu trúc nên có khi hệ thống HRM lớn hơn

## Nguyên tắc phụ thuộc

- UI gọi service hoặc use case ở lớp `Application` khi logic đó đã được đưa lên layer này.
- `Application` dùng `Domain`.
- `Infrastructure` triển khai interface của `Application` và sở hữu chi tiết kỹ thuật.
- `Domain` không phụ thuộc UI, EF Core hoặc DevExpress.
- Host `Web` không nên giữ nghiệp vụ lâu dài ngoài vai trò composition root và account/runtime host.

## Khi nào cần tách thêm project

Trong giai đoạn hiện tại, có thể tiếp tục giữ nhịp triển khai nhanh với baseline đang có. Nên tách thêm khi xuất hiện một trong các dấu hiệu:

- Nhiều nghiệp vụ HRM dùng chung logic cần test độc lập.
- Page bắt đầu chứa nhiều truy vấn và xử lý nghiệp vụ.
- Cần project test riêng cho `Domain`, `Application` hoặc `Web`.
- Cần background processing, API riêng, hoặc bounded context thay đổi độc lập nhanh.

## Quy tắc cho page Blazor

- Page chỉ điều phối UI và gọi service/use case.
- Không đặt công thức nghiệp vụ trực tiếp trong `.razor`.
- Không để data provider demo trở thành nơi chứa logic nghiệp vụ HRM dài hạn.
- Không truy vấn `DbContext` trực tiếp từ component nếu logic đó còn dùng lại nơi khác.
- Với form phức tạp, dùng model riêng thay vì bind trực tiếp entity.

## Quy tắc database

- `Infrastructure` là nơi sở hữu `ApplicationDbContext`, migrations và cấu hình provider.
- HRM hiện dùng PostgreSQL/Npgsql và dùng chung database Jifeng `jifeng_hrm` với gateway attendance.
- Migration Identity đã được chuẩn hóa lại theo PostgreSQL/Npgsql và đã được apply thành công.
- `ConnectionStrings:Postgres` là cấu hình ưu tiên cho runtime hiện tại của HRM.
- Các trường `DateTime` nghiệp vụ nên ưu tiên `timestamp without time zone` khi mở rộng model sang nghiệp vụ HRM thật.


