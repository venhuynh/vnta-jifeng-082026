# Prompt — Tách domain policy và business rules

```text
Bạn là domain modeling engineer .NET. Hãy tách và chuẩn hóa business rules của feature dưới đây thành policy/calculator testable, không thay đổi nghiệp vụ khi chưa được cấp quyền. Đây là tác vụ IMPLEMENT.

## Đầu vào
- Feature group / name: `KhauTru` / `KhauTruPhiCongDoan` — Khấu trừ phí công đoàn
- Rule hiện tại cần tách: Tạo snapshot theo kỳ lương từ dữ liệu nguồn khấu trừ tổng hợp; tính lại chỉ các dòng chưa khóa; cho phép điều chỉnh thủ công số tiền từ `0` đến `9_999_999_999_999_999.99`, tối đa 2 chữ số thập phân; không cho sửa khi dòng hoặc kỳ đã khóa; bảo toàn optimistic concurrency và audit.
- Thay đổi nghiệp vụ được phép: Không; các quy tắc về kỳ lương, khóa/mở khóa, tính lại, giá trị thủ công và rounding/validation phải giữ nguyên.

## Bắt buộc
1. Khảo sát nơi rule đang nằm: UI model, endpoint, database service, SQL/LINQ, constants, validation và test.
2. Chọn boundary đúng:
   - Pure deterministic calculation ở Application/Policies.
   - Rule cần đọc nguồn dữ liệu ngoài ở interface policy/application service; adapter nằm Infrastructure/Policies.
3. Tạo input/result có tên theo nghiệp vụ; không dùng bool/primitive mơ hồ hoặc magic string rải rác.
4. Giữ calculation server-authoritative. UI chỉ hiển thị preview nếu cần và không trở thành nguồn chân lý.
5. Không đổi điều kiện/ngưỡng/rounding/lock/manual behavior ngầm. Nếu phát hiện mâu thuẫn UI với server, tạo characterization test, báo rõ blocker và chờ quyết định trước khi đổi semantics.
6. Xóa duplicate calculation chỉ sau khi tất cả consumer dùng policy chuẩn.
7. Viết unit tests gồm normal, boundary, invalid/negative, rounding và các rule ngoại lệ.

## Definition of Done
- Policy có trách nhiệm duy nhất, không phụ thuộc UI/EF/HTTP.
- Rule được gọi từ use case thích hợp thay vì persistence query god-class.
- Test policy pass; build/test feature pass.
- Báo cáo rule cũ→mới, semantic được giữ và các quyết định còn chờ.
```
