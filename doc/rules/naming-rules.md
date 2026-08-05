# Quy Tắc Đặt Tên

Áp dụng cho C#, Razor, CSS, route, file và tài liệu.

## 1. C# và Razor

- Class, component, enum, record: `PascalCase`.
- Method, property, event callback: `PascalCase`.
- Biến local, parameter, field private: `camelCase`.
- Interface bắt đầu bằng `I`.
- Tên phải thể hiện nghiệp vụ, tránh tên mơ hồ như `Data`, `Info`, `Temp`, `Item` nếu thiếu ngữ cảnh.

## 2. Component Blazor

- Page nghiệp vụ đặt theo danh từ nghiệp vụ: `Employees`, `Departments`, `LeaveRequests`.
- Component dùng lại đặt theo vai trò: `EmployeeSelector`, `DepartmentTree`, `ApprovalStatusBadge`.
- File `.razor.css` đi cùng component khi style chỉ phục vụ component đó.

## 3. Route và thư mục

- Route nên ngắn, rõ nghiệp vụ và dùng tiếng Anh kỹ thuật ổn định.
- Caption hiển thị trên UI phải là tiếng Việt, dù route hoặc class dùng tiếng Anh.
- Tránh route chứa từ viết tắt khó hiểu.

## 4. Database và entity

- Entity dùng danh từ số ít: `Employee`, `Department`, `LeaveRequest`.
- DbSet dùng danh từ số nhiều: `Employees`, `Departments`, `LeaveRequests`.
- Trạng thái nên dùng enum hoặc hằng số tập trung, không rải chuỗi tự do trong code.

## 5. Tài liệu

- File Markdown dùng `lowercase-hyphen-separated.md`.
- Tiêu đề tài liệu viết tiếng Việt có dấu.
- Tài liệu phải có mục đích rõ ràng, tránh tạo file rỗng hoặc trùng nội dung.

## 6. Tên source và module theo ngữ cảnh HRM hiện hành

- Khi ghi đường dẫn source trong tài liệu mới, dùng `src/Vnta.HRM2026/...`.
- Không dùng `src/Vnta.HRM/...` cho nội dung mới trừ khi đang mô tả lịch sử.
- Namespace, tên thư mục và tên module mới phải phản ánh ngữ cảnh HRM thật như
  `Employees`, `Organizations`, `Attendance`, `Leave`, `Payroll`, `Contracts`,
  `Security`.
- Tránh đặt tên module HRM mới theo dấu vết demo như `Contacts`, `Planning`,
  `Analytics` nếu module đó không còn mang ý nghĩa demo.

## 7. Quy tắc bắt buộc cho tên file theo context

- Trước khi tạo, đổi tên hoặc di dời file feature, phải xác định `Tên nghiệp vụ`,
  `ContextKey` PascalCase không dấu và technical alias cần giữ nếu có.
- Tên folder, namespace và file thuộc một feature phải dùng cùng `ContextKey`.
- File C# feature dùng mẫu `{ContextKey}{VaiTro}.cs`, ví dụ
  `NhanVienFilter.cs`, `INhanVienService.cs`, `NhanVienEndpoints.cs` và
  `DatabaseNhanVienService.cs`.
- Razor page dùng `{ContextKey}.razor`, code-behind `{ContextKey}.razor.cs` và
  style cục bộ `{ContextKey}.razor.css`; component phụ thêm hậu tố vai trò.
- Không dùng tên chung chung như `Service.cs`, `Helper.cs`, `Model.cs` hoặc
  `Endpoints.cs` cho file feature. Technical alias chỉ giữ ở schema, integration
  hoặc compatibility boundary có lý do được ghi lại.

