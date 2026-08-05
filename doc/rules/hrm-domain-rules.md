# Quy Tắc Nghiệp Vụ HRM

Áp dụng cho chức năng nhân sự, tổ chức, chấm công, nghỉ phép, lương thưởng và hồ sơ người lao động.

## 1. Dữ liệu nhân sự là dữ liệu nhạy cảm

- Không ghi lộ thông tin cá nhân vào log, comment hoặc dữ liệu mẫu công khai.
- Cẩn trọng với CCCD, số điện thoại, email, ngày sinh, lương và thông tin hợp đồng.
- Mọi màn hình xem/sửa dữ liệu nhạy cảm cần nghĩ đến phân quyền.

## 2. Nghiệp vụ phải rõ trạng thái

- Các quy trình như nghỉ phép, tăng ca, điều chuyển, hợp đồng, quyết định lương phải có trạng thái rõ ràng.
- Không dùng chuỗi trạng thái tùy tiện nếu có thể dùng enum hoặc hằng số tập trung.
- Caption trạng thái phải là tiếng Việt.

## 3. Không tự bịa luật nghiệp vụ

- Không tự đặt công thức lương, cách tính công, quy định nghỉ phép hoặc phê duyệt khi chưa có yêu cầu.
- Nếu cần dữ liệu giả để dựng UI, phải ghi rõ đó là dữ liệu mẫu.
- Khi thiếu quy tắc nghiệp vụ, ưu tiên tạo điểm mở rộng thay vì khóa cứng giả định.

## 4. Truy vết thay đổi

- Các nghiệp vụ quan trọng nên thiết kế có khả năng truy vết người tạo, người sửa và thời điểm thay đổi.
- Với dữ liệu nhạy cảm, ưu tiên cập nhật có kiểm soát hơn là sửa trực tiếp không dấu vết.

