# Checklist Popup Từ Màn Hình UI Chính

Áp dụng cho mọi popup được mở ra từ một màn hình UI chính trong
`src/Vnta.HRM2026`, đặc biệt là các màn DevExpress Blazor dạng danh sách nghiệp
vụ, quản trị hoặc vận hành.

Checklist này dùng để chuẩn hóa các popup kiểu:

- popup chỉnh sửa dữ liệu
- popup chi tiết
- popup xem quy tắc hoặc tài liệu tại chỗ
- popup chọn phạm vi thao tác nghiệp vụ

Checklist này bổ sung cho:

- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/ui-state-checklist.md`
- `doc/checklists/operational-list-screen-checklist.md`

Màn tham chiếu chuẩn hiện tại:

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCan.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCan.razor.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCanEditForm.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCanRulesPopup.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCanRecalculatePopup.razor`

## 1. Xác định đúng vai trò popup

- [ ] Đã xác định popup này là `edit`, `detail`, `rules/info`, `scope selection` hay loại khác; không để một popup ôm nhiều vai trò mâu thuẫn.
- [ ] Đã xác định popup chỉ là phần mở rộng của màn cha, không tự biến thành một màn hình độc lập thứ hai.
- [ ] Đã xác định rõ dữ liệu nào thuộc ownership của màn cha và dữ liệu nào chỉ là state tạm của popup.
- [ ] Nếu popup chỉ dùng để tham khảo hoặc chọn phạm vi, nó không được tự mang thêm CRUD hoặc business action ngoài mục tiêu chính.

## 2. Wiring giữa màn cha và popup

- [ ] Trạng thái hiển thị popup được giữ ở màn cha bằng cờ rõ nghĩa như `IsRulesPopupVisible`, `IsRecalculateOptionsPopupVisible`.
- [ ] Popup nhận `Visible` hoặc `@bind-Visible` và `VisibleChanged` thay vì tự giữ vòng đời đóng mở riêng khó kiểm soát.
- [ ] Màn cha truyền đầy đủ context popup cần dùng như `record`, `selected count`, `busy state`, `error message`, `callback lưu`, `callback xác nhận`.
- [ ] Popup không tự inject lại provider dữ liệu nếu hành vi đó vốn thuộc orchestration của màn cha.
- [ ] Popup không tự tạo toast provider hoặc dialog provider cục bộ.

## 3. Bố cục và thành phần chuẩn

- [ ] Popup dùng `DxPopup` làm host chuẩn, không tự dựng modal HTML riêng.
- [ ] `HeaderText` là tiếng Việt có dấu, ngắn, nói rõ hành động hoặc mục đích popup.
- [ ] Phần thân popup ưu tiên dùng `DxFormLayout`, `DxFormLayoutGroup`, `DxFormLayoutItem` để canh layout ổn định.
- [ ] Footer popup dùng `FooterContentTemplate` với `DxButton` tường minh, không phụ thuộc nút mặc định tiếng Anh của control.
- [ ] Icon phải dùng `IconUrl` từ `VntaDevExpressIcons`, không dùng icon ngoài hệ thống.
- [ ] Không tự tạo CSS mới nếu popup có thể đạt yêu cầu bằng layout và style chuẩn của DevExpress.
- [ ] Nếu cần CSS scoped, class phải mang tiền tố theo feature popup, không copy tên class từ màn khác.

## 4. Popup nhập liệu và chỉnh sửa

- [ ] Dữ liệu mở vào popup được clone sang edit model riêng trước khi người dùng nhập, không bind trực tiếp vào dòng đang hiển thị trên grid.
- [ ] Popup nhập liệu có `EditContext` hoặc `EditForm` rõ ràng, không để validation chạy ngầm khó truy vết.
- [ ] Nếu dùng `EditForm`, đã đặt `FormName` hoặc `@formname` đúng chuẩn để tránh lỗi submit mơ hồ.
- [ ] Các trường hiển thị trong popup chỉ gồm đúng các field được phép sửa trong use case hiện tại.
- [ ] Các field dẫn xuất như kết quả tính, phân loại, tiền tổng hợp hoặc dữ liệu chỉ đọc không bị cho sửa trực tiếp nếu backend mới là nơi tính cuối cùng.
- [ ] Nút `Lưu` và `Hủy` nằm ngay trong popup footer, caption tiếng Việt, icon chuẩn DevExpress.
- [ ] Khi đang lưu, field và nút liên quan bị khóa đúng mức; không cho double-submit.
- [ ] Nếu thao tác lưu kéo dài, popup có trạng thái chờ rõ ràng như `DxWaitIndicator` hoặc disabled state có thông điệp.
- [ ] Nút `Hủy` phải đóng popup hoặc thoát mode edit mà không làm rò rỉ thay đổi tạm ra grid.

## 5. Popup thông tin, quy tắc và tài liệu tại chỗ

- [ ] Popup chỉ phục vụ việc đọc và hiểu thông tin, không chèn thêm thao tác nghiệp vụ ngoài phạm vi.
- [ ] Nội dung được chia section rõ theo góc nhìn nghiệp vụ như `nguồn dữ liệu`, `runtime`, `công thức`, `lưu ý`.
- [ ] Nếu popup chỉ là tra cứu nhanh, footer chỉ giữ đúng hành động `Đóng`.
- [ ] Nội dung popup phản ánh rule đang chạy thật trên source hiện tại, không sao chép tài liệu cũ chưa đối soát code.

## 6. Popup chọn phạm vi hoặc hành động trung gian

- [ ] Nếu một action lớn có nhiều phạm vi chạy như `toàn bộ tháng` và `các dòng đã chọn`, phải tách thành popup chọn phạm vi thay vì chạy thẳng.
- [ ] Popup phải nhận đủ context để hiển thị cho người dùng biết họ sắp tác động lên cái gì, ví dụ `SelectedRowCount`, kỳ lương, lock state hoặc điều kiện bỏ qua.
- [ ] Những lựa chọn chưa hợp lệ như `các dòng đã chọn` khi chưa chọn dòng phải bị disable rõ ràng.
- [ ] Nút hành động trong popup gọi callback của màn cha đúng một lần, không tự lồng thêm flow phụ khó đoán.

## 7. State, feedback và shared service

- [ ] Toast của popup vẫn phải đi qua `IHrmToastService` dùng chung từ flow của màn cha.
- [ ] Thành công hoặc thất bại sau khi popup gọi action phải có feedback rõ ràng cho người dùng.
- [ ] Popup không tự duy trì một state loading mâu thuẫn với loading state tổng của màn cha.
- [ ] Nếu popup đóng trong lúc đang bận sẽ gây lệch state, phải chặn `CloseOnOutsideClick`, `CloseOnEscape` hoặc `ShowCloseButton` theo `IsBusy`.

## 8. Đồng bộ dữ liệu sau thao tác

- [ ] Sau khi lưu hoặc thao tác xong, UI phải phản ánh lại nguồn thật từ backend bằng cách reload hoặc cập nhật cục bộ có kiểm soát.
- [ ] Nếu chỉ thay đổi đúng một dòng, ưu tiên cập nhật đúng dòng đó và đồng bộ lại selection thay vì reload toàn bộ grid.
- [ ] Nếu thao tác làm thay đổi tập dữ liệu lớn hoặc thay đổi rule nền, phải reload lại nguồn dữ liệu thay vì vá UI hời hợt.
- [ ] Popup không tự coi dữ liệu client là nguồn sự thật cuối cùng cho nghiệp vụ.

## 9. Guardrail nghiệp vụ và quyền thao tác

- [ ] Popup phải tôn trọng `lock state`, `permission`, `busy state` và các guardrail nghiệp vụ giống màn cha.
- [ ] Nếu rule là `unlock-first`, popup phải phản ánh rõ trong validation hoặc thông điệp thay vì cho sửa lẫn lộn.
- [ ] Những hành động phá hủy hoặc batch lớn không được gắn trực tiếp vào popup edit thông thường.

## 10. Smoke test bắt buộc

- [ ] Mở popup từ màn cha đúng dữ liệu ngữ cảnh.
- [ ] Đóng popup bằng `Hủy` hoặc `Đóng` không để side effect.
- [ ] Nhập dữ liệu hợp lệ và lưu thành công.
- [ ] Nhập dữ liệu không hợp lệ thì validation hiện đúng chỗ.
- [ ] Trong lúc đang lưu hoặc đang chạy action, nút và trạng thái bận hiển thị đúng.
- [ ] Sau khi thành công, grid hoặc màn cha phản ánh đúng thay đổi.
- [ ] Không có toast cục bộ, không có popup tự treo state sau khi action hoàn tất.

## Tài liệu nên đọc kèm

- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/operational-list-screen-checklist.md`
- `doc/checklists/ui-state-checklist.md`
- `doc/checklists/confirmation-popup-checklist.md`
- `doc/rules/blazor-devexpress-rules.md`
- `doc/rules/devexpress-icon-rules.md`
