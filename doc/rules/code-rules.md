# Quy Tắc Code

## 1. Comment bằng tiếng Việt có dấu

- Chuẩn chi tiết: [source-code-commenting-standard.md](./source-code-commenting-standard.md).
- Mọi comment mới phải viết bằng tiếng Việt có dấu đầy đủ.
- Comment phải viết theo cách dễ hiểu nhất cho dev đang đọc code, ưu tiên giải thích ý đồ, lý do và tác động thay vì dịch máy hoặc dùng câu quá hàn lâm.
- Nếu buộc phải giữ thuật ngữ kỹ thuật tiếng Anh, hãy viết phần giải thích chính bằng tiếng Việt rồi mới chèn thuật ngữ cần giữ nguyên.
- Chỉ thêm comment khi đoạn code thật sự khó hiểu hoặc cần giải thích ý đồ.
- Không viết comment dài dòng, không nhắc lại điều hiển nhiên.
- Không viết comment mơ hồ kiểu `xử lý dữ liệu`, `check điều kiện`, `fix bug`; comment phải đủ cụ thể để dev khác đọc vào biết đoạn đó đang bảo vệ điều gì hoặc phục vụ bước nào.
- Ưu tiên comment cho async/cancellation, concurrency, business invariant, query nhiều pha, mapping boundary và DI/transport có thể gây hiểu nhầm.
- Không dùng comment để bù cho tên mơ hồ, method quá dài hoặc logic đặt sai layer.

## 2. C# phải nhóm source code rõ ràng

- Với file `.cs`, phải nhóm source code thành từng khối có vai trò rõ ràng thay vì đặt lẫn lộn field, property, constructor và method.
- Thứ tự khuyến nghị:
  - hằng số hoặc static readonly
  - field private và dependency
  - property
  - constructor
  - method public
  - method protected hoặc internal
  - method private hoặc helper
- Khi file dài hoặc có nhiều phần dễ nhầm, thêm comment nhóm ngắn bằng tiếng Việt để chia khối, ví dụ: `// Dependency`, `// Trạng thái`, `// Luồng public`, `// Hàm hỗ trợ`.
- Không xen kẽ helper private vào giữa luồng public nếu không có lý do thật rõ ràng; phải giữ thứ tự ổn định để dev dễ quét file.
- Với partial class hoặc code-behind C#, mỗi file vẫn phải giữ grouping nội bộ rõ ràng theo trách nhiệm của chính file đó.

## 3. Theo chuẩn Clean

- Đặt tên rõ nghĩa, đúng vai trò.
- Hàm nhỏ, một nhiệm vụ.
- Tránh lặp code.
- Giữ logic đơn giản, dễ đọc.
- Không để code thừa, code chết, hoặc biến không dùng.
- Ưu tiên cấu trúc sạch hơn là tối ưu sớm.

## 4. Caption phải là tiếng Việt

- Tất cả caption, nhãn hiển thị, title UI, placeholder, tooltip, thông báo cho người dùng phải là tiếng Việt.
- Tránh trộn tiếng Anh trong phần hiển thị nếu không thật sự cần.
- Nếu có thuật ngữ bắt buộc giữ nguyên, ưu tiên giải thích ngắn gọn bằng tiếng Việt.

## 5. Bắt buộc dùng notification cho thông báo người dùng

- Khi code tạo, sửa hoặc xử lý một hành động có kết quả cần báo cho người dùng, phải dùng notification chuẩn của dự án thay vì chỉ ghi text rời, console/log hoặc redirect status.
- Thông báo thành công, lỗi, cảnh báo, thông tin và trạng thái xử lý phải đi qua service/component notification hiện hành; với Blazor HRM dùng `IHrmToastService`, `HrmToastProvider`, `HrmLoadingPanel` và caption loading chung qua `HrmUiDefaults` khi phù hợp.
- Notification là yêu cầu mặc định cho các luồng lưu, xóa, đăng nhập, xác thực, import/export, gửi yêu cầu và thao tác nghiệp vụ có thể thành công hoặc thất bại.
- Nội dung notification phải ngắn gọn, tiếng Việt có dấu, đúng ngữ cảnh và không chứa dữ liệu nhạy cảm.
- Trong `src/Vnta.HRM2026`, page hoặc component UI không được inject `IToastNotificationService`, không được gọi `ShowToast(...)` trực tiếp và không được tự render `DxToastProvider`.
- Rule bắt buộc cho shared toast layer nằm tại [`shared-toast-rules.md`](./shared-toast-rules.md).

## 6. Bắt buộc build sau khi sửa source

- Mọi thay đổi source code phải được build ngay sau khi hoàn tất lượt chỉnh sửa.
- Nếu build phát sinh lỗi, phải tiếp tục sửa và build lại cho đến khi không còn lỗi compile trước khi báo cáo hoàn tất.
- Build phải bao phủ toàn bộ project bị ảnh hưởng; khi thay đổi đi qua nhiều layer hoặc solution, ưu tiên build solution HRM.
- Chỉ được hoãn build khi môi trường hoặc dependency ngoài scope chặn việc build; phải báo rõ nguyên nhân, lỗi còn lại và phần đã xác minh.

## 7. Đặt code đúng source và đúng layer

- Code HRM mới phải đi vào `src/Vnta.HRM2026`.
- Không tạo hoặc cập nhật code mới theo đường dẫn `src/Vnta.HRM/...` như thể đó là
  source đang hoạt động.
- Không dồn logic nghiệp vụ dài hạn vào page `.razor`, `Program.cs` hoặc helper UI
  nếu phần đó thuộc `Domain`, `Application` hoặc `Infrastructure`.
- Trước khi thêm file mới, xác định rõ nó thuộc host, client hay layer nghiệp vụ.

## 8. Không mở rộng module demo thành module HRM thật một cách cơ học

- `Analytics`, `Contacts`, `Planning` là baseline demo hiện còn trong source.
- Không copy nguyên module demo rồi đổi caption thành tính năng HRM.
- Chỉ tái sử dụng pattern layout, state, editor hoặc interaction; không tái sử
  dụng nguyên naming, route, model và semantics demo.

## 9. Commit và push sau mỗi task

- Sau khi hoàn tất và kiểm chứng một task, được phép commit và push lên nhánh hiện tại.
- Chỉ stage các thay đổi thuộc task vừa hoàn tất; không gộp thay đổi không liên quan đang có trong worktree.
- Commit message phải mô tả rõ phạm vi, hành động và đối tượng thay đổi; ưu tiên Conventional Commits khi phù hợp.
- Nếu push thất bại, phải báo rõ nguyên nhân và giữ nguyên commit cục bộ để xử lý tiếp.
