# Playbook fix lỗi NavMenu + DxTreeView cho AI Agent

Tài liệu này dùng cho các AI agent cần sửa nhóm lỗi liên quan đến:

- `NavMenu.razor`
- `NavMenuTreeNode.razor`
- `VntaNavMenuCatalog.cs`
- `DxTreeView` trong shell chính của HRM

Playbook này đặc biệt hữu ích khi branch khác đã lệch khỏi baseline `main` và NavMenu bắt đầu có một hoặc nhiều triệu chứng:

- menu mở lên không ổn định
- node expand/collapse bị giật
- current module đổi không đúng
- icon node sai hoặc thiếu
- loading state và empty state chèn thẳng vào cây menu
- menu thay đổi theo role nhưng render bất ổn
- branch đã merge `main` một phần nên NavMenu bị "nửa cũ nửa mới"

## 1. Khi nào dùng playbook này

Áp dụng ngay nếu branch đang gặp một trong các dấu hiệu:

- `NavMenu` render khác `main`
- branch thiếu một số Bootstrap icon hoặc icon không đúng theo `VntaNavMenuCatalog.cs`
- node menu hiện đúng nhưng không expand theo route hiện tại
- loading/empty state được render như một node giả trong `DxTreeView`
- current module trên `MainLayout` đổi không khớp route
- branch còn dùng root cũ như `Components/Contacts`, `Planning`, `Analytics`, `Attendance`, `Implementation`

## 2. Source of truth bắt buộc phải đọc trên `origin/main`

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenu.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenu.razor.css`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenuTreeNode.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/MainLayout.razor`
- `doc/rules/menu-structure-rules.md`
- `doc/project/source-map.md`
- `doc/project/menu-sync-20260701.md`
- `doc/project/branch-merge-notes-20260701.md`
- tài liệu này

Không được đoán NavMenu "nên như thế nào" bằng trí nhớ.

## 3. Nhóm lỗi thường gặp

### 3.1. Lệch source of truth về menu và icon

- branch giữ cây menu cũ
- branch map sai icon
- branch thêm node mới nhưng không cập nhật `VntaNavMenuCatalog.cs`

### 3.2. Lệch source of truth về folder/namespace

- source đã move theo menu mới trên `main` nhưng branch vẫn sửa root cũ
- component owner đúng route nhưng namespace/file name vẫn là legacy

### 3.3. Render tree không ổn định trong `DxTreeView`

Cảnh giác cao nếu branch:

- render loading/empty state thành `DxTreeViewNode` giả
- thay đổi shape của cây node trong cùng một `DxTreeView` mà không có `@key`
- đưa current path vào recurse nhưng không có state ổn định để trigger re-render
- bind node tree theo object identity mơ hồ

### 3.4. Role-based menu cập nhật nhưng current module không theo kịp

- location đổi nhưng `CurrentModuleChanged` không cập nhật đúng lúc
- route đã đổi nhưng current path resolve vẫn dùng giá trị cũ

## 4. Audit checklist cho AI agent

### 4.1. Audit menu structure

- số node root có khớp `VntaNavMenuCatalog.All` trên `main` không
- text, route, route alias, `Key`, `IconCssClass` có khớp không
- branch có node roadmap/placeholder nào tự ý chèn vào không

### 4.2. Audit component owner

- `NavMenu.razor` có phân biệt rõ 3 state:
  - loading
  - empty
  - menu thật
- `NavMenuTreeNode.razor` có đệ quy đúng theo `Node.Children`
- `MainLayout.razor` có nhận `CurrentModuleChanged` từ NavMenu

### 4.3. Audit render stability

Cảnh giác cao nếu thấy:

- `DxTreeViewNode` dùng để hiện "Loading..." hoặc "No menu items..."
- đệ quy node mà không có `@key`
- thay đổi current path bằng cách đọc trực tiếp mỗi chỗ mà không cache state
- thay đổi toàn bộ tree state nhưng không có render key cho cây mới

## 5. Pattern fix được ưu tiên

### 5.1. Loading và empty state nằm ngoài `DxTreeView`

Không render loading/empty bằng node giả trong cùng một tree.

Nên tách rõ:

- đang loading -> block/status riêng
- rỗng menu -> block/status riêng
- có menu thật -> render `DxTreeView`

### 5.2. Dùng key ổn định cho tree và node

- `DxTreeView` nên có `@key` khi tree cần rebuild theo một baseline state mới
- mỗi `NavMenuTreeNode` nên có `@key` theo `Node.Key`
- node con cũng phải key theo `child.Key`

### 5.3. Current path cần là state rõ ràng

Không nên mỗi chỗ cần route lại tự đọc từ `NavigationManager.Uri` theo kiểu ngầm.

Nên:

- resolve current path thành state riêng
- cập nhật state đó khi location change
- dùng state đó để:
  - expand node
  - resolve current module
  - rebuild tree khi cần

### 5.4. Menu render key phải đổi khi baseline tree đổi

Nếu visible nodes thay đổi theo:

- role
- current path
- source of truth mới

có thể cần một `MenuRenderKey`/tree key để ép `DxTreeView` nhận đây là cây mới, tránh giữ state cũ sai cách.

### 5.5. Icon và node key phải đến từ catalog

Agent không được sửa icon trực tiếp rải rác trong NavMenu component nếu source of truth đang nằm ở `VntaNavMenuCatalog.cs`.

## 6. Lệnh grep gợi ý

```powershell
rg -n "DxTreeView|DxTreeViewNode|Loading menu|No menu items|CurrentModuleChanged|LocationChanged|NavigationManager|IconCssClass|Node\.Key|@key|BuildVisibleNodes|ResolveCurrentModule|IsExpanded" src/Vnta.HRM2026/Vnta.Hrm.Web.Client
```

Để tìm root legacy và menu path lệch:

```powershell
rg -n "Components/(Contacts|Planning|Analytics|Attendance|Implementation)" src đọc
```

## 7. Acceptance criteria bắt buộc

Không đóng task nếu chưa đạt đủ:

- NavMenu khớp source of truth trên `main`
- icon khớp 100% với `VntaNavMenuCatalog.cs`
- loading/empty state không còn render bằng node giả trong tree
- node key ổn định, đệ quy key ổn định
- current module đổi đúng theo route
- branch không còn phát triển trên root legacy nếu phạm vì đang đồng bộ menu

## 8. Cách báo cáo kết quả của AI agent

AI agent phải nói rõ:

- file menu/đọc nào đã đối chiếu với `origin/main`
- branch đang lệch ở đâu:
  - catalog
  - icon
  - layout
  - folder/namespace
  - render state
- đã sửa những gì
- có cần move source hay không
- đã build/compile gì
- còn risk nào chưa xử lý

## 9. Không được làm

- không tự phát thêm icon/route/node ngoài `VntaNavMenuCatalog.cs`
- không copy một phần NavMenu từ `main` rồi bỏ qua phần icon và namespace
- không sửa tree node bằng cách chèn loading node giả vào cùng cây
- không sửa branch khác theo trí nhớ khi chưa đọc `origin/main`



