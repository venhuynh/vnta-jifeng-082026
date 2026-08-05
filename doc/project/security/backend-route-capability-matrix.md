# Ma trận route, capability và phạm vi dữ liệu backend HRM

Tài liệu này là baseline review cho `SEC-005`. Policy tại Web là lớp chặn đầu tiên; service vẫn phải kiểm tra ownership hoặc phạm vi dữ liệu trước persistence. Không suy diễn quyền từ việc UI có/không hiển thị nút thao tác.

## Policy và role hiện hành

| Policy | Role được phép | Mục đích |
| --- | --- | --- |
| `AttendanceAdministration` | `SystemAdmin`, `AttendanceAdmin`, `HrAdmin` | Quản trị chấm công, lịch, dữ liệu thô và tăng ca. |
| `PayrollAdministration` | `SystemAdmin`, `PayrollAdmin`, `HrAdmin` | Lương, phụ cấp và khấu trừ. |
| `DeviceAdministration` | `SystemAdmin`, `AttendanceAdmin` | Thiết bị ADMS, command và monitor realtime. |
| `HumanResourcesAdministration` | `SystemAdmin`, `HrAdmin` | Hồ sơ nhân sự. |
| `EmployeeAccountAdministration` | capability `employee_accounts.open` | Mở, kích hoạt, vô hiệu, reset tài khoản. |
| `EmployeeAccountApproval` | capability `employee_accounts.approve` | Phê duyệt hoặc từ chối tài khoản. |

## Route quản trị chấm công

| Route/action | Policy | Quyền dữ liệu bắt buộc tại service |
| --- | --- | --- |
| `GET /api/attendance/devices`, `POST /devices`, `/devices/delete`, `/devices/validate` | `AttendanceAdministration` | Thiết bị phải thuộc tenant HRM hiện hành; không nhận quyết định quyền từ client. |
| `GET /status-codes`, `GET/POST/DELETE /work-calendar*` | `AttendanceAdministration` | Validate năm/ngày và ràng buộc calendar trước ghi. |
| `GET /logs/recent`, `/logs/by-date-range`, `POST /logs/search` | `AttendanceAdministration` | Query server-side, giới hạn date range/pagination. |
| `POST /biometric-data/search`, `/device-commands/push`, `/device-commands/delete`, `/refresh*` | `AttendanceAdministration` | Chỉ lệnh thuộc thiết bị/nhân viên tồn tại; không tin danh sách ID từ client. |
| `POST /logs/daily-summary/*`, `/logs/workday-summary/*` | `AttendanceAdministration` | Validate khoá kỳ và tập ID trước update/delete/rebuild. |
| `POST /overtime-registrations/search` | `AttendanceAdministration` | Hiện là route quản trị; nếu mở self-service phải lọc theo `EmployeeId` claim. |
| `POST /overtime-registrations/draft`, `POST /overtime-registrations` | `AttendanceAdministration` | Actor phải có `EmployeeId`; khi sửa, chỉ requester gốc được sửa trừ role quản trị xưởng. |
| `POST /overtime-registrations/status` | `AttendanceAdministration` | Chỉ role quản trị xưởng được chuyển trạng thái/phê duyệt; ghi lịch sử actor. |
| `POST /employees*`, `PUT /employees/{id}`, `/employees/delete`, `/employees/refresh` | `AttendanceAdministration` | Chỉ dùng cho đồng bộ attendance; validate ID/path/body và soft-delete scope. |

## Route payroll

| Route/action | Policy | Quyền dữ liệu bắt buộc tại service |
| --- | --- | --- |
| POST /api/payroll/attendance-allowance/export | PayrollAdministration | Chỉ nhận kỳ + định dạng; service validate kỳ, trả allowlist toàn kỳ, áp dụng scope dữ liệu và audit format/kỳ/số dòng. |
| `/api/payroll/basic-salaries*` | `PayrollAdministration` | Validate kỳ lương, employee ID và lock state. |
| `/seniority-allowances*`, `/attendance-allowance*`, `/meal-allowance*`, `/hazard-allowance*` | `PayrollAdministration` | Refresh/sync/lock/delete chỉ trong kỳ hợp lệ; actor được audit. |
| `/responsibility-allowance-grade-config*`, `/monthly-responsibility-allowance-abc*` | `PayrollAdministration` | Chỉ quản trị lương sửa cấu hình, mapping, bonus hoặc lock row. |
| `/allowance-summary*`, `/leave-holiday-allowance*`, `/other-responsibility-allowance/search` | `PayrollAdministration` | Không cho sửa/xoá khi kỳ đã khóa; filter phải server-validated. Export tổng hợp phụ cấp chỉ nhận kỳ + format, trả allowlist toàn kỳ và audit format/kỳ/số dòng. |
| `/deduction-summary*`, `/social-health-insurance-deductions*` | `PayrollAdministration` | Validate source data, kỳ và lock state trước persistence. `POST /deduction-summary/export` chỉ nhận kỳ + định dạng, trả DTO allowlist, audit actor/correlation/format/kỳ/số dòng và từ chối kỳ quá 5.000 dòng. |
| `/other-deductions*` | `PayrollAdministration` | Điều chỉnh chỉ cho record thuộc kỳ hợp lệ, chưa khóa; server đối soát version và ghi audit actor. |

## Route thiết bị, tích hợp và nhân sự

| Route/action | Policy/cơ chế | Quyền dữ liệu bắt buộc tại service |
| --- | --- | --- |
| `/api/adms/device-commands/*` | `DeviceAdministration` | Command và response chỉ trong inventory thiết bị HRM. |
| `/hubs/adms-monitor` | `DeviceAdministration` | Chỉ group có policy nhận event; payload phải tối thiểu hóa. |
| `POST /api/integration/attendance-gateway/*`, `/api/integration/adms/realtime/events` | mTLS + HMAC + replay/rate/body limit | Gateway chỉ ghi event theo contract; không có quyền người dùng HRM. |
| `/api/nhan-su/chi-tiet-nhan-vien/*` | `HumanResourcesAdministration` | Truy cập hồ sơ phải theo scope HR; path ID phải khớp payload. |
| `/api/nhan-su/nhan-vien/*` | `HumanResourcesAdministration` | Danh sách, create và refresh chỉ dành cho HR; command phải audit actor. |
| `/api/admin/employee-accounts/*` | capability account tương ứng | Tách quyền quản trị tài khoản và quyền phê duyệt. |

## Quy tắc thay đổi

1. Route mới phải thêm một dòng vào tài liệu này trong cùng PR.
2. Action ghi/xoá/refresh/lock/phê duyệt không được dùng policy chỉ xác thực chung.
3. Nếu workflow self-service được mở cho `Employee` hoặc `Manager`, phải bổ sung test: actor A không thể đọc/sửa/phê duyệt dữ liệu actor B.
4. Thay đổi role/capability phải cập nhật `InternalAccountRoles`, policy Web và regression tests cùng lúc.
