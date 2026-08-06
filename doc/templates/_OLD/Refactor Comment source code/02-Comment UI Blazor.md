# Prompt — Comment UI Blazor

```text
BẠN LÀ SENIOR BLAZOR UI ENGINEER. Đây là tác vụ IMPLEMENT COMMENT, không thay đổi behavior.

## Đầu vào
- Feature/name: [điền]
- UI root/route: [điền]
- Comment map: [dán kết quả prompt 01 hoặc cho agent tự lập]

## Bắt buộc
1. Comment file `.razor` và `.razor.cs` theo flow route → render → event → state → provider.
2. Dùng `@* *@` cho comment source-only; chỉ dùng HTML comment khi cần xuất hiện trong DOM.
3. Giải thích `@page`, authorization, cascading parameter, `@bind`, EventCallback, lifecycle, `@key`, loading/error/empty state, cancellation và chống double-submit khi có ý nghĩa.
4. XML-doc public component/model/service; comment private logic ở mức khối hoặc dòng có side effect/nghiệp vụ, không comment điều hiển nhiên.
5. Nêu component nào sở hữu state, callback nào cập nhật state và method nào gọi provider/API.
6. Không gọi DbContext/infrastructure trực tiếp từ UI; nếu code hiện tại làm vậy thì chỉ ghi cảnh báo, không refactor.

## Kiểm tra
- `git diff` chỉ chứa comment/XML docs.
- Build client/project liên quan và báo lỗi chính xác.

## Báo cáo
- Bảng component/file:line đã comment.
- Luồng từng nút/form/event → provider/API.
- Behavior/UI/route không thay đổi.
```
