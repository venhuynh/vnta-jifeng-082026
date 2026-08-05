# PostgreSQL

Tài liệu này ghi các nguyên tắc triển khai database PostgreSQL cho dự án.

## Provider

EF Core provider chuẩn:

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
```

Đăng ký DbContext:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
```

## Kiểu dữ liệu thời gian

Quy tắc bắt buộc cho dữ liệu nghiệp vụ:

```csharp
configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");
```

Ngoại lệ: field hệ thống của ASP.NET Identity có thể dùng `DateTimeOffset` theo framework.

## Migration

- Migration phải dùng provider PostgreSQL/Npgsql.
- Không dùng migration SQL Server.
- Khi thêm entity nghiệp vụ có trường thời gian, kiểm tra migration sinh ra `timestamp without time zone`.

