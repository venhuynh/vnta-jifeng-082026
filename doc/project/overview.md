# Tổng Quan Dự Án

JIFENG HRM là ứng dụng quản trị nhân sự xây dựng bằng Blazor và DevExpress Blazor.

## Source chính hiện tại

- Source root hiện hành: `src/Vnta.HRM2026`
- Solution hiện hành: `src/Vnta.HRM2026/Jifeng.Hrm.slnx`
- Dự án server/host: `src/Vnta.HRM2026/Vnta.Hrm.Web`
- Dự án client UI: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client`
- Dự án domain: `src/Vnta.HRM2026/Vnta.Hrm.Domain`
- Dự án application: `src/Vnta.HRM2026/Vnta.Hrm.Application`
- Dự án infrastructure: `src/Vnta.HRM2026/Vnta.Hrm.Infrastructure`
- Solution đồng bộ PostgreSQL độc lập: `src/Vnta.PostgresSync`
- Source `src/Vnta.HRM` đã bị loại khỏi repo và chỉ còn xuất hiện trong tài liệu lịch sử
- Tài liệu vận hành console sync: `doc/setup/postgres-sync-console.md`

## Công nghệ hiện tại

- .NET 10
- Blazor Web App với Interactive Server Components và Interactive WebAssembly Components
- DevExpress Blazor `26.1.x` với package đang pin `26.1.3`
- ASP.NET Core Identity
- Entity Framework Core
- PostgreSQL/Npgsql dùng chung database với gateway attendance
- Worker sync PostgreSQL-to-PostgreSQL cấu hình bằng `appsettings.json` hoặc biến môi trường
- AI integration demo endpoint của DevExpress

## Hiện trạng kỹ thuật cần nhớ

- Tên solution và project kỹ thuật đã đổi sang ngữ cảnh HRM, nhưng cấu trúc feature bên trong vẫn còn nhiều dấu vết CRM demo.
- Baseline runtime hiện tại của source này đã chốt là `.NET 10` với DevExpress `26.1.x`.
- HRM hiện dùng chung PostgreSQL với `src/zkteco-adms-gateway` tại database Jifeng `jifeng_hrm`.
- `ApplicationDbContext`, `ApplicationUser`, migration Identity và `IdentityNoOpEmailSender` đã được chuyển sang `Vnta.Hrm.Infrastructure`.
- Runtime không còn seed tài khoản demo; cấu hình cục bộ dùng `appsettings.Local.json` đã được ignore và lấy mẫu từ `appsettings.Local.example.json`.
- Migration Identity hiện có vẫn là di sản từ SQL Server demo cũ và cần được chuẩn hóa lại theo Npgsql ở các lượt tiếp theo.
- Roadmap refactor theo từng phase được chốt tại `doc/project/refactor-roadmap.md`.

## Mục tiêu sản phẩm

Ứng dụng hướng đến nghiệp vụ HRM, bao gồm các nhóm chức năng chính:

- Hồ sơ nhân sự
- Cơ cấu tổ chức
- Phòng ban, chức danh, vị trí
- Chấm công
- Nghỉ phép
- Hợp đồng
- Lương thưởng
- Phân quyền và tài khoản

## Phạm vi chưa chốt

Các phần sau cần đặc tả riêng trước khi triển khai:

- Công thức tính công
- Công thức tính lương
- Quy trình phê duyệt nghỉ phép, tăng ca, điều chỉnh công
- Ma trận phân quyền chi tiết
- Chính sách audit và lưu vết dữ liệu nhạy cảm

## Nguyên tắc phát triển

- Giao diện quản trị rõ ràng, ưu tiên thao tác nhanh.
- Caption và thông báo cho người dùng phải là tiếng Việt.
- Không tự bịa quy tắc nghiệp vụ HRM.
- Khi tiếp tục mở rộng dữ liệu nghiệp vụ trên PostgreSQL, ưu tiên thống nhất quy tắc thời gian theo Npgsql.
- Mọi thay đổi lớn nên có đặc tả ngắn trước khi code.


