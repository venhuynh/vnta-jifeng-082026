# 03 - Kế hoạch refactor và decision gate

## Đầu vào

Dán Feature Refactor Manifest, source map bước 01 và audit register bước 02.

## Prompt

Hãy tạo kế hoạch refactor read-only cho {{feature.display_name}}. Đọc AGENTS.md, kiểm tra git status --short --branch và không thay đổi source/config/migration/tài liệu/commit ở bước này.

Chia thay đổi thành lát use case có thể kiểm chứng, ví dụ Read/Search, Export, Refresh/Recalculate, Manual adjustment, Lock/Unlock. Không tách UI và backend thành hai đợt dài khiến contract drift; mỗi lát phải có đường đi hoàn chỉnh từ UI đến persistence/test khi được triển khai.

Với mỗi lát, đưa ra bảng:

| Thứ tự | Use case | Mục tiêu boundary | File sẽ chạm | Contract/consumer bảo toàn | Rủi ro | Test/verification | Điều kiện hoàn tất |
| --- | --- | --- | --- | --- | --- | --- | --- |

Tạo Compatibility Ledger:

| Contract/route/interface/DTO | Consumer được chứng minh | Giữ/đổi/xóa | Lý do | Contract test | Exit plan nếu legacy |
| --- | --- | --- | --- | --- | --- |

Tạo Decision Gate chỉ cho thay đổi có semantic impact:

| Quyết định | Hiện trạng có bằng chứng | Phương án | Khuyến nghị | Cần người dùng phê duyệt? | Tác động nếu chưa duyệt |
| --- | --- | --- | --- | --- | --- |

Decision Gate bắt buộc gồm route/public payload, authorization, schema/migration, business formula, canonical writer/data ownership, data scope/tenant và xóa legacy consumer. Không yêu cầu phê duyệt cho refactor nội bộ giữ nguyên behavior nếu source map chứng minh an toàn.

Tạo Branch Gate trước mọi lát code:

| Base branch | Base commit | Nhánh mới bắt buộc | Worktree sạch? | Lệnh tạo nhánh dự kiến | Trạng thái |
| --- | --- | --- | --- | --- | --- |

Branch Gate phải dùng branch.base và branch.name trong manifest. Bước kế hoạch này không tạo/chuyển nhánh. Trước source/config edit đầu tiên, implementation prompt phải xác minh worktree sạch, branch.name chưa tồn tại, tạo nhánh mới từ base bằng git switch --create và xác minh nhánh hiện tại. Nếu gate chưa đạt, không refactor trên branch base và không reset/stash thay đổi người dùng.

Mỗi bước code phải chỉ ra Branch Gate, lệnh build/test phù hợp, scope staging/commit theo AGENTS.md và rollback logic không phá thay đổi người dùng. Nếu manifest là AUDIT_ONLY, chỉ xuất kế hoạch; không chuyển sang code.

Kết thúc bằng thứ tự prompt 04 đến 15 cần chạy, các bước có thể song song, các bước bị gate và trạng thái GO hoặc NEEDS_USER_DECISION.
