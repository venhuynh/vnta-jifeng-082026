# Prompt 21 — Quy tắc nghiệp vụ, validation và xử lý lỗi

Dán toàn bộ khối sau vào Codex.

```text
THÔNG TIN ĐẦU VÀO — NGƯỜI DÙNG PHẢI ĐIỀN
- Tên màn hình: <TEN_MAN_HINH>
- Menu/breadcrumb: <MENU_PATH_HOAC_KHONG_CO>
- Route/URL: <ROUTE_HOAC_KHONG_CO>
- Thư mục UI: <DUONG_DAN_UI>
- Đường dẫn source bổ sung: <DANH_SACH_DUONG_DAN_SOURCE_HOAC_DE_TRONG>
- Thư mục tài liệu đích: <THU_MUC_TAI_LIEU_DICH>
- Baseline tham chiếu (nếu có): <DUONG_DAN_BASELINE_HOAC_KHONG_AP_DUNG>

QUY TẮC
1. Kiểm tra các đường dẫn đầu vào trước khi phân tích. Nếu thiếu thư mục UI hoặc thư mục tài liệu đích, dừng và hỏi lại; không tự dùng source của màn khác.
2. Source runtime hiện hành là nguồn sự thật. Ghi đường dẫn và symbol/method làm bằng chứng cho mọi claim quan trọng.
3. Chỉ tạo/chỉnh sửa Markdown trong <THU_MUC_TAI_LIEU_DICH>; không sửa source production, migration, cấu hình hoặc dữ liệu.
4. Nếu control hoặc action được yêu cầu không tồn tại, ghi rõ “Không có trong source hiện hành”, kèm kết quả tìm kiếm/source đã kiểm tra; không suy diễn hành vi.
5. Phân biệt rõ: đã xác minh, suy luận và cần xác nhận. Kết thúc bằng file đã đổi, lệnh kiểm tra đã chạy, phát hiện và rủi ro/câu hỏi mở. Chạy git diff --check.

NHIỆM VỤ
Tạo hoặc cập nhật tài liệu:
<THU_MUC_TAI_LIEU_DICH>/21-quy-tac-validation-va-loi.md

Đối soát công thức/rule dữ liệu, ngưỡng biên, nguồn dữ liệu, validation client/server/database và error recovery cho toàn màn.
Lập rule matrix, validation matrix và error matrix; liên kết tới action cụ thể 09–18.

CẤU TRÚC BẮT BUỘC
1. Thuật ngữ/nguồn dữ liệu.
2. Rule và công thức.
3. Validation matrix.
4. Error/recovery/message matrix.
5. Edge cases, chưa xác minh và bằng chứng source.
```
