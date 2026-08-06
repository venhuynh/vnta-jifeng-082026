# Prompt 14 — Logic dòng dữ liệu — Chi tiết

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
<THU_MUC_TAI_LIEU_DICH>/14-row-chi-tiet.md

Kiểm tra action Chi tiết/View details của từng dòng. Nếu có, mô tả trigger, row identity, quyền, API/read model, popup/page điều hướng, dữ liệu hiển thị, loading/error và hành vi khi row bị khóa.
Nếu không có action Chi tiết, ghi N/A và chỉ rõ action gần nhất nhưng không đánh đồng chúng.

CẤU TRÚC BẮT BUỘC
1. Kết quả inventory/N/A.
2. Trigger và điều kiện theo dòng.
3. Sequence và data contract.
4. UI state/error/test.
5. Bằng chứng source.
```
