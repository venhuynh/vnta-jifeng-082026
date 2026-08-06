# 04 - Data ownership, atomicity và compatibility gate

## Đầu vào

Dán Feature Refactor Manifest, source map, audit register và kế hoạch/decision gate đã được chấp thuận. Với mỗi use case mutation, dán contract hiện có và các entity/projection liên quan.

## Prompt

Hãy chốt thiết kế dữ liệu cho {{feature.display_name}} trước khi refactor code. Đọc AGENTS.md và kiểm tra git status --short --branch. Đây là bước thiết kế/read-only trừ khi manifest cho phép rõ ràng tạo hoặc cập nhật tài liệu phân tích; không sửa source/config/migration và không commit.

Không lấy tên field hay business rule của Phụ cấp chuyên cần làm mặc định. Chỉ áp dụng các nguyên tắc: canonical writer duy nhất, projection read-only, command atomic, server-authoritative validation, lock/concurrency/audit có bằng chứng.

Tạo Writer and Invariant Matrix cho toàn bộ field được hiển thị hoặc thay đổi:

| Use case | Field UI có thể gửi | Canonical writer | Read-only/derived/projection consumer | Final-state invariant | Command atomic | Transaction boundary | Concurrency token | Lock/precondition | Audit action/entity/property | Compatibility |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

Áp dụng các quy tắc bắt buộc sau:

- Một field chỉ có một canonical writer. Nếu aggregate/màn hình khác hiển thị bản sao, bản sao là projection; generic/manual update của consumer không được nhận hoặc ghi đè field đó.
- Request command chỉ allowlist các field user thực sự được phép sửa. Không bind persistence entity, không nhận derived amount/status/rule/actor/tenant từ client.
- Nếu một thao tác thay đổi nhiều field phụ thuộc nhau, UI gọi đúng một command. Command validate trạng thái cuối cùng trước khi persist; không để UI gọi hai endpoint mutation nối tiếp rồi tự bù lỗi.
- Nếu command thay đổi nhiều record/bảng trong cùng invariant, xác định transaction boundary và thứ tự claim/concurrency. Không trả success khi projection liên quan chưa đồng bộ.
- Mọi mutation chọn dòng phải định nghĩa rõ concurrency token. Stale update, not found, locked, validation và forbidden có semantic lỗi khác nhau và mapping HTTP/test tương ứng.
- Mọi mutation phải enforce lock ở server, kể cả row action, batch action, refresh/recalculate hoặc generic command. Khi có selected và whole-period scope, định nghĩa rõ null, empty list và non-empty list; không để empty vô tình chuyển nghĩa thành whole period.
- Actor, tenant, organization, period scope và ownership lấy từ server/principal/canonical aggregate, không tin payload client.
- Audit phải nêu action, entity/property hoặc operation audit thực tế. Tracked write và raw/bulk write có cơ chế audit khác nhau; kiểm tra policy allowlist và masking field nhạy cảm.
- Legacy route/interface/DTO chỉ giữ khi source map có consumer. Ghi consumer, behavior, contract test, thời hạn/điều kiện xóa; wrapper không được trở thành dependency của code mới.

Nếu canonical writer, schema, semantic command hoặc public compatibility cần đổi mà manifest chưa phê duyệt, tạo Decision Gate và không tự ý code phần đó. Nếu discovery chứng minh không có mutation, ghi N/A có evidence.

Kết thúc bằng matrix hoàn chỉnh, danh sách invariant có thể test, compatibility ledger cập nhật và kiến trúc command đã được chốt để các bước 05-10 triển khai.
