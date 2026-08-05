# JIFENG HRM

Hệ thống quản trị nhân sự, chấm công và tính lương triển khai cho **CÔNG TY TNHH JIFENG HOME FURNISHING (VIET NAM)**.

| Thông tin | Giá trị |
| --- | --- |
| Mã số thuế | `3502539513` |
| Tên quốc tế | JIFENG HOME FURNISHING (VIET NAM) CO., LTD |
| Tên viết tắt | JIFENG (VN) |
| Địa chỉ | Lô A16, Đường số 5, KCN Mỹ Xuân A, Phường Phú Mỹ, Thành phố Hồ Chí Minh, Việt Nam |

## Thành phần chính

- **JIFENG HRM** — ứng dụng web quản trị nhân sự, chấm công, phê duyệt và tính lương.
- **ADMS Gateway** — nhận và đồng bộ dữ liệu từ thiết bị chấm công ZKTeco.
- **Postgres Sync** — công cụ đồng bộ dữ liệu PostgreSQL theo nhu cầu vận hành.

## Công nghệ

- .NET 10 (SDK được ghim trong [`global.json`](global.json))
- ASP.NET Core và Blazor
- DevExpress Blazor `26.1.3`
- Entity Framework Core; môi trường production dùng PostgreSQL
- Docker Compose cho triển khai Ubuntu

## Cấu trúc repository

```text
src/
├── Vnta.HRM2026/
│   ├── Jifeng.Hrm.slnx                 # Solution chính
│   ├── Vnta.Hrm.Domain/                # Domain model
│   ├── Vnta.Hrm.Application/           # Use cases và contracts
│   ├── Vnta.Hrm.Infrastructure/        # Data access và integrations
│   ├── Vnta.Hrm.Web.Client/            # Blazor WebAssembly client
│   ├── Vnta.Hrm.Web/                   # ASP.NET Core web host
│   └── Vnta.Hrm.*.Tests/               # Automated tests
├── zkteco-adms-gateway/                # Gateway thiết bị chấm công
└── Vnta.PostgresSync/                  # Công cụ đồng bộ PostgreSQL

deploy/ubuntu/                           # Dockerfiles, Compose và script triển khai
```

## Phát triển cục bộ

### Yêu cầu

- .NET SDK tương thích với [`global.json`](global.json)
- Quyền truy cập NuGet feed chứa các package DevExpress
- Cấu hình kết nối cơ sở dữ liệu và các secret cục bộ qua User Secrets hoặc biến môi trường

### Khôi phục package

```powershell
dotnet restore src\Vnta.HRM2026\Jifeng.Hrm.slnx
```

### Build ứng dụng

```powershell
dotnet build src\Vnta.HRM2026\Vnta.Hrm.Web\Vnta.Hrm.Web.csproj --no-restore
```

Để kiểm tra riêng Web Client:

```powershell
dotnet build src\Vnta.HRM2026\Vnta.Hrm.Web.Client\Vnta.Hrm.Web.Client.csproj --no-restore
```

### Chạy ứng dụng web

```powershell
dotnet run --project src\Vnta.HRM2026\Vnta.Hrm.Web\Vnta.Hrm.Web.csproj
```

### Cấu hình Postgres Sync cục bộ

`Postgres Sync` đọc file `appsettings.Local.json` đã được ignore trong project
console. Tạo file từ mẫu, điền `ConnectionStrings:SourcePostgres` bằng database
nguồn đã được cấp quyền và `ConnectionStrings:TargetPostgres` bằng database đích
Jifeng (`Database=jifeng_hrm`). Không commit connection string.

```powershell
Copy-Item src\Vnta.PostgresSync\Vnta.PostgresSync.Console\appsettings.Local.example.json src\Vnta.PostgresSync\Vnta.PostgresSync.Console\appsettings.Local.json
```

Chạy `inspect` trước mọi lệnh đồng bộ. Xem hướng dẫn chi tiết tại
[`doc/setup/postgres-sync-console.md`](doc/setup/postgres-sync-console.md).

Không đưa chuỗi kết nối, chứng thư TLS, token hay khoá HMAC vào file cấu hình được commit. Các file `appsettings.Local.example.json` chỉ là mẫu cấu hình cục bộ.

## Triển khai Ubuntu

Tài nguyên triển khai production nằm trong [`deploy/ubuntu`](deploy/ubuntu):

- `docker-compose.production.yml` chạy `hrm-web` và `adms-gateway`.
- `.env.production.example` liệt kê các biến môi trường bắt buộc.
- `scripts/` gồm các script build, publish, deploy, rollback, backup và migration cơ sở dữ liệu.

Trước khi triển khai, tạo file môi trường bảo mật từ mẫu, cung cấp image đã phát hành, chứng thư TLS và toàn bộ biến bắt buộc. Không commit file `.env` hay dữ liệu chứng thực vào repository.

## Bảo mật và vận hành

- Gateway dùng khoá HMAC và có hỗ trợ xác thực chứng thư client/server.
- Log production được mount ra volume riêng; cần kiểm soát quyền truy cập và thời hạn lưu giữ.
- Migration cơ sở dữ liệu production phải thực hiện qua script triển khai được kiểm soát.

## Quy ước đóng góp

- Giữ mã nguồn chính trong `src/` và triển khai trong `deploy/`.
- Chạy build hoặc test phù hợp trước khi tạo commit.
- Không commit output build (`bin/`, `obj/`), secret hoặc tài liệu vận hành có thông tin nhạy cảm.
