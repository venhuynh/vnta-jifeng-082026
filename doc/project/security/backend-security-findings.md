# Sổ Phát Hiện Bảo Mật Backend

## Quy ước

- ID: `SEC-###`.
- Mức độ: `Critical`, `High`, `Medium`, `Low`, `Informational`.
- Trạng thái: `Open`, `In Progress`, `Mitigated`, `Verified`, `Accepted Risk`.
- Không ghi dữ liệu nhạy cảm. Thay giá trị thật bằng `<redacted>` và chỉ ghi đường dẫn source tương đối.

## Danh sách hiện tại

### SEC-001 — Fallback kết nối database có credential hard-code

- Mức độ: `Critical`.
- Trạng thái: `In Progress`.
- Phạm vi/asset ảnh hưởng: database HRM dùng chung với attendance gateway.
- Mô tả và tác động: Infrastructure có fallback connection string chứa thông tin xác thực. Ai đọc được source hoặc artifact có thể dùng thông tin đó để thử truy cập database nếu endpoint mạng còn mở hoặc credential còn hiệu lực.
- Bằng chứng đã làm sạch: `Vnta.Hrm.Infrastructure/DependencyInjection.cs` dùng chuỗi fallback khi các nguồn cấu hình không có giá trị.
- Khuyến nghị: source đã fail-closed và tracked config đã được làm sạch; tiếp theo phải xoay vòng credential từng tồn tại, giới hạn quyền/network access của tài khoản DB và xác nhận secret runtime ở từng môi trường.
- Chủ sở hữu: vận hành database và backend.
- Liên kết sprint/PR: `sprint-023-backend-security-refactor`.
- Cách kiểm chứng sau xử lý: source, image và configuration repository không còn credential; app fail-closed khi thiếu cấu hình; credential cũ không còn đăng nhập được.

### SEC-002 — Demo administrator mặc định được bootstrap trong runtime

- Mức độ: `Critical`.
- Trạng thái: `Mitigated`.
- Phạm vi/asset ảnh hưởng: Identity, toàn bộ API và dữ liệu HRM.
- Mô tả và tác động: mỗi lần render ngoài khu vực Account, app chạy bootstrap demo; tài khoản định danh cố định được kích hoạt, phê duyệt và gán vai trò quản trị. Mật khẩu cũng được điền sẵn ở form đăng nhập. Nếu chạy ngoài môi trường demo, đây là đường chiếm quyền quản trị trực tiếp.
- Bằng chứng đã làm sạch: `Vnta.Hrm.Web/Data/DemoData.cs`, `Vnta.Hrm.Web/Components/App.razor`, `Vnta.Hrm.Web/Components/Account/Pages/Login.razor`.
- Khuyến nghị: bootstrap và prefill đã được bỏ khỏi runtime; quy trình cấp tài khoản ban đầu có audit nằm tại `initial-account-provisioning-runbook.md`; vô hiệu hóa/đổi credential demo hiện hữu nếu còn trong database.
- Chủ sở hữu: backend, Identity và vận hành.
- Liên kết sprint/PR: `sprint-023-backend-security-refactor`.
- Cách kiểm chứng sau xử lý: một request UI không tạo/sửa user hoặc role; không còn credential demo trong source; tài khoản bootstrap chỉ tồn tại khi được cấp phát qua quy trình vận hành đã duyệt.

### SEC-003 — Gateway inbound không xác thực nguồn gửi

- Mức độ: `High`.
- Trạng thái: `In Progress`.
- Phạm vi/asset ảnh hưởng: dữ liệu chấm công, trạng thái thiết bị, log và monitor realtime.
- Mô tả và tác động: ba endpoint `/api/integration/*` nhận attendance, system log và realtime event mà không có `RequireAuthorization`, shared-secret, chữ ký, mTLS hay cơ chế chống replay. Bất kỳ nguồn nào tới được host đều có thể giả mạo hoặc làm ngập event.
- Bằng chứng đã làm sạch: `Vnta.Hrm.Web/Endpoints/AttendanceGatewayIntegrationEndpoints.cs`.
- Khuyến nghị: source đã áp contract mTLS + HMAC có timestamp/nonce/replay protection, rate/body limit; cần provision certificate/key thật, cấu hình gateway và kiểm thử rollout theo `gateway-inbound-contract.md`.
- Chủ sở hữu: backend và đội vận hành attendance gateway.
- Liên kết sprint/PR: `sprint-023-backend-security-refactor`.
- Cách kiểm chứng sau xử lý: request thiếu/sai credential bị `401/403`; replay bị từ chối; gateway hợp lệ vẫn gửi được theo contract mới.

### SEC-004 — ADMS monitor SignalR cho phép truy cập và broadcast công khai

- Mức độ: `High`.
- Trạng thái: `Mitigated`.
- Phạm vi/asset ảnh hưởng: trạng thái thiết bị, event realtime và raw event body có thể chứa dữ liệu chấm công.
- Mô tả và tác động: hub `/hubs/adms-monitor` không áp authorization; mọi client có thể gọi snapshot và nhận broadcast qua `Clients.All`. Service hiện chỉ cắt ngắn raw body, không áp kiểm soát người nhận theo quyền.
- Bằng chứng đã làm sạch: `Vnta.Hrm.Web/Program.cs`, `Vnta.Hrm.Web/Hubs/AdmsMonitorHub.cs`, `Vnta.Hrm.Web/Hubs/AdmsMonitorEventPublisher.cs`.
- Khuyến nghị: policy `DeviceAdministration` và group theo quyền đã được áp dụng; monitor realtime chỉ nhận marker redacted thay cho raw payload. Raw log chỉ được tra cứu ở gateway log hạn chế quyền theo quy trình vận hành.
- Chủ sở hữu: backend và quản trị thiết bị.
- Liên kết sprint/PR: `sprint-023-backend-security-refactor`.
- Cách kiểm chứng sau xử lý: client chưa xác thực/không có quyền không thể connect, invoke hoặc nhận event; client có quyền chỉ thấy payload đã được tối thiểu hóa.

### SEC-005 — Attendance và Payroll thiếu policy theo nghiệp vụ tại API boundary

- Mức độ: `High`.
- Trạng thái: `In Progress`.
- Phạm vi/asset ảnh hưởng: hồ sơ nhân sự, biometric data, lệnh thiết bị, chấm công và dữ liệu lương/phụ cấp.
- Mô tả và tác động: các route group `/api/attendance`, `/api/adms/device-commands` và `/api/payroll` chỉ yêu cầu authenticated user. Nhiều thao tác đọc, ghi, xóa, refresh và lock có thể bị gọi trực tiếp mà không đi qua giới hạn UI.
- Bằng chứng đã làm sạch: `Vnta.Hrm.Web/Endpoints/AttendanceGatewayIntegrationEndpoints.cs`, `Vnta.Hrm.Web/Endpoints/PayrollEndpoints.cs`; các policy chi tiết mới có ở `SecurityEndpoints` và `ChiTietNhanVienEndpoints`.
- Khuyến nghị: policy theo nhóm đã được áp dụng cho Payroll, Attendance và ADMS command; ma trận route/action được lưu tại `backend-route-capability-matrix.md`. Workflow tăng ca kiểm tra actor ownership trước persistence và chỉ quản trị xưởng được đổi trạng thái; các workflow self-service mới vẫn phải áp dụng mẫu kiểm tra này.
- Chủ sở hữu: chủ nghiệp vụ HRM, payroll, attendance và backend.
- Liên kết sprint/PR: `sprint-023-backend-security-refactor`.
- Cách kiểm chứng sau xử lý: test `401/403` cho mọi role không được cấp; các role hợp lệ chỉ thực hiện được action thuộc capability của mình.

### SEC-006 — Đăng nhập không kích hoạt lockout và host chưa có rate limit

- Mức độ: `Medium`.
- Trạng thái: `In Progress`.
- Phạm vi/asset ảnh hưởng: tài khoản nội bộ và tính sẵn sàng host.
- Mô tả và tác động: login gọi `PasswordSignInAsync` với `lockoutOnFailure: false`; host không đăng ký hay dùng rate limiter. Điều này tăng khả năng brute-force và flood cho login/API công khai.
- Bằng chứng đã làm sạch: `Vnta.Hrm.Web/Components/Account/Pages/Login.razor`, `Vnta.Hrm.Web/Program.cs`.
- Khuyến nghị: lockout, password policy, generic login failure, secure cookie, request-size limit, rate limiting và structured rejection log đã được thêm; vận hành phải cấu hình alert thật và thực thi load test staging theo `rate-limit-alert-and-load-test-runbook.md`.
- Chủ sở hữu: Identity và backend.
- Liên kết sprint/PR: `sprint-023-backend-security-refactor`.
- Cách kiểm chứng sau xử lý: request vượt ngưỡng bị throttled; lockout chỉ áp dụng đúng tài khoản và được audit; login hợp lệ vẫn hoạt động.

### SEC-007 — Chưa có bộ test bảo mật cho boundary quan trọng

- Mức độ: `Medium`.
- Trạng thái: `In Progress`.
- Phạm vi/asset ảnh hưởng: khả năng ngăn tái phát của toàn bộ refactor.
- Mô tả và tác động: solution hiện chưa có project test độc lập; không có regression test chứng minh policy endpoint, gateway authentication, SignalR authorization hay bootstrap production an toàn.
- Bằng chứng đã làm sạch: inventory project dưới `src/Vnta.HRM2026` không có test project.
- Khuyến nghị: đã thêm regression test unauthenticated cho API/hub và workflow secret/dependency scan; cần cấu hình secret package source trên CI, bổ sung case `403`/authorized/gateway replay rồi chạy pipeline thành công trước khi đóng.
- Chủ sở hữu: backend và CI/CD.
- Liên kết sprint/PR: `sprint-023-backend-security-refactor`.
- Cách kiểm chứng sau xử lý: pipeline chạy test policy và secret scan; mỗi `SEC` đóng có test regression tương ứng.
