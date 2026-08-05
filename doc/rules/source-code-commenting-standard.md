# Chuẩn comment source code

Tài liệu này quy định cách viết và review comment trong source HRM. Comment là một phần của codebase cần bảo trì: chỉ giữ lại khi giúp người đọc hiểu quyết định, boundary hoặc invariant mà tên code chưa thể hiện đủ.

## 1. Mục tiêu

Comment tốt trả lời một trong các câu hỏi sau:

- Tại sao code phải đi theo nhánh này thay vì cách đơn giản hơn?
- Điều kiện nào phải luôn đúng để luồng vẫn an toàn?
- Boundary nào đang được bảo vệ giữa UI, Application, Infrastructure hoặc external system?
- Điều gì sẽ sai nếu thứ tự, giới hạn hoặc cơ chế đồng bộ này bị thay đổi?

Comment không dùng để dịch từng dòng C#, Razor hoặc CSS sang tiếng Việt.

## 2. Ngôn ngữ và hình thức

- Mọi comment mới viết bằng tiếng Việt có dấu, câu ngắn và chính xác.
- C# dùng comment dòng cho nhóm nội bộ và XML documentation cho public contract khi ý nghĩa không tự hiển nhiên.
- Razor dùng Razor comment; CSS dùng block comment để đặt tên nhóm style.
- Comment mô tả hành vi hiện tại, không ghi lịch sử sửa lỗi, tên người sửa hoặc ngày sửa. Lịch sử thuộc Git và implementation log.
- Khi thuật ngữ kỹ thuật cần giữ nguyên, phần giải thích chính vẫn viết bằng tiếng Việt.

## 3. Khi nào phải có comment

Phải thêm comment tại các điểm có ít nhất một đặc tính sau:

| Tình huống | Comment cần nói rõ |
| --- | --- |
| Async, cancellation, single-flight, retry hoặc request version | Cơ chế đang ngăn race condition nào và khi nào kết quả bị bỏ |
| View-state có hai phiên bản | Khác biệt trách nhiệm, ví dụ filter đang nhập và filter đã áp dụng |
| Query nhiều bước hoặc có giới hạn | Lý do chia query, thứ tự paging ổn định, default/maximum và chi phí cần tránh |
| Mapping qua layer | Contract nguồn/đích và lý do chuẩn hóa hoặc chọn bản ghi ưu tiên |
| Business rule, lock, authorization, transaction hoặc concurrency | Quy tắc nào được bảo vệ ở server và vì sao UI không được là nguồn xác nhận cuối |
| Boundary DI, render mode hoặc HTTP | Transport runtime thực tế và abstraction mà UI phải phụ thuộc |
| CSS hoặc Razor có cấu trúc không hiển nhiên | Vai trò của nhóm layout hoặc lý do render động, không diễn giải từng thuộc tính |

## 4. Khi nào không viết comment

Không thêm comment nếu tên, kiểu và cấu trúc đã diễn đạt đủ. Đặc biệt tránh:

- comment lặp lại tên biến, tên hàm hoặc câu lệnh điều kiện;
- comment chung chung như xử lý dữ liệu, kiểm tra điều kiện hoặc gọi API;
- comment dùng để che hàm quá dài hay tên mơ hồ thay vì refactor;
- mô tả implementation có thể lỗi thời ngay khi đổi code;
- comment chỉ ghi yêu cầu UI đơn giản hoặc giá trị CSS tự hiển nhiên.

## 5. Mẫu theo layer

### UI và Razor

- Comment theo nhóm state hoặc event khi action có lifecycle không hiển nhiên.
- Ghi rõ input nào chỉ thay đổi view-state và action nào mới gọi query/command.
- Với render động, mô tả identity hoặc dữ liệu mà component cần để remount đúng.
- Không comment mỗi control DevExpress hay mỗi property binding.

### Data provider và typed client

- Ghi rõ đây là adapter map contract thành view model hoặc HTTP transport.
- Comment các quy tắc map có ưu tiên, fallback hoặc de-duplicate.
- Không đưa business rule vào comment để hợp thức hóa việc đặt rule ở client.

### Application

- Public filter, request, response và interface được comment khi mô tả phạm vi use case, paging hoặc tính bất biến không thể hiện trong tên.
- Contract không nhắc EF Core, SQL hay UI implementation.

### Infrastructure

- Comment các query có nhiều pha, join không tầm thường, stable sort, transaction, concurrency và fallback persistence.
- Ghi rõ mục tiêu hiệu năng hoặc tính đúng đắn, không mô tả lại cú pháp LINQ.

### Host, endpoint và DI

- Comment wiring chỉ khi choice transport hoặc implementation có thể gây hiểu nhầm.
- Endpoint comment về HTTP boundary; implementation nghiệp vụ vẫn nằm sau application interface.

## 6. Quy tắc thực thi và đặt `#region`

Mỗi comment mới phải vượt qua ba câu hỏi trước khi được giữ lại:

1. **Ý đồ**: nó có giải thích quyết định, bất biến hoặc boundary mà code không tự nói rõ không?
2. **Vị trí**: nó có đứng ngay trước quyết định cần bảo vệ, thay vì đứng ở đầu file hoặc cuối class không?
3. **Khả năng sống lâu**: nó có còn đúng khi đổi tên biến, tách hàm hoặc đổi cú pháp nhưng không đổi nghiệp vụ không?

Quy tắc bắt buộc:

- Không dùng `#region` để thay thế cấu trúc code rõ ràng. Chỉ dùng trong type dài có từ ba nhóm trách nhiệm độc lập trở lên; tên region phải mô tả trách nhiệm, ví dụ `Tính công và áp dụng quy tắc thâm niên`.
- Không đặt `#region` trong method, quanh một method đơn lẻ, hoặc trong adapter/interface/request ngắn. Những nơi này phải được tổ chức bằng tên type và method rõ ràng.
- XML documentation chỉ đặt trên public contract khi cần nêu phạm vi, đầu vào tùy chọn, invariant hoặc hậu quả. Không tạo XML documentation rời rạc sau dấu đóng class/type.
- Comment tại HTTP boundary chỉ nói trách nhiệm của boundary (xác thực payload, authorization, audit, response); không lặp lại thuật toán thuộc application/infrastructure.
- Comment tại UI chỉ nói lifecycle state hoặc lý do chọn `AppliedPeriod`; không mô tả lại thao tác mở popup hoặc gán biến hiển nhiên.
- Comment tại infrastructure phải nằm sát query, transaction, locking, fallback hoặc rule có thứ tự ưu tiên; không mô tả lại từng dòng LINQ/EF Core.

## 7. Cách đặt comment

- Đặt comment ngay trước nhóm code hoặc dòng có quyết định cần giải thích.
- Ưu tiên mô tả lý do và hậu quả. Ví dụ: Phân trang nhân viên trước để một nhân viên có nhiều ngày công không làm sai kích thước trang.
- Nếu cần nhiều hơn ba đến bốn dòng để giải thích một đoạn nhỏ, xem xét tách hàm, tạo type hoặc đưa nội dung dài vào tài liệu kỹ thuật.
- Khi comment nói một invariant, code gần đó phải thực sự enforce invariant ấy.
- Sửa hoặc xóa comment trong cùng thay đổi khi behavior liên quan đổi.

## 8. Checklist review

- [ ] Comment mới có trả lời lý do, boundary hoặc invariant cụ thể không?
- [ ] Tên code đã đủ rõ để có thể bỏ comment chưa?
- [ ] Comment có còn đúng với runtime, DI registration và contract hiện tại không?
- [ ] Comment có dùng tiếng Việt có dấu và không chứa thông tin nhạy cảm không?
- [ ] Async/cancellation/paging/concurrency phức tạp đã có comment đúng chỗ chưa?
- [ ] Comment public contract có tránh chi tiết UI, EF Core và SQL không?
- [ ] CSS/Razor comment có mô tả nhóm hoặc ý đồ thay vì từng thuộc tính không?

## 9. Màn tham chiếu

Bảng công tháng là màn tham chiếu cho chuẩn này. Comment của màn phải làm rõ:

- tháng/năm đang chọn khác kỳ đã áp dụng để tải dữ liệu;
- request cũ bị hủy hoặc bị bỏ khi state mới hơn xuất hiện;
- calendar dùng cache và single-flight theo năm;
- bảng công phân trang nhân viên trước, rồi mới tải day-cell của page;
- component Interactive Server gọi application contract qua DI, không mô tả sai thành HTTP.

Đọc cùng:

- doc/rules/code-rules.md
- doc/project/ui-backend-action-flow-standard.md
- doc/checklists/screen-implementation-principles.md
