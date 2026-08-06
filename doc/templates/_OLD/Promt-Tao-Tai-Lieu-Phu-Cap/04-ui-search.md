# Prompt 04 — Mô tả UI — Search

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
<THU_MUC_TAI_LIEU_DICH>/04-ui-search.md

Phân tích DxSearchBox hoặc search control tương đương: placeholder, field/value binding, debounce/delay, trigger, scope tìm kiếm, client/server filtering, cancellation, reset, loading/error và ảnh hưởng tới paging/selection.
Nếu màn không có search, ghi N/A có bằng chứng.

CẤU TRÚC BẮT BUỘC
1. Inventory search/N/A.
2. Bảng thuộc tính và handler.
3. Sequence nhập từ khóa → dữ liệu hiển thị.
4. Edge cases, accessibility và bằng chứng source.
```
