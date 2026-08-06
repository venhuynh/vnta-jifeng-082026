# 06 - Refactor policy, rule và validation source of truth

## Đầu vào

Dán Feature Refactor Manifest, Writer and Invariant Matrix, Application contract của lát {{USE_CASE_SLICE}}, và toàn bộ rule/period/validation hiện có tìm được ở UI, API, Application và Infrastructure.

## Prompt

Hãy chuẩn hóa policy và validation cho {{USE_CASE_SLICE}} của {{feature.display_name}}. Đọc AGENTS.md và git status --short --branch. Chỉ sửa code trong phạm vi được phê duyệt; không đổi business semantics nếu manifest không cho phép. Trước source/config edit, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout; nếu không, dừng an toàn và báo blocker.

Mục tiêu là một source of truth có thể test, không phải gom tất cả code vào một class lớn.

- Đặt business calculation, threshold, classification, period support, effective-rule selection và invariant độc lập vào pure policy/calculator/validator phù hợp.
- Tách source adapter khỏi policy: Infrastructure chỉ đọc raw data/config rồi chuyển input trung lập; policy không biết DbContext, SQL, HTTP hoặc Blazor.
- Chọn canonical source cho từng rule. UI không lặp literal nghiệp vụ; nếu UI cần giải thích rule, expose read-only metadata/DTO từ server hoặc dùng shared policy đúng dependency direction.
- Phân biệt validation UX và validation authority. UI có thể báo sớm, nhưng Application/command phải validate lại mọi rule, period, scope, lock và editable combination.
- Nếu period/filter có selected và applied state, nêu rõ ownership/state transition. Khoảng period hợp lệ phải nhất quán tại read/query và command, không để một layer tự normalize còn layer khác reject khác nghĩa.
- Rule theo hiệu lực phải do server xác định từ period/config canonical; client không tự chọn rule/amount có quyền ghi.
- Viết test boundary cho null/invalid input, min/max/effective period, threshold/rounding, override và combination invalid. Test phải chứng minh behavior cũ được giữ nếu scope là structural refactor.
- Comment policy/calculator/validator chỉ khi cần làm rõ canonical source, ý nghĩa business, effective period, unit/rounding, override hoặc invariant không thể hiện rõ qua API. Không biến formula hiển nhiên thành comment theo dòng.

Không copy threshold, class, formula hoặc constants từ Phụ cấp chuyên cần. Nếu source hiện tại có nhiều literal mâu thuẫn mà không thể xác định canonical semantics, mở Decision Gate thay vì chọn một giá trị theo phỏng đoán.

Kết thúc bằng bảng rule trước/sau, canonical owner, nơi UI nhận metadata, các test mới và lệnh verification đã chạy. Áp dụng quy tắc build/test/commit của AGENTS.md như bước 05 nếu đây là work item độc lập.
