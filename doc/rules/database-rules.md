# Quy Tắc Database

Áp dụng cho mọi thiết kế dữ liệu, migration và truy vấn trong dự án.

## 1. Database chuẩn

- Dự án sử dụng PostgreSQL.
- EF Core provider chuẩn là `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Không thêm lại SQL Server provider nếu không có quyết định kiến trúc mới.

## 2. Lưu ngày giờ không kèm time zone

- Trường `DateTime` nghiệp vụ phải lưu bằng PostgreSQL `timestamp without time zone`.
- Không dùng `timestamp with time zone` cho ngày công, ca làm, nghỉ phép, hợp đồng, hiệu lực quyết định hoặc mốc nghiệp vụ HRM nếu không có yêu cầu rõ ràng.
- Với dữ liệu chỉ có ngày, ưu tiên kiểu phù hợp như `DateOnly` khi thiết kế domain.
- Không dùng `DateTimeOffset` cho nghiệp vụ HRM thông thường nếu không thật sự cần offset.
- Trường hệ thống nội bộ của ASP.NET Identity có thể dùng `DateTimeOffset` theo framework; không lấy đó làm chuẩn cho dữ liệu nghiệp vụ HRM.

## 3. Migration

- Migration phải được tạo bằng provider PostgreSQL.
- Không dùng migration sinh từ SQL Server cho PostgreSQL.
- Khi đổi provider hoặc đổi convention kiểu dữ liệu, phải tạo migration mới ở bước riêng có kiểm chứng.

## 4. Connection string

- Không commit mật khẩu thật.
- Dùng user secret, biến môi trường hoặc cấu hình triển khai cho thông tin nhạy cảm.
- `appsettings.json` chỉ được chứa connection string mẫu hoặc placeholder.
