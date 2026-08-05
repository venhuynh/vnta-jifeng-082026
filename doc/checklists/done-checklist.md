# Checklist Hoàn Tất

Áp dụng trước khi AI hoặc kỹ sư kết thúc một lượt triển khai.

## Nội dung thay đổi

- [ ] Thay đổi đúng phạm vi yêu cầu.
- [ ] Không tự ý refactor ngoài phạm vi.
- [ ] Không tự ý thêm package hoặc đổi kiến trúc.
- [ ] Caption UI mới là tiếng Việt có dấu.
- [ ] Comment mới là tiếng Việt có dấu.
- [ ] Page hoặc component production đã tách hợp lý giữa `.razor`, `.razor.cs`, `.razor.css`.

## Kiến trúc và boundary

- [ ] Nếu có màn hình mới hoặc refactor màn hình, đã đối chiếu `doc/checklists/screen-implementation-principles.md`.
- [ ] Nếu là `Operational List Page`, đã đối chiếu thêm `doc/checklists/operational-list-data-processing-standard.md` và xác nhận không lệch khỏi pattern `NhanVien`.
- [ ] Nếu có màn hình mới hoặc refactor lớn, `bounded context`, `context key` bằng tiếng Việt không dấu và folder map đã khớp với `doc/project/feature-folder-standard.md`.
- [ ] UI không truy cập trực tiếp `ApplicationDbContext`, EF Core hoặc SQL.
- [ ] Business rule cốt lõi, validation cuối cùng và persistence được neo ở phía server.
- [ ] Search, filter, paging và các command nghiệp vụ đi qua boundary đúng chỗ, không trộn vào logic view.
- [ ] Nếu có multi-table update hoặc lock state hoặc nhiều người cùng sửa, đã xác định transaction hoặc concurrency hoặc cơ chế khóa phù hợp.

## Database

- [ ] Nếu có trường thời gian nghiệp vụ, dùng `timestamp without time zone`.
- [ ] Không commit mật khẩu thật trong connection string.
- [ ] Migration dùng PostgreSQL hoặc Npgsql nếu có thay đổi schema.

## Tài liệu

- [ ] Cập nhật tài liệu liên quan trong `doc/`.
- [ ] Nếu màn tạo hoặc chỉnh pattern dữ liệu kiểu `search + summary + reload + popup`, đã kiểm tra xem `doc/checklists/operational-list-data-processing-standard.md` có cần cập nhật không.
- [ ] Nếu là màn mới hoặc refactor lớn, đã tạo hoặc cập nhật `doc/templates/screen-implementation-template.md` cho màn đang làm.
- [ ] Nếu đang làm theo sprint, cập nhật folder sprint tương ứng trong `doc/sprints/<nhom>/`.
- [ ] Cập nhật đúng file ngày trong `doc/implementation-log/yyyyMMdd.md`.
- [ ] Nếu thay đổi pattern UI lặp lại, đã kiểm tra hoặc cập nhật rule hoặc checklist liên quan.
- [ ] Đường dẫn source mới trong tài liệu dùng `src/Vnta.HRM2026/...`, không vô tình ghi như thể `src/Vnta.HRM/...` vẫn là source hoạt động.

## UI và UX

- [ ] Icon UI dùng DevExpress Icon Library qua `IconUrl` hoặc `VntaDevExpressIcons`; không dùng Bootstrap Icons, CDN `bootstrap-icons`, class `bi` hoặc `bi-*`.
- [ ] Màn có loading state rõ ràng.
- [ ] Màn có empty state rõ ràng nếu là data screen.
- [ ] Màn có error state an toàn cho người dùng.
- [ ] Màn có success feedback rõ ràng sau save hoặc action.
- [ ] Nếu là màn danh sách nghiệp vụ, search, summary badge và data surface không mâu thuẫn nhau về vai trò hoặc trạng thái hiển thị.
- [ ] Toast đi qua `IHrmToastService` theo `doc/rules/shared-toast-rules.md`.
- [ ] UI không inject `IToastNotificationService`, không render `DxToastProvider` và không tự gọi `ShowToast(...)` ngoài shared layer.

## Grid và form

- [ ] Nếu dùng grid, đã chọn đúng một mode: paging, virtual scrolling hoặc bounded all rows.
- [ ] Nếu dùng `ShowAllRows`, dữ liệu có giới hạn tự nhiên nhỏ và không cấu hình pager/virtual scrolling.
- [ ] Nếu grid có selection, có `KeyFieldName` ổn định.
- [ ] Nếu có manual refresh, refresh đó clear selection trước khi reload.
- [ ] Nếu grid có search server-side riêng, search đã có debounce hoặc delay phù hợp và không bắn reload chồng nhau vô kiểm soát.
- [ ] Nếu grid có summary badge, summary badge thực sự đổi filter dữ liệu chứ không chỉ đổi trạng thái giao diện.
- [ ] Nếu có popup edit form, form có validation hiển thị trong form.
- [ ] Form DevExpress tuân thủ `doc/rules/devexpress-input-validation-rules.md`.
- [ ] Editor editable bind đúng property, có validation state và message gần field.
- [ ] Validation message dùng màu danger đủ rõ ràng, không chỉ đổi border hoặc icon editor.
- [ ] Nút hành động trong popup dùng icon quen thuộc khi ứng dụng đã có thư viện icon.
- [ ] Popup edit form có footer `Lưu`/`Hủy` tiếng Việt, dùng icon DevExpress `Save`/`Cancel`, nút `Lưu` submit form và nút `Hủy` gọi `CancelEditAsync()` hoặc callback đóng popup tương ứng.
- [ ] Editor read-only không tham gia validation đã đặt `ValidationEnabled="false"`.
- [ ] Save pipeline chặn persistence khi validation fail.

## Permission và wiring

- [ ] Menu, route, permission và DI của màn mới khớp nhau.
- [ ] Action bị giới hạn quyền đã được ẩn hoặc disable đúng chỗ ở UI.
- [ ] Không check role string rải rác trong Razor nếu đã có permission abstraction.

## Kiểm chứng

- [ ] Ghi rõ đã kiểm chứng bằng cách nào.
- [ ] Nếu chưa chạy build hoặc test, ghi rõ chưa chạy build hoặc test.
- [ ] Không nói đã pass nếu không có bằng chứng thực thi.
- [ ] Nếu chuẩn bị đóng sprint hoặc merge, tài liệu sprint hoặc review notes đã ghi rõ gate còn mở, rủi rõ còn lại và lệnh kiểm chứng cuối.


