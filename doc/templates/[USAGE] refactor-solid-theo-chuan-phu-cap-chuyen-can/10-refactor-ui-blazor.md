# 10 - Refactor UI Blazor theo use case

## Đầu vào

Dán Feature Refactor Manifest, source map, UI action inventory, policy/rule metadata contract, client capability contract và lát {{USE_CASE_SLICE}}.

## Prompt

Hãy refactor UI Blazor cho {{feature.display_name}} theo lát {{USE_CASE_SLICE}}, giữ behavior/UX hiện có trừ thay đổi được manifest phê duyệt. Đọc AGENTS.md và git status --short --branch trước khi sửa. Trước source/config edit, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout; nếu không, dừng an toàn và báo blocker.

Áp dụng đúng mức cần thiết, không tạo folder/component chỉ để giống Phụ cấp chuyên cần:

- Page host sở hữu lifecycle, state và orchestration. Tách Section, Dialog, State, Model, Command, Presentation hoặc Export khi chúng có responsibility độc lập và giúp page không đồng thời sở hữu render, popup, filter, async flow và business mapping.
- Child component nhận data qua Parameter và phát tín hiệu qua EventCallback/callback rõ nghĩa; child không inject persistence/HTTP hoặc tự sửa parent state ngoài callback.
- UI chỉ inject capability DataProvider cần thiết. Không gọi DbContext, EF, raw HTTP, endpoint, Infrastructure service hoặc business calculator từ razor/code-behind.
- Dựng filter qua factory/state có trách nhiệm rõ. Phân biệt toolbar selection và applied dataset nếu feature có period/filter; mọi action mutation/export phải chống thao tác trên context chưa áp dụng.
- UI validation chỉ cho feedback nhanh. Rule amount/threshold/effective period/lock authority lấy từ server policy/metadata; popup quy tắc hiển thị dữ liệu canonical, không sao chép literal.
- Một thao tác người dùng tương ứng một command đã chốt. Không orchestration nhiều HTTP mutation để cập nhật các field có invariant chung.
- Thiết kế explicit loading, disabled state, error, empty state, retry, confirmation, selection/page/export lifecycle. Bảo vệ stale response bằng cancellation, disposal/version/snapshot khi request có thể chồng nhau.
- Với dữ liệu liên quan chỉ để xem, popup/read model phải read-only, server-scope và không trở thành writer của feature nguồn.
- Giữ accessibility, label/validation message, keyboard/focus popup, CSS isolation/layout/page-size behavior theo chuẩn UI repo. Không thay route/menu hoặc UI meaning nếu chưa được phê duyệt.
- Comment Razor/code-behind khi cần xác định state owner, lifecycle, EventCallback, applied-vs-selected context, cancellation/stale response, double-submit/concurrency UX hoặc lý do tách component. Dùng @* *@ cho comment chỉ dành cho source; không render ghi chú kỹ thuật ra DOM.

Kiểm tra build client bằng lệnh trong manifest khi phạm vi chạm Web.Client. Bổ sung test/UI workflow hoặc provider-level test cho state transition quan trọng: load, filter, save/409/locked, refresh, export, batch action, dispose/cancellation tùy use case có tồn tại.

Kết thúc bằng danh sách component/state responsibility trước-sau, capability injected, action-to-command map, UX behavior giữ lại/thay đổi, test/build result và commit theo AGENTS.md nếu đây là work item độc lập đã hoàn tất.
