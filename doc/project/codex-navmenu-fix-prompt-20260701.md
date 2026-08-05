# Prompt fix NavMenu cho các branch khác

Đây là prompt sẵn để dán vào AI agent trên các branch khác khi cần lấy baseline NavMenu chuẩn từ `origin/main` và sửa nhóm lỗi menu/render/icon/folder mismatch.

## Source of truth

- Branch chuẩn: `main`
- Tài liệu chuẩn nằm trên `origin/main`
- Baseline menu/source đã vào `main` qua PR `#10`

## Prompt sẵn để dán

```text
Bạn đang làm việc trong repo JIFENG HRM trên một branch khác, không phải main.

Mục tiêu của bạn là sửa nhóm lỗi liên quan đến NavMenu, DxTreeView, menu icon, current module, folder/namespace hoặc branch đang lệch khỏi baseline menu chuẩn trên origin/main.

Bắt buộc đọc và đối chiếu các file sau trên origin/main trước khi sửa:
- doc/troubleshooting/navmenu-dxtreeview-playbook.md
- doc/rules/menu-structure-rules.md
- doc/project/source-map.md
- doc/project/menu-sync-20260701.md
- doc/project/branch-merge-notes-20260701.md
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenu.razor
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenu.razor.css
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenuTreeNode.razor
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/MainLayout.razor

Yêu cầu:
1. Xác định branch đang lệch ở đâu:
   - lệch cây menu
   - lệch icon
   - lệch folder/namespace
   - lệch render state trong DxTreeView
   - current module đổi sai
2. Ưu tiên merge origin/main. Nếu chưa merge full được, vẫn phải sync thủ công các file menu và tài liệu trên.
3. Không đoán icon/route/node. Mọi thứ phải khớp VntaNavMenuCatalog.cs trên main.
4. Nếu NavMenu đang render loading/empty state bằng node giả trong DxTreeView, sửa theo hướng tách loading/empty ra ngoài tree.
5. Đảm bảo node tree dùng key ổn định:
   - DxTreeView key nếu cần rebuild tree
   - NavMenuTreeNode key theo Node.Key
   - node con key theo child.Key
6. Current path phải là state rõ ràng, được cập nhật khi location change và được dùng để resolve current module/expanded node.
7. Nếu branch đang sửa source ở root legacy như Contacts, Planning, Analytics, Attendance, Implementation, đánh giá move về root mới đúng theo menu baseline.

Lệnh grep gợi ý:
rg -n "DxTreeView|DxTreeViewNode|Loading menu|No menu items|CurrentModuleChanged|LocationChanged|NavigationManager|IconCssClass|Node\\.Key|@key|BuildVisibleNodes|ResolveCurrentModule|IsExpanded" src/Vnta.HRM2026/Vnta.Hrm.Web.Client

Acceptance criteria:
- NavMenu khớp source of truth trên main
- icon khớp 100% với VntaNavMenuCatalog.cs
- loading/empty state không còn là node giả trong tree
- node key ổn định, current module đổi đúng
- không còn phát triển trên root legacy nếu phạm vi branch đang đồng bộ menu

Verification:
- build hoặc compile phần source bị ảnh hưởng
- smoke test các route mà branch đang chạm vào
- nếu branch có thay đổi role-based menu, test với user/có role tương ứng

Sau khi xong, báo cáo:
- file đã đối chiếu và file đã sửa
- branch lệch ở đâu so với main
- icon nào đã được đồng bộ
- có move source/namespace hay không
- kết quả build/compile
- risk còn lại nếu có
```

## Cách dùng prompt này

- Dán nguyên prompt vào AI agent đang làm trên branch đó.
- Nếu đã biết rõ route/màn đang tác động, thêm ở đầu prompt:
  - `Branch đang gặp lỗi ở NavMenu khi vào /some-route`
  - `Role/người dùng gặp lỗi: ...`
- Nếu branch có thay đổi đang dở trong NavMenu, yêu cầu agent giữ logic branch khi không xung đột với baseline `main`.


