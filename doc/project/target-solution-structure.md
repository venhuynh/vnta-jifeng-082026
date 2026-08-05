# Cấu Trúc Solution Đích

Tài liệu này mô tả cấu trúc source và solution đích cho Vnta HRM theo ba giai đoạn phát triển, dựa trên baseline đang hoạt động của repo.

1. Hiện tại
2. Refactor tiếp trong `Vnta.HRM2026`
3. Khi hệ thống lớn

Mục tiêu của tài liệu là giúp nhóm giữ cùng một hướng kiến trúc khi thêm module mới, tránh để UI, Identity và truy cập dữ liệu tiếp tục dồn vào một chỗ.

## Ghi chú quan trọng

- Từ `2026-06-29`, source HRM chính thức của repo là `src/Vnta.HRM2026`.
- Thư mục `src/Vnta.HRM` đã bị loại khỏi repo.
- Solution HRM hiện hành là `src/Vnta.HRM2026/Vnta.Hrm.slnx`.
- Baseline công nghệ chính thức là `.NET 10` và DevExpress `26.1.x`.
- Runtime database là PostgreSQL/Npgsql, dùng chung database `vnta-2026` với gateway attendance.

## Nguyên tắc chung

- `Domain` chỉ chứa mô hình và quy tắc nghiệp vụ thuần.
- `Application` chứa use case, DTO, validation, interface và orchestration nghiệp vụ.
- `Infrastructure` chứa persistence, Identity, migration, tích hợp ngoài và triển khai interface.
- `Web` chứa host, account/runtime, route, layout và composition root.
- `Web.Client` giữ phần UI interactive chạy trong browser khi mô hình hybrid vẫn còn được giữ.
- UI ưu tiên gọi service hoặc use case rõ ràng, không dồn nghiệp vụ lâu dài vào helper của template demo.

## Giai đoạn 1: Hiện tại

### Mục tiêu

Ghi nhận đúng hiện trạng của source HRM2026 sau khi đã tách skeleton layer, chốt startup host và ổn định PostgreSQL/Npgsql.

### Cây project hiện tại

```text
src/Vnta.HRM2026/
  Vnta.Hrm.slnx
  Directory.Packages.props

  Vnta.Hrm.Domain/
    Common/
    Employees/
    Organizations/
    Attendance/
    Leave/
    Payroll/
    Contracts/
    Security/

  Vnta.Hrm.Application/
    Common/
    Employees/
    Organizations/
    Attendance/
    Leave/
    Payroll/
    Contracts/
    Security/

  Vnta.Hrm.Infrastructure/
    Data/
      ApplicationDbContext.cs
      Migrations/
    Identity/
    Repositories/
    Integrations/
    BackgroundJobs/
    DependencyInjection.cs

  Vnta.Hrm.Web/
    Components/
      Account/
      Pages/
    Properties/
      launchSettings.json
    wwwroot/
    Program.cs

  Vnta.Hrm.Web.Client/
    Components/
      Analytics/
      Contacts/
      Layout/
      Planning/
    Models/
    Services/
    Tools/
    Program.cs
```

### Quy ước áp dụng ngay

- Mọi thay đổi HRM mới phải được thực hiện trong `src/Vnta.HRM2026`.
- Không tạo lại source mới dưới `src/Vnta.HRM`.
- Khi cập nhật tài liệu hiện hành, dùng đường dẫn `src/Vnta.HRM2026/...`.
- `Vnta.Hrm.Web` là startup host để debug.
- `Vnta.Hrm.Web.Client` là client assembly của mô hình hybrid, không phải startup project độc lập.
- `Infrastructure` là nơi sở hữu `ApplicationDbContext`, Identity support, migrations và cấu hình PostgreSQL.
- Nếu cần luồng đồng bộ PostgreSQL-to-PostgreSQL chạy độc lập, ưu tiên để nó ở solution riêng cạnh `Vnta.HRM2026`, hiện tại là `src/Vnta.PostgresSync`.

### Khoản nợ kỹ thuật đang chấp nhận ở giai đoạn này

- Nhiều feature và module trong UI vẫn là baseline CRM demo.
- `Domain` và `Application` đã có module shells, nhưng chưa có nghiệp vụ HRM thật.
- Không giữ bootstrap account hoặc credential demo trong host/client runtime.
- Chưa có bộ test project riêng cho `Domain`, `Application`, `Infrastructure` và `Web`.
- Chưa chốt cuối cùng việc giữ hay gom cặp `Web` + `Web.Client`.

## Giai đoạn 2: Refactor tiếp trong `Vnta.HRM2026`

### Mục tiêu

Giữ naming HRM đã chốt, dọn tiếp dấu vết CRM demo, và đưa module HRM thật đầu tiên vào các layer đã tạo.

### Cây project đích gần hạn

```text
src/Vnta.HRM2026/
  Vnta.Hrm.slnx

  Vnta.Hrm.Domain/
    Common/
    Employees/
    Organizations/
    Attendance/
    Leave/
    Payroll/
    Contracts/
    Security/

  Vnta.Hrm.Application/
    Common/
    Employees/
    Organizations/
    Attendance/
    Leave/
    Payroll/
    Contracts/
    Security/

  Vnta.Hrm.Infrastructure/
    Data/
    Identity/
    Repositories/
    Integrations/
    BackgroundJobs/

  Vnta.Hrm.Web/
    Components/
      Account/
      Layout/
      Shared/
    Pages/
    wwwroot/
    Program.cs

  Vnta.Hrm.Web.Client/
    Features/
      Employees/
      Organizations/
      Attendance/
      Leave/
      Payroll/
      Contracts/
      Security/
    Shared/
    Services/
    Program.cs
```

### Thay đổi kiến trúc nên thực hiện ở giai đoạn này

- Đổi các page và model demo không thuộc HRM sang feature folder theo nghiệp vụ thật.
- Chọn một module HRM đầu tiên để làm pilot, ví dụ `Employees`, `Organizations` hoặc `Attendance`.
- Đưa entity, rule nghiệp vụ, DTO, use case và abstraction thật vào `Domain` và `Application`.
- Giữ `Infrastructure` là nơi duy nhất chứa EF Core, migration và tích hợp database.
- Làm rõ role của `Web` và `Web.Client`, hoặc chốt kế hoạch gom lại nếu hybrid không còn cần.

### Dấu hiệu hoàn thành giai đoạn 2

- Repo có thể được hiểu ngay là dự án HRM, không còn nhầm với demo CRM.
- Các module mới bắt đầu đi theo feature folder rõ ràng và đúng tên nghiệp vụ.
- `Domain`, `Application` và `Infrastructure` chứa logic thật thay vì chỉ là skeleton.
- Tài liệu hiện hành, setup guide và source map đều trỏ đúng `Vnta.HRM2026`.

## Giai đoạn 3: Khi hệ thống lớn

### Mục tiêu

Khi HRM có nhiều phân hệ, workflow phức tạp, phân quyền nhiều tầng và cần kiểm thử độc lập mạnh hơn, solution nên được tổ chức rõ hơn theo bounded context và host responsibility.

### Cây solution đích

```text
src/
  Vnta.HRM2026/
    Vnta.Hrm.Domain/
      Common/
      Employees/
      Organizations/
      Attendance/
      Leave/
      Payroll/
      Contracts/
      Security/

    Vnta.Hrm.Application/
      Common/
      Employees/
      Organizations/
      Attendance/
      Leave/
      Payroll/
      Contracts/
      Security/

    Vnta.Hrm.Infrastructure/
      Data/
        Configurations/
        Interceptors/
        Migrations/
      Identity/
      Repositories/
      Integrations/
        Email/
        FileStorage/
        AttendanceGateway/
      BackgroundJobs/

    Vnta.Hrm.Web/
      Components/
        Layout/
        Shared/
      Features/
        Employees/
        Organizations/
        Attendance/
        Leave/
        Payroll/
        Contracts/
        Security/
      Program.cs

    Vnta.Hrm.Web.Client/
      Features/
      Shared/
      Program.cs

    Vnta.Hrm.Tests.Unit/
    Vnta.Hrm.Tests.Integration/
    Vnta.Hrm.Tests.Web/
```

### Ý nghĩa của các project bổ sung

- `Vnta.Hrm.Domain/`
  - chứa entity, value object, enum và rule nghiệp vụ thuần
- `Vnta.Hrm.Application/`
  - chứa use case, DTO, validation, interface và orchestration
- `Vnta.Hrm.Infrastructure/`
  - chứa EF Core, Identity, persistence và tích hợp ngoài
- `Vnta.Hrm.Web/`
  - chứa host, account, layout và composition root
- `Vnta.Hrm.Web.Client/`
  - giữ phần UI interactive nếu vẫn cần WebAssembly trong giai đoạn chuyển tiếp
- `Vnta.Hrm.Tests.*`
  - hỗ trợ test domain, integration và UI

### Khi nào nên tách hơn nữa

- Có nhu cầu background processing riêng cho payroll, sync chấm công hoặc gửi thông báo hàng loạt.
- Có API riêng cho mobile hoặc tích hợp bên thứ ba.
- Một bounded context có tốc độ thay đổi nhanh hơn phần còn lại.

Khi đó có thể cân nhắc:

```text
src/
  Vnta.HRM2026/
    Vnta.Hrm.Api/
    Vnta.Hrm.Worker/
    Vnta.Hrm.SharedKernel/
```

Nhưng chỉ nên tách khi đã có nhu cầu thật, không tách sớm để tránh tăng chi phí điều phối.

## Đề xuất thực thi theo thứ tự

1. Giữ tài liệu, setup guide và source map đồng bộ với `src/Vnta.HRM2026`.
2. Chọn module HRM pilot để đưa nghiệp vụ thật vào `Domain`, `Application`, `Infrastructure`.
3. Dọn bớt baseline demo CRM trong `Vnta.Hrm.Web.Client`.
4. Chốt chiến lược dài hạn cho `Vnta.Hrm.Web` và `Vnta.Hrm.Web.Client`.
5. Tạo bộ test project khi module nghiệp vụ đầu tiên ổn định.

Để có thứ tự triển khai chi tiết hơn theo từng phase và từng project, dùng thêm tài liệu:

- [`refactor-roadmap.md`](./refactor-roadmap.md)

## Kết luận

Cấu trúc đích của repo không còn xuất phát từ `src/Vnta.HRM`. Điểm xuất phát mới là `src/Vnta.HRM2026`, đi theo ba bước:

- ngắn hạn: ghi đúng hiện trạng `Vnta.HRM2026` và giữ docs khớp với repo
- trung hạn: đưa module HRM thật vào các layer đã tạo và dọn baseline demo
- dài hạn: tách thêm project test, API hoặc worker khi nhu cầu nghiệp vụ đã đủ rõ
