# Thiết Lập Môi Trường Local

Tài liệu này mô tả môi trường local khuyến nghị cho dự án Vnta HRM Blazor.

## Source chính hiện tại

- Source HRM hiện hành: `src/Vnta.HRM2026`
- Solution hiện hành: `src/Vnta.HRM2026/Vnta.Hrm.slnx`
- Project host: `src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj`
- Solution console đồng bộ PostgreSQL: `src/Vnta.PostgresSync/Vnta.PostgresSync.slnx`

Source `src/Vnta.HRM` không còn tồn tại trong repo. Nếu tài liệu cũ nhắc tới đường dẫn đó, hãy xem đó là tư liệu lịch sử.

## Công cụ cần có

- .NET SDK 10
- `global.json` ở root repo khóa baseline SDK .NET 10 cho toàn bộ dự án
- Workload WebAssembly cho .NET 10 nếu build báo thiếu `wasm-tools-net10`
- Visual Studio Insider 2026 hoặc công cụ tương thích với `.slnx`
- DevExpress Blazor license/package source phù hợp
- Git
- PostgreSQL có thể truy cập tới máy chủ dùng chung của gateway

## Cấu hình database hiện hành

HRM dùng chung PostgreSQL với `src/zkteco-adms-gateway`. Thông số kết nối và credential phải lấy từ môi trường cục bộ hoặc hệ thống secret, không ghi trong tài liệu hay source.

Thứ tự lấy connection string hiện hành của HRM:

1. `ConnectionStrings:Postgres`
2. `ConnectionStrings:DefaultConnection`
3. biến môi trường `VNTA_DB`

Nếu không có các giá trị trên, HRM và design-time DbContext factory phải dừng với lỗi cấu hình thay vì dùng fallback.

Các file cấu hình hiện tại:

- `src/Vnta.HRM2026/Vnta.Hrm.Web/appsettings.json`
- `src/Vnta.HRM2026/Vnta.Hrm.Web/appsettings.Development.json`
- `src/Vnta.HRM2026/Vnta.Hrm.Web/appsettings.Local.json` (không commit; tạo từ `appsettings.Local.example.json`)
- `src/Vnta.PostgresSync/Vnta.PostgresSync.Console/appsettings.json`

### Thiết lập bằng .NET User Secrets

Với local development, ưu tiên lưu connection string trong User Secrets của project host:

```powershell
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=<db-host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;Timezone=Asia/Ho_Chi_Minh" --project src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj
```

Lệnh trên không thay đổi file trong repository. Có thể kiểm tra key đã được lưu bằng
`dotnet user-secrets list --project src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj`, nhưng không đưa output chứa credential vào ticket, log hoặc tài liệu.

Tài liệu vận hành riêng cho solution console:

- `doc/setup/postgres-sync-console.md`

## Chạy source HRM hiện hành

Source `Vnta.HRM2026` yêu cầu cấu hình database cục bộ hoặc runtime:

- `Vnta.Hrm.Infrastructure/DependencyInjection.cs` cấu hình `ApplicationDbContext` bằng `UseNpgsql(...)`.
- Không có bootstrap tài khoản demo/admin trong runtime.
- URL debug mặc định của host hiện tại nằm trong `src/Vnta.HRM2026/Vnta.Hrm.Web/Properties/launchSettings.json`.

Vì vậy local run cơ bản của HRM cần một PostgreSQL phù hợp, nhưng không được phụ thuộc credential đã commit.

## PostgreSQL và gateway

`src/zkteco-adms-gateway` vẫn là source riêng nhưng hiện đang dùng cùng database PostgreSQL với HRM.

Khi tiếp tục refactor hoặc bổ sung migration cho HRM, cần đặc biệt lưu ý:

- không làm hỏng các bảng gateway đang sử dụng
- phân biệt rõ bảng Identity/HRM với bảng attendance gateway
- thống nhất quy tắc migration và naming trước khi tạo thêm schema nghiệp vụ HRM

## Quy tắc bảo mật cấu hình

- Không commit mật khẩu, token, key hoặc connection string thật.
- Không ghi secret vào tài liệu, log hoặc comment.
- Dùng `appsettings.Local.json` đã ignore, user secret, biến môi trường hoặc secret store triển khai.
- Dùng `appsettings.Local.example.json` làm mẫu tên key; chỉ thay placeholder ở file local không commit.
- Credential từng nằm trong source/config phải được xoay vòng bởi chủ sở hữu vận hành trước khi deploy.

## Quy tắc ngày giờ

- Gateway và HRM hiện cùng dùng PostgreSQL với timezone `Asia/Ho_Chi_Minh`.
- Khi tiếp tục mở rộng dữ liệu nghiệp vụ HRM, ưu tiên thống nhất cách lưu `DateTime` để tránh xung đột với provider Npgsql.
- Dữ liệu chỉ có ngày nên cân nhắc `DateOnly`.
- Không dùng `DateTimeOffset` cho nghiệp vụ HRM thông thường nếu không có yêu cầu rõ ràng.

## Lưu ý build/test

AI không được tự ý chạy build/test. Khi cần kiểm chứng bằng build/test, phải có yêu cầu rõ ràng từ người dùng.


