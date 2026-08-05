# Hướng Dẫn Lấy Mã Kích Hoạt Thiết Bị

Tài liệu này mô tả cách lấy `Mã kích hoạt` cho thiết bị chấm công trong đúng ngữ cảnh của repo `Vnta-Blazor-2026`, nơi `HRM` và `adms-gateway` đang dùng chung một thuật toán sinh mã.

## Phạm vi áp dụng

- `src/Vnta.HRM2026`
- `src/zkteco-adms-gateway`

Tài liệu này không dựa vào tool ngoài repo làm nguồn chân lý. Nguồn logic chuẩn nằm ngay trong source hiện tại của `HRM` và `adms-gateway`.

## Nguồn logic chuẩn

Hai file sau phải luôn cho ra cùng một kết quả:

- [AttendanceDeviceActivationCode.cs](/C:/Users/VNSIT/Documents/GitHub/Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Application/Attendance/AttendanceDeviceActivationCode.cs)
- [VntaCrypto.cs](/C:/Users/VNSIT/Documents/GitHub/Vnta-Blazor-2026/src/zkteco-adms-gateway/Security/VntaCrypto.cs)

Ý nghĩa:

- `HRM` dùng `AttendanceDeviceActivationCode.Generate(...)` để sinh và kiểm tra mã.
- `adms-gateway` dùng `VntaCrypto.GenerateActivationCode(...)` và `ValidateActivationCode(...)` để xác minh thiết bị ở runtime.
- Nếu mã không khớp cùng một `SerialNumber`, gateway sẽ không xem thiết bị là hợp lệ.

## Cách lấy mã trong ứng dụng HRM

Đây là cách vận hành chuẩn, không cần tool riêng:

1. Mở màn `Máy chấm công` tại route `/attendance/devices`.
2. Tạo mới hoặc điều chỉnh một thiết bị.
3. Nhập `Số serial`.
4. Để trống ô `Mã kích hoạt` rồi lưu.

Kết quả:

- hệ thống sẽ tự chuẩn hóa serial;
- nếu serial hợp lệ, `HRM` sẽ tự sinh `Mã kích hoạt` chuẩn gateway trước khi lưu;
- nếu người dùng nhập tay mã kích hoạt, mã đó vẫn sẽ bị kiểm tra lại theo cùng thuật toán.

Luồng này đang được xử lý trong:

- [MayChamCong.razor.cs](/C:/Users/VNSIT/Documents/GitHub/Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor.cs)
- [DatabaseAttendanceDeviceService.cs](/C:/Users/VNSIT/Documents/GitHub/Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/Integrations/AttendanceGateway/DatabaseAttendanceDeviceService.cs)

## Quy tắc chuẩn hóa serial

Trước khi tính mã, hệ thống luôn chuẩn hóa `Số serial` theo cùng một quy tắc:

- cắt khoảng trắng đầu cuối;
- đổi sang chữ in hoa;
- bỏ mọi ký tự không phải chữ cái tiếng Anh hoặc chữ số.

Ví dụ:

- `cq ug-221860047` -> `CQUG221860047`
- ` dejb213260141 ` -> `DEJB213260141`

## Định dạng mã kích hoạt

Mã chuẩn có dạng:

```text
VN1-XXXX-XXXX-XXXX-XXXX
```

Trong đó:

- tiền tố luôn là `VN1`;
- phần còn lại dùng bộ ký tự base32 nội bộ của dự án;
- `HRM` và `adms-gateway` đều kiểm tra theo cùng định dạng này.

## Ví dụ đối chiếu

Theo thuật toán hiện tại trong source:

```text
Serial gốc       : CQUG221860047
Serial chuẩn hóa : CQUG221860047
Mã kích hoạt     : VN1-TK9K-JAA5-JEJH-SBXA
```

```text
Serial gốc       : DEJB213260141
Serial chuẩn hóa : DEJB213260141
Mã kích hoạt     : VN1-LG8K-5JSQ-QSRJ-67KN
```

## Lưu ý vận hành

- Hiện tại repo này chưa có một console tool riêng để sinh mã kiểu `HyperTech.ActivationTool`.
- Trong bối cảnh triển khai hiện tại, nguồn chân lý là logic trong `AttendanceDeviceActivationCode` và `VntaCrypto`.
- Không dùng cờ trạng thái trong database để suy ra thiết bị đã kích hoạt đúng hay chưa.
- Ở runtime, `adms-gateway` chỉ tin vào cặp `SerialNumber` và `ActivationCode` sau khi chuẩn hóa.
- Nếu đổi `Số serial`, phải sinh lại `Mã kích hoạt` tương ứng với serial mới.

## Khi nào cần làm tool riêng

Chỉ nên tách thêm tool console riêng khi đội vận hành thật sự cần:

- sinh mã hàng loạt ngoài UI;
- đối chiếu mã trên máy không cài `HRM`;
- cấp phát mã trong quy trình triển khai thiết bị tại hiện trường.

Nếu mở nhánh này, tool phải gọi lại đúng thuật toán đang có trong:

- `Vnta.Hrm.Application.Attendance.AttendanceDeviceActivationCode`
- hoặc `Vnta.AttendanceGateway.Security.VntaCrypto`

và không được tự viết một phiên bản thuật toán khác.
