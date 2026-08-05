# Checklist Popup Xác Nhận Trước Khi Hành Động

Áp dụng cho mọi popup xác nhận lại trước khi thực hiện hành động trong
`src/Vnta.HRM2026`, đặc biệt là các thao tác:

- xóa dữ liệu
- đồng bộ dữ liệu
- tính lại hoặc refresh từ nguồn
- thao tác batch trên nhiều dòng
- thao tác có thể tốn thời gian hoặc gây thay đổi khó hoàn tác

Checklist này không thay thế popup edit hoặc popup detail. Nó chỉ áp dụng cho
những popup có nhiệm vụ xác nhận, chọn phạm vi hoặc xác nhận lại quyết định của
người dùng trước khi chạy command.

Mẫu tham chiếu hiện tại:

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCanRecalculatePopup.razor`
- luồng gọi từ `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCan.razor`
- orchestration trong `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan/PhuCapChuyenCan.razor.cs`

## 1. Khi nào phải dùng popup xác nhận

- [ ] Hành động có khả năng thay đổi nhiều dữ liệu hoặc chạy batch trên cả kỳ, cả danh sách, hoặc nhiều dòng đã chọn.
- [ ] Hành động có thể mất thời gian đủ lâu để người dùng cần hiểu rõ mình sắp làm gì.
- [ ] Hành động có logic bỏ qua đặc biệt như `dòng đã khóa sẽ giữ nguyên`, `chỉ áp dụng cho dòng đã chọn`, `ghi đè dữ liệu đang mở`.
- [ ] Hành động có từ hai phạm vi chạy trở lên và người dùng phải chọn trước khi hệ thống thực thi.
- [ ] Không dùng popup xác nhận cho thao tác nhỏ, đảo trạng thái tức thời hoặc click điều hướng bình thường.

## 2. Dữ liệu popup bắt buộc phải nhận từ màn cha

- [ ] Popup nhận `Visible` hoặc `VisibleChanged` từ màn cha.
- [ ] Popup nhận `IsBusy` để tự disable đóng popup và disable nút trong lúc đang chạy action.
- [ ] Popup nhận đủ context nghiệp vụ để mô tả hành động, ví dụ `SelectedRowCount`, kỳ lương, tên đối tượng, số dòng bị tác động.
- [ ] Popup nhận callback confirm rõ nghĩa như `RecalculateWholeMonth`, `RecalculateSelectedRows`, `DeleteConfirmed`.
- [ ] Popup không tự truy vấn lại dữ liệu chỉ để xác định xem có được phép xác nhận hay không nếu màn cha đã biết điều đó.

## 3. Nội dung phải giúp người dùng ra quyết định

- [ ] Header popup là động từ hoặc mục đích rõ ràng như `Chọn phạm vi tính lại`, `Xác nhận xóa dữ liệu`, `Xác nhận đồng bộ`.
- [ ] Nội dung phải trả lời được ít nhất 3 câu hỏi: `sắp làm gì`, `ảnh hưởng đến ai/cái gì`, `cái gì sẽ không bị tác động`.
- [ ] Nếu có rule bỏ qua như `dòng đã khóa được giữ nguyên`, popup phải nói rõ.
- [ ] Nếu có lựa chọn phụ thuộc context như `các dòng đã chọn`, popup phải nói rõ nó dựa trên selection hiện tại nào.
- [ ] Nếu một lựa chọn đang không hợp lệ, popup phải giải thích vì sao bị khóa thay vì chỉ disable im lặng.

## 4. Bố cục và hành động chuẩn

- [ ] Popup dùng `DxPopup`, không tự dựng modal HTML riêng.
- [ ] Body popup ưu tiên `DxFormLayout` hoặc layout DevExpress chuẩn; không cần dựng CSS cầu kỳ cho popup xác nhận đơn giản.
- [ ] Footer dùng `DxButton` tường minh cho từng quyết định.
- [ ] Nút xác nhận chính dùng `RenderStyle.Primary` và icon DevExpress phù hợp.
- [ ] Nút phụ hoặc lựa chọn phụ dùng `Secondary` hoặc `Outline` để phân cấp thị giác rõ ràng.
- [ ] Nút `Hủy` luôn có mặt và luôn dễ hiểu.
- [ ] Không dùng caption chung chung kiểu `OK`; caption phải nói đúng hành động hoặc phạm vi.

## 5. Hành vi thực thi phải an toàn

- [ ] Khi người dùng bấm `Hủy`, popup chỉ đóng và không chạy side effect.
- [ ] Khi người dùng bấm xác nhận, callback tương ứng chỉ được gọi đúng một lần.
- [ ] Trong lúc `IsBusy = true`, popup chặn `CloseOnOutsideClick`, `CloseOnEscape` và `ShowCloseButton` nếu việc đóng ngang có thể làm lệch trải nghiệm.
- [ ] Những nút không hợp lệ như `Các dòng đã chọn` khi `SelectedRowCount = 0` phải bị disable.
- [ ] Popup xác nhận không tự viết orchestration nghiệp vụ lớn trong `@code`; nó chỉ phát signal về màn cha.

## 6. Shared toast, loading và feedback

- [ ] Popup xác nhận không tự render `DxToastProvider` hoặc inject toast provider riêng.
- [ ] Feedback thành công hoặc thất bại phải do flow của màn cha hoặc shared service phát ra.
- [ ] Nếu action dài, màn cha phải có loading state rõ ràng như loading panel hoặc loading text.
- [ ] Nếu action dài mà người dùng vẫn đang nhìn popup, nút trong popup phải phản ánh trạng thái bận bằng disabled state hoặc wait indicator.
- [ ] Sau khi thành công, popup đóng theo luồng tường minh; không để popup treo ở trạng thái đã hoàn tất nhưng chưa phản hồi.

## 7. Tính nhất quán ngôn ngữ và rule nghiệp vụ

- [ ] Toàn bộ caption, mô tả, cảnh báo là tiếng Việt có dấu và đúng thuật ngữ nghiệp vụ của màn.
- [ ] Popup phải phản ánh rule thật đang chạy trong source code hiện tại, không sao chép rule cũ từ tài liệu chưa được verify.
- [ ] Nếu hành động có nhiều nhánh như `toàn bộ tháng` và `các dòng đã chọn`, tên hàm callback và tên nút phải khớp với hành vi thật.
- [ ] Không được hard-code giải thích mâu thuẫn với backend hiện tại.

## 8. Kiểm thử bắt buộc

- [ ] Mở popup xác nhận từ action chính đúng ngữ cảnh.
- [ ] `Hủy` đóng popup và không thay đổi dữ liệu.
- [ ] Xác nhận phạm vi thứ nhất chạy đúng command thứ nhất.
- [ ] Xác nhận phạm vi thứ hai chạy đúng command thứ hai.
- [ ] Trường hợp không có dòng được chọn, lựa chọn phụ bị khóa đúng.
- [ ] Trong lúc action đang chạy, popup không cho double-submit.
- [ ] Sau khi action hoàn tất, toast và dữ liệu trên màn cha phản ánh đúng kết quả.

## Tài liệu nên đọc kèm

- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/ui-popup-checklist.md`
- `doc/checklists/ui-state-checklist.md`
- `doc/rules/blazor-devexpress-rules.md`
- `doc/rules/devexpress-icon-rules.md`
