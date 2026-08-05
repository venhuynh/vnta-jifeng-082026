# DuLieuSinhTracHoc

Màn hình này sở hữu route `/attendance/biometric-data` trong HRM.

## Mục tiêu tài liệu

Tài liệu này chốt lại:

- nghĩa vụ nghiệp vụ của nút `Làm mới`;
- logic tổng hợp dữ liệu `biometric_data` từ các bảng nguồn;
- trạng thái triển khai hiện tại trong repo HRM;
- các điểm cần lưu ý khi mở rộng hoặc bảo trì màn hình.

## Nguồn tham chiếu đã đối chiếu

Các nguồn dưới đây nằm trong repo tham chiếu do user chỉ định trực tiếp cho lượt rà soát:

- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\src\app\components\biometric\BiometricManagementPage.tsx`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\src\app\components\biometric\BiometricDataGrid.tsx`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\src\services\api\biometric-data-api.ts`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\backend\src\HyperTech.Api\Controllers\BioMetricDataController.cs`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\backend\src\HyperTech.Infrastructure\Services\BioMetricDataRefreshService.cs`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\docs\05_delivery-plan\sprints\_OLD\sprint-36-biometric-refresh-database-refactor\implementation-plan.md`

## Nguồn tham chiếu thêm cho hướng refactor avatar

Các nguồn dưới đây được dùng trong lượt rà soát ngày `2026-07-05` theo chỉ định trực tiếp của user:

- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\src\app\components\nhansu\danh_muc_nhan_vien\EmployeeGrid.tsx`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\src\app\components\nhansu\shared\employee-api-mappers.ts`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\src\services\api\employees-api.ts`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\backend\src\HyperTech.Application\Employees\EmployeeService.cs`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\backend\src\HyperTech.Infrastructure\Services\PostgresEmployeePhotoSyncService.cs`
- `C:\Users\Admin\source\Workspaces\2026\Vnta2026figmadesign\backend\src\HyperTech.Api\Controllers\BioMetricDataController.cs`

## Kết luận nghiệp vụ

`biometric_data` không phải bảng nguồn gốc. Đây là bảng summary/tổng hợp được build lại từ các bảng sinh trắc học và nhân sự liên quan.

Vì vậy, nút `Làm mới` đúng nghĩa nghiệp vụ không chỉ là:

- reload danh sách trên UI;
- query lại `biometric_data`;
- làm mới grid client-side.

Thay vào đó, nút này phải kích hoạt một luồng backend để tổng hợp lại và đồng bộ lại dữ liệu vào bảng `biometric_data`.

## Ý nghĩa của 2 thao tác trên màn hình

### 1. `Tải lại`

`Tải lại` chỉ có trách nhiệm:

- query lại dữ liệu hiện có trong `biometric_data`;
- render lại grid theo dữ liệu đã tồn tại ở DB đích.

Nó không chạy lại thuật toán tổng hợp.

### 2. `Làm mới`

`Làm mới` phải:

- chạy backend aggregation;
- đọc lại các bảng nguồn liên quan;
- cập nhật `biometric_data`;
- đồng bộ thêm các dữ liệu phụ trợ cần thiết để kết quả summary mới nhất.

## Endpoint hiện tại trong HRM

Backend hiện đã triển khai:

- `POST /api/attendance/biometric-data/refresh`
- `POST /api/attendance/biometric-data/refresh/{employeeId}`
- `GET /api/attendance/biometric-data/refresh/progress`
- `POST /api/attendance/biometric-data/search`

Lưu ý:

- màn hình hiện tại đang dùng waiting/loading panel khi bấm `Làm mới`;
- progress bar thử nghiệm trước đó đã được gỡ bỏ khỏi UI.

## Thuật toán backend của `Làm mới`

Luồng tổng hợp hiện tại trong `DatabaseAttendanceBiometricDataRefreshService` được chốt như sau:

1. Bảo đảm bảng `biometric_data` tồn tại.
2. Bảo đảm cột `employees.Avatar` tồn tại để có thể cập nhật ảnh đại diện.
3. Lấy `employees` làm nguồn quyết định active set.
4. Active set gồm các nhân sự có `Status` thuộc `Active = 2` hoặc `OnLeave = 3`.
5. Đồng bộ active set vào `device_user_profiles`.
6. Đồng bộ ảnh từ `user_pictures` sang `bio_photos` nếu `bio_photos` còn thiếu.
7. Cập nhật `employees.Avatar` theo thứ tự ưu tiên:
   - ưu tiên `bio_photos`;
   - nếu không có thì fallback sang `user_pictures`;
   - nếu cả hai nguồn đều không có thì xóa `Avatar`.
8. Tính `FpQty` từ các nguồn vân tay hiện có.
9. Xác định `HasFaceData` từ các nguồn khuôn mặt hiện có.
10. Insert/update summary vào `biometric_data` cho active set.
11. Xóa các dòng stale trong `biometric_data` nếu nhân sự không còn thuộc active set.

## Nguồn dữ liệu đang được tổng hợp

### Nguồn profile

- `device_user_profiles`

Dùng để tổng hợp:

- `CardNumber`
- `Password`
- `IsAdmin`

### Nguồn vân tay

Ưu tiên theo thứ tự:

1. `fingerprint_templates`
2. `biodata`
3. các bảng legacy như `templatev10`, `templatev9`, `templatev8`, `template`, `userfinger`, `fptemplate`

### Nguồn khuôn mặt

Ưu tiên theo thứ tự:

1. `bio_photos`
2. `user_pictures`
3. `face_templates`
4. `biodata`
5. các bảng legacy như `biophoto`, `userpic`, `face`

## Logic avatar nhân viên

Màn hình danh sách hiện hiển thị avatar từ `employees.Avatar`.

Luật cập nhật `Avatar` hiện tại:

1. Nếu `bio_photos` có ảnh hợp lệ cho nhân sự thì dùng ảnh đó và dừng.
2. Nếu `bio_photos` không có thì tìm trong `user_pictures`.
3. Nếu cả `bio_photos` và `user_pictures` đều không có ảnh thì đặt `employees.Avatar = null`.

Mục tiêu là giữ `Avatar` luôn phản ánh dữ liệu sinh trắc mới nhất, không để sót ảnh cũ khi nguồn đã bị xóa.

## Hiển thị UI hiện tại

Màn hình list hiện có:

- checkbox chọn từng dòng;
- cột `STT`;
- cột avatar tròn hiển thị từ `employees.Avatar`;
- cột `Nhân viên`;
- cột `Số vân tay`;
- cột `Khuôn mặt` dạng checkbox readonly;
- cột `Thẻ`;
- cột `Mật khẩu` dạng checkbox readonly;
- cột `Admin` dạng checkbox readonly.

Nếu avatar không render được ở UI:

- UI sẽ fallback sang avatar chữ cái đầu;
- lớp model client tự chuẩn hóa chuỗi ảnh về dạng `data:image/...;base64,...` nếu dữ liệu DB đang lưu base64 thô.

## Phân tích sự cố avatar hiện tại

Qua đối chiếu source hiện hành:

- `DatabaseAttendanceBiometricDataRefreshService` đang đồng bộ `employees.Avatar` trực tiếp từ `bio_photos.Content` rồi fallback sang `user_pictures.Content`.
- `DatabaseAttendanceBiometricDataReadService` trả trường `Avatar` cho list screen từ `employees.Avatar`.
- `AttendanceBiometricDataRecord.AvatarImageSrc` ở client hiện chỉ normalize tốt 3 nhóm:
  - `data:image/...`
  - URL `http`, `https`, `/`
  - base64 text "sạch"

Điểm rủi ro hiện tại:

- nếu ảnh trong DB đang được lưu ở dạng text hex của PostgreSQL `\x...`, hoặc một biến thể text khác không phải base64 thuần, `AvatarImageSrc` sẽ trả `null`;
- khi đó `<img src="...">` không render được và UI rơi về avatar chữ cái đầu;
- nghĩa là contract ảnh đang phụ thuộc vào "cách DB tình cờ lưu text", không phải contract ổn định giữa backend và UI.

Khác biệt quan trọng với repo tham chiếu:

- repo tham chiếu không đẩy raw avatar text thẳng ra UI;
- backend chuẩn hóa ảnh trước thành `avatarDataUrl`;
- UI list/detail chỉ cần bind `src={avatarDataUrl}` và fallback nếu giá trị rỗng;
- `PostgresEmployeePhotoSyncService.ConvertToDataUrl(...)` của repo tham chiếu còn xử lý được:
  - `byte[]`
  - `data:image/...`
  - base64 text
  - text hex dạng `\x...`

## Kế hoạch refactor avatar đề xuất

### Mục tiêu

- Ngừng để UI phải tự đoán format ảnh từ raw DB text.
- Đảm bảo list screen `Sinh trắc học` luôn nhận được một giá trị `src` ổn định cho `<img>`.
- Giữ `employees.Avatar` nếu cần cho cache/snapshot, nhưng không xem raw text đó là UI contract dài hạn.

### Pha 1 - Vá hiển thị ngắn hạn

- Mở rộng normalize ở `AttendanceBiometricDataRecord.AvatarImageSrc` để nhận thêm:
  - text hex `\x...`
  - prefix `data:` tổng quát hơn thay vì chỉ `data:image/`
- Mục tiêu là xử lý nhanh lỗi "ảnh có trong DB nhưng không render".

### Pha 2 - Chuyển chuẩn hóa sang backend

- Đổi contract list từ kiểu `Avatar` raw sang một trường đã chuẩn hóa như `AvatarDataUrl`, hoặc ít nhất bảo đảm `Avatar` backend trả ra luôn ở dạng dùng được cho `img src`.
- Điểm thay đổi dự kiến:
  - `AttendanceBiometricDataListItemDto`
  - `DatabaseAttendanceBiometricDataReadService`
  - `HttpAttendanceBiometricDataReadService`
  - `AttendanceBiometricDataRecord`
- Ưu tiên tạo helper backend dùng chung để convert ảnh sang data URL thay vì lặp logic ở nhiều page model.

### Pha 3 - Tách nguồn ảnh hiệu lực khỏi snapshot thô

- Cân nhắc bổ sung một service đọc ảnh hiệu lực tương tự `PostgresEmployeePhotoSyncService` của repo tham chiếu.
- Service này nên:
  - ưu tiên `bio_photos`
  - fallback `user_pictures`
  - normalize về data URL ổn định
  - hỗ trợ cả schema text/base64/hex đang gặp ngoài thực tế
- Khi đó `employees.Avatar` có thể tiếp tục tồn tại như cache đồng bộ, nhưng UI không còn phụ thuộc hoàn toàn vào raw snapshot này nữa.

### Hướng ưu tiên cho repo HRM hiện tại

Ưu tiên khuyến nghị:

1. Vá client để unblock hiển thị avatar ngay.
2. Sau đó refactor contract backend để list screen nhận avatar đã chuẩn hóa.
3. Nếu cần dùng lại cho nhiều màn, tách helper/service ảnh dùng chung ở tầng `Infrastructure` hoặc `Application`.

## Kết quả trả về sau khi `Làm mới`

Backend trả về các chỉ số:

- `totalEmployees`
- `inserted`
- `updated`
- `profilesInserted`
- `profilesUpdated`
- `profilesDeleted`
- `employeesWithFingerprints`
- `employeesWithFaceData`
- `refreshedAtUtc`
- `fingerprintSource`
- `faceSource`

Toast thành công trên UI hiện mô tả:

- đã đồng bộ bao nhiêu nhân sự vào `biometric_data`;
- profile `+insert / ~update / -delete`;
- bao nhiêu nhân sự có vân tay;
- bao nhiêu nhân sự có dữ liệu khuôn mặt.

## Trạng thái triển khai hiện tại trong repo HRM

Hiện tại đã có:

- list screen đọc từ `biometric_data`;
- màn hình danh sách với cột avatar lấy từ `employees.Avatar`;
- workflow backend tổng hợp lại `biometric_data` từ các bảng nguồn;
- đồng bộ `device_user_profiles`, `bio_photos`, `employees.Avatar`;
- log nguồn dữ liệu vân tay/khuôn mặt trong lần refresh;
- auto-create `biometric_data` và auto-create cột `employees.Avatar` nếu thiếu ở môi trường local/dev.

Hiện tại chưa giữ trong UI:

- progress bar phần trăm cho lượt tổng hợp;
- block hiển thị snapshot progress riêng trên màn hình.

Thay vào đó, màn hình đang dùng waiting/loading panel để phản ánh trạng thái đang tổng hợp.

## Ghi chú bảo trì

- Nếu thay đổi cách lưu `Avatar` trong DB, cần cập nhật cả logic backend lẫn chuẩn hóa `AvatarImageSrc` ở client.
- Nếu bổ sung thêm nguồn sinh trắc mới, ưu tiên cập nhật `DatabaseAttendanceBiometricDataRefreshService` và tài liệu này cùng lúc.
- Nếu thay đổi schema bảng nguồn, service hiện có cơ chế dò alias cột/bảng, nhưng vẫn cần kiểm tra lại log nguồn sau khi triển khai.
- Nếu refactor xong theo hướng backend trả `AvatarDataUrl`, cần cập nhật lại tài liệu này để hạ `AvatarImageSrc` ở client xuống vai trò fallback thay vì tuyến xử lý chính.

## Trạng thái sau lượt triển khai 2026-07-05

- Đã tách helper dùng chung `AvatarImageSourceHelper` ở tầng `Application`.
- Đã đổi contract UI-ready từ `Avatar` sang `AvatarDataUrl` cho:
  - `AttendanceBiometricDataListItemDto`
  - `EmployeeListItemDto`
  - `EmployeeRecord`
  - `ContactDetail`
- Đã sửa root cause ảnh JPEG base64 mở đầu bằng `/9j/...` bị hiểu nhầm thành path `/...`.
- Đã smoke test UI sau đăng nhập với dữ liệu local:
  - `/attendance/biometric-data`
  - `/attendance/employees/details?id=019d1e3a-8ac1-7054-8285-3a93e2d84094`
- Kết quả smoke test:
  - avatar nhân viên `00005 - Nguyễn Thị Hậu` render thành `data:image/jpeg;base64,...`
  - `naturalWidth = 300` ở cả grid Sinh trắc học và màn Chi tiết nhân viên
