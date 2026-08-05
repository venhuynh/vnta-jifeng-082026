# Prompt — Chuẩn hóa test feature

```text
Bạn là .NET test engineer. Hãy xây dựng test suite theo feature cho feature dưới đây sau refactor. Đây là tác vụ IMPLEMENT; không thay đổi production behavior chỉ để test dễ pass.

## Đầu vào
- Feature group / name: `KhauTru` / `KhauTruPhiCongDoan` — Khấu trừ phí công đoàn
- Các use case/command: chuẩn bị snapshot kỳ lương, tìm kiếm/phân trang, tính lại, điều chỉnh thủ công, khóa/mở khóa từng dòng, khóa/mở khóa hàng loạt theo dòng chọn/toàn kỳ, xuất dữ liệu và đối chiếu bảng công tháng chỉ đọc.
- Test project hiện có: `src/Vnta.HRM2026/Vnta.Hrm.Infrastructure.Tests` và `src/Vnta.HRM2026/Vnta.Hrm.Web.Tests`.
- Test source/feature roots: `Infrastructure.Tests/KhauTru/KhauTruPhiCongDoan`, `Web.Tests/Endpoints/KhauTru/KhauTruPhiCongDoan` (bổ sung component/provider tests nếu cần).

## Bắt buộc
1. Đọc code và test hiện có, xác định lỗ hổng theo policy, query, command, endpoint, provider/UI workflow.
2. Đưa test feature vào đúng folder: `Infrastructure.Tests/[Feature]/[Feature]` và `Web.Tests/Endpoints/[Feature]/[Feature]`; không tạo test project mới nếu project hiện có phù hợp.
3. Bổ sung test tối thiểu:
   - policy/calculator: normal, boundary, invalid, rounding;
   - query: filter, paging, summary, export;
   - command: refresh/recalculate, manual adjustment, lock/unlock;
   - lock/manual/concurrency conflict và transaction behavior;
   - endpoint: authorization, client actor spoofing, validation, HTTP 409;
   - provider mapping cho UI model khi có transformation.
4. Test phải kiểm tra behavior nghiệp vụ, không mock implementation detail vô ích.
5. Dùng fixture/database integration theo convention repo; cleanup dữ liệu an toàn, không tác động database người dùng.

## Definition of Done
- Test có tên theo business behavior và nằm theo feature folder.
- Các defect/characterization quan trọng có test tái hiện.
- Chạy test feature và full relevant project; báo số pass/fail/skip.
- Nêu chính xác coverage/rủi ro còn thiếu.
```
