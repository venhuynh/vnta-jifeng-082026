# Màn Hình Quản Lý Máy Chấm Công

Tài liệu này phân tích mô hình `ZktecoDevice` trong `src/zkteco-adms-gateway` để thiết kế màn hình quản lý Máy chấm công cho HRM.

## Nguồn phân tích

- `src/zkteco-adms-gateway/Domain/ZktecoDevice.cs`
- `src/zkteco-adms-gateway/Data/ZktecoDbContext.cs`
- `src/zkteco-adms-gateway/Security/DeviceAuthorizationService.cs`
- `src/zkteco-adms-gateway/Protocol/Handlers/HandshakeHandler.cs`
- `src/zkteco-adms-gateway/Integration/DeviceOptionsSyncService.cs`
- `src/zkteco-adms-gateway/Integration/DeviceCommandCallbackService.cs`
- `src/zkteco-adms-gateway/Integration/DeviceCommandPollingService.cs`
- `src/zkteco-adms-gateway/Security/VntaCrypto.cs`
- [`project/hrm-list-screen-blueprint.md`](./hrm-list-screen-blueprint.md)

## Kết luận chính từ source

`ZktecoDevice` không chỉ là danh mục máy chấm công. Đây là bản ghi trung tâm cho:

- thông tin đăng ký máy
- thông tin nhận diện và cấu hình kết nối
- tham số đồng bộ giữa máy và gateway
- số liệu thống kê thiết bị
- dấu mốc đồng bộ log
- heartbeat giao tiếp gần nhất

Vì vậy màn hình HRM không nên coi tất cả field của `ZktecoDevice` là field nhập liệu thông thường.

## Phân nhóm dữ liệu

### 1. Nhóm đăng ký máy

Đây là nhóm nên cho người dùng HRM quản lý trực tiếp:

- `Code`
- `Name`
- `SerialNumber`
- `Location`
- `IsInUse`

Ý nghĩa:

- `Code`: mã máy nội bộ của HRM và gateway.
- `Name`: tên hiển thị của máy.
- `SerialNumber`: định danh quan trọng nhất để gateway nhận diện thiết bị.
- `Location`: vị trí đặt máy.
- `IsInUse`: cờ đánh dấu máy còn được sử dụng hay không.

### 2. Nhóm nhận diện và kết nối

Đây là nhóm nên hiển thị chính trên grid, nhưng đa số nên là chỉ đọc:

- `IpAddress`
- `MacAddress`
- `Port`
- `VendorName`
- `DeviceModel`
- `FirmwareVersion`
- `FingerprintVersion`
- `TimeZone`

Lý do:

- Các field này đang được `DeviceOptionsSyncService` và `DeviceCommandCallbackService` cập nhật từ payload thực tế máy gửi lên.
- Nếu cho sửa tay toàn bộ trong popup, người dùng có thể ghi đè lên dữ liệu runtime vừa đồng bộ.

### 3. Nhóm cấu hình đồng bộ

Đây là nhóm cấu hình kỹ thuật của gateway khi trả handshake về máy:

- `TransferFlag`
- `Delay`
- `Realtime`
- `TransInterval`
- `TransTimes`
- `Encrypt`
- `ErrorDelay`
- `Timeout`
- `SyncTime`

Nhận xét:

- `HandshakeHandler` dùng trực tiếp các giá trị này để tạo response trả về máy.
- Nhóm này có thể cho phép chỉnh sửa, nhưng nên đặt trong khu vực `Cấu hình nâng cao`, không để lẫn với thông tin đăng ký máy.

### 4. Nhóm thống kê và dấu mốc đồng bộ

Đây là nhóm chỉ đọc:

- `UserCount`
- `AttendanceLogCount`
- `FingerprintCount`
- `AttendanceLogStamp`
- `AttendancePhotoStamp`
- `OperationLogStamp`
- `ErrorLogStamp`
- `MultiBioDataSupport`
- `IrTempDetectionFunOn`
- `MaskDetectionFunOn`
- `LastRequestTime`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Lý do:

- Các field này phản ánh trạng thái thực tế của thiết bị hoặc hệ thống gateway.
- Một số field được cập nhật trong luồng đồng bộ log, options, callback hoặc authorization.

### 5. Trường cần xử lý cẩn trọng

- `ActivationCode`
- `Status`

#### `ActivationCode`

Source cho thấy gateway chỉ chấp nhận máy khi:

- `SerialNumber` tồn tại trong DB
- `ActivationCode` hợp lệ với `SerialNumber`

Theo `VntaCrypto`, mã kích hoạt được kiểm tra dựa trên serial. Vì vậy UI nên:

- hiển thị `ActivationCode`
- cho người dùng nhập tay `ActivationCode` khi tạo mới hoặc điều chỉnh
- kiểm tra đúng thuật toán gateway với format `VN1-XXXX-XXXX-XXXX-XXXX`
- để trạng thái kích hoạt thực tế diễn ra ngầm ở backend hoặc gateway sau khi record hợp lệ được lưu

#### `Status`

`Status` hiện là `int`, nhưng source đang dùng cho `ZktecoDevice` chưa cho biết mapping nghiệp vụ rõ ràng. Vì vậy:

- không nên cho người dùng sửa trực tiếp
- không nên hiển thị caption trạng thái nghiệp vụ cuối cùng dựa trên field này nếu chưa có bảng mã
- nếu cần hiển thị trước mắt, chỉ nên dùng nhãn kỹ thuật như `Trạng thái mã nguồn`

## Thiết kế màn hình đề xuất

Màn hình này dùng chuẩn danh sách HRM:

1. Toolbar trên cùng
2. Grid danh sách máy ở vùng chính
3. Popup form khi thêm hoặc điều chỉnh

Không nên dùng `TreeList` vì `ZktecoDevice` là dữ liệu phẳng.

Về phân loại pattern:

- đây là `Master Data List Page` có thêm action kỹ thuật riêng
- không nên tự biến thành `Operational List Page` nếu chưa có cohort/filter
  nghiệp vụ thật sự
- khi cần chuẩn hóa chi tiết UI, đọc thêm:
  - `doc/project/hrm-list-screen-blueprint.md`
  - `doc/checklists/ui-screen-checklist.md`
  - `doc/checklists/operational-list-screen-checklist.md` cho các action vận
    hành riêng như đồng bộ, tạo lệnh hoặc query INFO

## Toolbar

Theo blueprint chuẩn:

- Bên trái: `Máy chấm công`
- Bên phải trên một hàng:
  - `Mới`
  - `Điều chỉnh`
  - `Xóa`
  - `Làm mới`
  - `Xuất dữ liệu`
  - `Chọn cột`
  - `Tìm kiếm`

### Đề xuất action mở rộng

Không đưa vào nhóm CRUD chính, nhưng nên cân nhắc cho pha sau:

- `Sao chép mã kích hoạt`
- `Đồng bộ thông tin máy`
- `Xem lịch sử lệnh`

Các action này phù hợp hơn ở toolbar phụ, menu ngữ cảnh, hoặc popup chi tiết kỹ thuật.

## Grid danh sách chính

### Cột nên hiển thị mặc định

- `Tên máy` từ `Name`
- `Số serial` từ `SerialNumber`
- `IP` từ `IpAddress`
- `MAC Address` từ `MacAddress`
- `Vị trí` từ `Location`
- `Trạng thái nguồn` từ `Status`
- `Model` từ `DeviceModel`
- `Đang sử dụng` từ `IsInUse`
- `Lần liên hệ cuối` từ `LastRequestTime`

### Cột nên cho phép bật thêm bằng Column Chooser

- `MAC Address`
- `Firmware`
- `Phiên bản vân tay`
- `Múi giờ`
- `Số người dùng`
- `Số log chấm công`
- `Số mẫu vân tay`
- `AttendanceLogStamp`
- `AttendancePhotoStamp`
- `OperationLogStamp`
- `ErrorLogStamp`
- `Ngày tạo`
- `Ngày cập nhật`

### Cột trạng thái hiển thị nên có

UI hiện tại đang hiển thị trực tiếp `Status` dưới caption kỹ thuật `Trạng thái nguồn`.

Nếu cần tiến thêm một bước ở pha sau, có thể dựng lại trạng thái tổng hợp từ source:

- `Chưa đăng ký serial`
  - khi `SerialNumber` rỗng
- `Chưa kích hoạt`
  - khi có `SerialNumber` nhưng `ActivationCode` không hợp lệ
- `Đang hoạt động`
  - khi đã kích hoạt và có `LastRequestTime`
- `Chưa ghi nhận kết nối`
  - khi đã kích hoạt nhưng chưa có `LastRequestTime`

Ghi chú:

- Source chưa có quy tắc chính thức để xác định `Mất kết nối` theo số phút không heartbeat.
- Nếu muốn có badge `Ngoại tuyến`, cần chốt thêm một ngưỡng thời gian riêng.

## Popup thêm mới hoặc điều chỉnh

Popup nên chia 3 nhóm rõ ràng.

### Ghi chú triển khai popup DevExpress

- Màn này phải dùng đúng popup edit form của `DxGrid`, không tự dựng inline form trong thân page nếu mục tiêu là hành vi giống `ReferenceCode`.
- Nút `Mới` trên toolbar nên gọi `StartEditNewRowAsync()`.
- Nút `Điều chỉnh` nên gọi `StartEditDataItemAsync(...)` với item đang chọn.
- `DxGrid` nên giữ `EditMode="GridEditMode.PopupEditForm"` và có `DxGridCommandColumn` trong `Columns`, kể cả khi cột command được ẩn. Kinh nghiệm thực tế ở HRM cho thấy cách này giữ vòng đời edit command ổn định hơn khi popup được gọi từ toolbar ngoài grid.
- Nếu popup đã được grid kích hoạt nhưng nhìn như "nằm trong trang", phải kiểm tra `overflow` của layout cha trước. Trong app HRM, lớp bọc kiểu `drawer-content` không được chặn overlay của DevExpress.
- Trong giai đoạn debug popup edit form, nên ưu tiên cấu hình grid sát `ReferenceCode` và chưa bật thêm biến thể render như virtual scrolling cho tới khi popup đã ổn định.

Popup hiện tại dùng một form gọn thống nhất cho cả `Mới` và `Điều chỉnh`, chỉ còn 5 field:

- `Tên máy`
- `Số serial`
- `Đang sử dụng`
- `Vị trí`
- `Mã kích hoạt`

Ghi chú triển khai:

- Không còn group `Thông tin khởi tạo`; form được trải theo một flow nhập liệu ngắn.
- `Đang sử dụng` được đặt cạnh `Tên máy` để ưu tiên trạng thái vận hành ngay ở phần đầu popup.
- Popup có vùng cuộn riêng để không bị kẹt chiều cao khi host layout giới hạn không gian hiển thị.

## Validation đề xuất

### Bắt buộc

- `Mã máy` không được rỗng
- `Tên máy` không được rỗng
- `Số serial` không được rỗng khi lưu chính thức

### Độ dài

Theo `ZktecoDevice.cs`:

- `Code`: tối đa 100
- `Name`: tối đa 250
- `SerialNumber`: tối đa 50
- `IpAddress`: tối đa 50
- `MacAddress`: tối đa 50
- `Location`: tối đa 500
- `ActivationCode`: tối đa 200

### Quy tắc bổ sung nên có

- `Số serial` nên được chuẩn hóa in hoa và bỏ ký tự thừa theo hướng của `VntaCrypto.NormalizeSerial(...)`.
- `Số serial` chỉ được nhập ở luồng tạo mới; sau khi thiết bị đã được tạo thì không cho phép điều chỉnh.
- `Mã kích hoạt` là field bắt buộc.
- `Mã kích hoạt` phải đúng shape `VN1-XXXX-XXXX-XXXX-XXXX`.
- Với record tạo mới hoặc điều chỉnh, `Mã kích hoạt` phải pass `VntaCrypto.ValidateActivationCode(serial, activationCode)` thì mới được lưu.
- `Port` nếu nhập tay phải là số dương hợp lệ.
- Không cho sửa `Status` trực tiếp.

## Luồng CRUD đề xuất

### Mới

- Tạo mới một bản ghi máy chấm công.
- Luồng UI đúng là: click toolbar `Mới` -> grid vào edit mode -> DevExpress mở `PopupEditForm`.
- Ở trạng thái hiện tại của source `Vnta.HRM2026`, popup `Mới` chỉ hiển thị 5 input:
  - `Tên máy`
  - `Số serial`
  - `Vị trí`
  - `Đang sử dụng`
  - `Mã kích hoạt`
- Các field kỹ thuật còn lại được seed ngầm bằng giá trị mặc định ở tầng UI state trước khi lưu, ví dụ `IpAddress`, `MacAddress`, `Delay`, `Realtime`, `TransferFlag`, `TimeZone`, `LastRequestTime`, `VendorName` và các mốc stamp.
- Nếu không thấy popup, không được kết luận ngay là grid đang inline edit. Cần kiểm tra lần lượt event toolbar, event `CustomizeEditModel`, exception trong form edit và CSS overflow của layout.
- `Code` không còn là input hiển thị ở luồng `Mới`; UI tự sinh mã nội bộ trước khi lưu để không chặn workflow tạo nhanh.
- `Mã kích hoạt` là input bắt buộc do người dùng nhập.
- Nếu người dùng nhập tay `Mã kích hoạt` mà sai thuật toán gateway, thao tác lưu bị chặn ngay trên form.
- Sau khi record hợp lệ được lưu, backend hoặc gateway sẽ tự xử lý trạng thái kích hoạt thực tế ở luồng authorize thay vì UI tự ghi đè mã.
- `Vị trí lắp đặt` vẫn là field cho phép nhập khi tạo mới và điều chỉnh khi edit.

### Điều chỉnh

- Cho chỉnh sửa thông tin đăng ký máy.
- Chỉ quản trị kỹ thuật mới nên được sửa nhóm `Cấu hình đồng bộ nâng cao`.

### Xóa

- Chỉ nên cho xóa khi máy chưa còn được sử dụng hoặc chưa được ràng buộc nghiệp vụ khác.
- Vì source gateway đang dùng `SerialNumber` để authorize và đồng bộ log, thao tác xóa cần xác nhận mạnh bằng dialog.

## Dữ liệu nên có ở màn chi tiết hoặc popup phụ

Không nên nhồi vào grid chính:

- lịch sử lệnh từ `device_cmd`
- log vận hành
- log lỗi
- số liệu đồng bộ ảnh chấm công
- chi tiết khả năng sinh trắc học `MultiBioDataSupport`

Các phần này phù hợp với:

- popup phụ `Thông tin kỹ thuật`
- popup `Lịch sử lệnh`
- màn chi tiết máy chấm công ở pha sau

## Đề xuất hiện thực trong HRM

### Route

- `/attendance/devices`

### Tên page và component

- `MayChamCong.razor`
- `MayChamCong.razor.css`
- `MayChamCong.razor.cs`
- `AttendanceDeviceRecord.cs`
- `AttendanceDeviceDataProvider.cs`
- `IAttendanceDeviceService.cs`
- `DatabaseAttendanceDeviceService.cs`
- `AttendanceGatewayDeviceDbContext.cs`

### Ghi chú source hiện hành

- Route `/attendance/devices` hiện nối vào source thật tại `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor`.
- Menu `Máy chấm công` hiện là node top-level dưới nhóm `Quản trị` trong cây menu động `VntaNavMenuCatalog`.
- Dữ liệu hiện đi qua `AttendanceDeviceDataProvider`, nhưng provider này đã là adapter gọi `IAttendanceDeviceService` ở tầng server thay vì giữ dataset in-memory cục bộ.
- Tầng `Infrastructure` đã map trực tiếp bảng `devices` của PostgreSQL dùng chung với gateway qua `AttendanceGatewayDeviceDbContext`.
- Thuật toán kiểm tra `Mã kích hoạt` đã được đưa lên `Vnta.Hrm.Application.Attendance.AttendanceDeviceActivationCode` để UI, service DB và gateway dùng cùng một nguồn logic.

### Thành phần dùng lại

- `DxToolbar`
- `DxGrid`
- `HrmLoadingPanel`
- `IHrmToastService`
- `IHrmDialogService`

### Command thiết bị và popup thông tin

- Toolbar có menu `Tạo lệnh`, áp dụng cho một hoặc nhiều máy đang chọn có
  `SerialNumber`.
- `Truy vấn thông tin` tạo một command `Content = "INFO"` cho mỗi serial hợp lệ.
- `Khởi động lại` tạo một command `Content = "REBOOT"` cho mỗi serial hợp lệ và có
  dialog xác nhận.
- Các action mới dùng DevExpress Icon Library qua `IconUrl` hoặc `VntaDevExpressIcons`; không dùng Bootstrap Icons, CDN `bootstrap-icons`, class `bi` hoặc `bi-*`.
- Icon `Chi tiết` nằm sau tên máy và mở popup độc lập với selection toolbar.
- Popup query record `device_cmd` mới nhất có cùng serial, `Content = "INFO"` và
  `ResponseTime != null`.
- `ReturnValue` được parse ở tầng Application, sau đó map key kỹ thuật sang nhãn tiếng
  Việt tại Web.Client.
- Grid popup chỉ đọc có hai cột `Thông tin`/`Giá trị`, hỗ trợ search, filter row/menu và
  dùng `ShowAllRows` không phân trang vì tập thông số INFO có giới hạn tự nhiên nhỏ.

### Checklist debug popup "Mới"

1. Xác nhận nút toolbar gọi đúng `StartEditNewRowAsync()`.
2. Xác nhận grid có `EditMode="PopupEditForm"` và `EditFormTemplate`.
3. Xác nhận trong `Columns` có `DxGridCommandColumn`; có thể ẩn nhưng không nên bỏ hẳn khi popup do toolbar điều khiển.
4. Xác nhận `EditFormTemplate` không ném exception runtime.
5. Xác nhận các editor read-only dùng one-way binding đã tắt validation nếu cần.
6. Xác nhận layout cha không chặn popup bằng `overflow: hidden` hoặc vùng scroll sai chỗ.

## Kết luận

Từ source hiện tại, màn `Máy chấm công` nên được hiểu là màn quản lý đăng ký thiết bị kèm quan sát trạng thái gateway, không phải chỉ là form CRUD thuần.

Thiết kế an toàn nhất cho pha đầu là:

- grid quản lý danh sách máy
- popup chỉ tập trung vào thông tin đăng ký và một phần cấu hình nâng cao
- tách phần telemetry, stamp, đếm log và lịch sử lệnh thành dữ liệu chỉ đọc

Điều này phù hợp với cách `ZktecoDevice` đang được dùng thực tế trong gateway và giảm rủi rõ ghi đè sai dữ liệu runtime do thiết bị gửi về.


