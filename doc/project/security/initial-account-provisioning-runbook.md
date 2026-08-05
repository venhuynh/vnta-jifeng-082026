# Runbook cấp tài khoản ban đầu

Runbook này thay thế hoàn toàn cho bootstrap account trong runtime. Chỉ chủ vận hành Identity được ủy quyền mới được thực hiện.

1. Nhận yêu cầu đã phê duyệt, xác định nhân viên, role tối thiểu và thời hạn truy cập.
2. Tạo hoặc liên kết hồ sơ nhân viên trước; không dùng danh tính demo hoặc account dùng chung.
3. Cấp tài khoản qua API/chức năng quản trị có policy `EmployeeAccountAdministration`; việc phê duyệt dùng policy `EmployeeAccountApproval` theo tách nhiệm vụ.
4. Gửi password tạm hoặc link thiết lập password bằng kênh ngoài repository; buộc đổi password ở lần đăng nhập đầu nếu quy trình Identity hỗ trợ.
5. Ghi audit: mã yêu cầu, người cấp, người phê duyệt, employee ID đã ẩn danh trong báo cáo, role được cấp và thời gian. Không ghi password/token vào audit hay implementation log.
6. Kiểm tra login bằng account vừa cấp, xác nhận chỉ có capability tối thiểu; thu hồi ngay nếu yêu cầu bị hủy hoặc nhân sự không còn hiệu lực.

Không chạy seed tự động lúc ứng dụng khởi động. Mọi bulk provisioning phải có script/rà soát riêng, idempotent, có dry-run và được owner vận hành phê duyệt.
