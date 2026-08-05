# Bài Học Triển Khai Và Quy Tắc Vận Hành

Tài liệu này gom các bài học thực chiến nên được giữ thành rule để tránh lặp
lại cùng một loại lỗi trong HRM Blazor.

## 1. Xác nhận đúng host đang chạy trước khi sửa shell

Trước khi sửa:

- menu
- layout
- route shell
- provider
- `Program.cs`
- DI

phải xác nhận đúng host đang thực sự chạy.

Ít nhất cần kiểm:

- startup project
- file `Program.cs` đang được dùng
- `MainLayout` và `Routes` đang được render
- feature assembly nào đang được load

## 2. Route phải có một owner duy nhất

- Mỗi route và alias chỉ được có một page owner thực sự.
- Không để demo page giữ lại `@page` trùng với page thật.
- Trước khi nối menu vào màn mới, search toàn repo route string đó.

Mục tiêu là tránh `AmbiguousMatchException` và nhầm route.

## 3. Menu chỉ được xem là wired khi đủ 4 điều kiện

Một menu item chỉ xem là nối xong khi:

1. route tồn tại thật
2. page owner tồn tại thật
3. permission đã map đúng
4. DI hoặc service cho page đó đã đăng ký ở active host

Thiếu một mắt xích thì menu chưa hoàn tất.

## 4. Checklist tích hợp màn mới

Trước khi nói một feature page đã vào app, phải kiểm:

1. route và alias đã chốt
2. host load đúng assembly chứa page
3. project reference đã đủ
4. service dependency đã được đăng ký
5. menu trỏ đúng route và permission
6. page chưa sẵn sàng thì menu phải disable hoặc ẩn, không link tới route chết

## 5. Shared provider chỉ có tác dụng khi đặt ở active host

Khi dùng shared service cho toast hoặc dialog:

- provider phải nằm ở layout của host thật
- service phải đăng ký ở `Program.cs` của host thật
- feature page phải đi qua đúng layout đó

Đừng chỉ thêm service hoặc provider ở một host song song rồi tưởng app đã dùng.

## 6. Refresh grid phải xóa selection

Đây là rule vận hành, không phải gợi ý:

- user bấm refresh
- clear selection
- clear focused row
- clear selected row id
- rồi mới reload data

Mục tiêu là tránh để action `Sửa` hoặc `Xóa` tiếp tục bật dựa trên selection cũ.

## 7. Route, permission và DI phải khớp cùng lúc

Nếu một page:

- có route nhưng không có service
- có service nhưng menu chưa có permission
- có permission nhưng route chưa được host load

thì đó vẫn là trạng thái chưa hoàn chỉnh.

Khi review màn mới, luôn kiểm route, permission và DI như một cụm.

## 8. Runtime evidence quan trọng hơn suy đoán

Khi chưa có build hoặc test tự động ngay trong luồng làm việc:

- phải ghi rõ cách kiểm chứng runtime
- phải nói rõ chưa chạy build hoặc test nếu thực sự chưa chạy
- không được ghi đã pass nếu không có bằng chứng thực thi

## 9. Dùng composition mặc định cho màn quản trị

Với các màn CRUD, catalog, directory, operational list:

- page shell full-height
- toolbar rõ ràng
- một primary data surface
- drawer hoặc popup cho inspect hoặc edit khi workflow cần
- loading bao quanh primary surface

Không bẻ layout sang các pattern ngẫu hứng nếu chưa có lý do UX rõ ràng.

## 10. Chỉ tái sử dụng pattern, không copy sample nguyên khối

Khi đọc reference hoặc sample:

- lấy pattern bố cục
- lấy ý tưởng state
- lấy guideline editor hoặc control

Không copy nguyên:

- route name

## 11. Xác nhận ranh giới host và client trước khi sửa

- Sửa `Program.cs`, auth pipeline, cookie, Identity endpoint, static asset hoặc
  render mode thì bắt đầu từ `Vnta.Hrm.Web`.
- Sửa page interactive, layout, component, client service hoặc helper UI thì bắt
  đầu từ `Vnta.Hrm.Web.Client`.
- Nếu một thay đổi đi qua cả hai phía, tài liệu triển khai phải nói rõ phần nào
  thuộc host và phần nào thuộc client.

## 12. Không coi module demo là chuẩn tổ chức dài hạn

- `Analytics`, `Contacts`, `Planning` hữu ích như reference layout và interaction.
- Chúng không phải naming chuẩn để thêm bounded context HRM mới.
- Khi lấy cảm hứng từ các module này, phải đổi sang ngữ cảnh HRM thật ở mức route,
  folder, model và tài liệu.

## 13. Sau pull kiến trúc, cập nhật rule trước khi mở rộng feature

- Nếu repo vừa đổi source root, solution, host/client split hoặc layer skeleton,
  phải rà lại `doc/rules/` và `doc/checklists/` trước.
- Không tiếp tục mở rộng feature mới trên bộ rule cũ đang mô tả sai source hiện tại.

## 14. Validation nghiệp vụ dùng chung phải có một nguồn thuật toán duy nhất

- Nếu UI, service lưu DB và gateway cùng kiểm tra một giá trị như `ActivationCode`,
  không được để mỗi nơi tự viết lại thuật toán theo trí nhớ.
- Hãy đưa helper dùng chung lên tầng có thể tham chiếu từ cả client adapter và server service,
  rồi chỉ gọi lại từ các nơi cần dùng.
- Với màn `Máy chấm công`, `Generate/Validate/Normalize` của mã kích hoạt phải luôn bám
  đúng `VntaCrypto` của gateway.

## 15. Với `timestamp without time zone`, đừng vô thức `ToLocalTime()`

- Nếu DB nghiệp vụ đang lưu `DateTime` kiểu local/unspecified, map ra model cũng nên giữ
  `DateTimeKind.Unspecified` để tránh cộng trừ múi giờ hai lần.
- Lúc format UI, phải kiểm tra `DateTime.Kind` trước khi gọi `ToLocalTime()`.
- Bài học này đặc biệt quan trọng khi HRM đọc dữ liệu thật từ PostgreSQL dùng chung với gateway.

## 16. Toolbar grid: action refresh nên đứng cạnh nhóm CRUD nếu phục vụ cùng context

- Khi `Làm mới` chỉ có tác dụng reload chính lưới hiện tại, vị trí tự nhiên nhất là gần
  `Mới`, `Điều chỉnh`, `Xóa` thay vì tách xa sang cuối toolbar.
- Nếu dùng icon-only, bắt buộc giữ `Tooltip` rõ nghĩa để không mất khả năng nhận biết thao tác.

## 17. Tài liệu rule phải được cập nhật khi vấp lỗi lặp lại

Nếu một lỗi:

- đã xảy ra thực tế
- có nguy cơ lặp lại
- không phải lỗi ngẫu nhiên một lần

thì nên chuyển nó từ ghi chú cá nhân thành rule hoặc checklist trong `doc/`.

## 18. Menu mới mặc định đi vào `Đang triển khai`

- Khi thêm menu mới mà information architecture chưa chốt dài hạn, đặt nó dưới nhóm level 0 `Đang triển khai`.
- Chỉ kéo menu đó ra nhóm top-level riêng khi đã rõ bounded context, route, permission và tài liệu đi kèm.
- Nếu menu mới chỉ là reference UI hoặc placeholder nghiệp vụ, vẫn đi theo rule này để tránh làm cây điều hướng phình sớm.

## 19. Review checklist

- Đã xác nhận đúng host đang chạy.
- Route mới không bị trùng owner.
- Menu mới đủ route + permission + DI.
- Menu mới chưa chốt IA thì đang nằm dưới `Đang triển khai`.
- Shared provider được đặt ở active host.
- Refresh grid clear selection.
- Bằng chứng kiểm chứng runtime được ghi rõ.
- Pattern mới là tái sử dụng có chọn lọc, không phải copy sample nguyên khối.

## 20. Render stability phải được coi là acceptance criteria

- Một màn DevExpress không được xem là "xong" chỉ vì chạy được ở một tab.
- Nếu màn có timer, SignalR, auto-refresh, detail load bất đồng bộ hoặc update grid thường xuyên, phải kiểm theo góc nhìn render stability:
  - không có `Unhandled exception rendering component`
  - không có callback muộn chạm vào component đã dispose
  - không có mutation tại chỗ làm grid chỉ cập nhật sau khi click header
- Smoke test nhiều tab là bước kiểm chứng bắt buộc cho nhóm màn này.
- Khi một lỗi render stability đã được khắc phục, phải đẩy bài học đó vào:
  - `doc/rules/`
  - `doc/checklists/`
  - `doc/knowledgeBase/`
  để lần sau team không quay lại pattern cũ.

## 13. Move Razor page vào folder trùng tên thì phải khóa `@namespace`

- Nếu component `GiamSatAdms` được chuyển vào folder `GiamSatAdms/`, Razor có thể suy ra namespace con trùng với tên class.
- Khi code-behind vẫn nằm ở namespace cũ, build dễ vấp `CS0101` hoặc đụng tên kiểu.
- Cách an toàn cho repo này:
  - hoặc khai báo `@namespace` rõ trong file `.razor`
  - hoặc đổi namespace của code-behind theo cấu trúc folder mới một cách nhất quán

## 14. Realtime `DxGrid` với list mutate tại chỗ có thể cần `Reload()`

- Với màn realtime, team thường `Insert(0, ...)`, `RemoveRange(...)` hoặc cập nhật item trong `List<T>` đang bind.
- Trong một số trường hợp DevExpress Grid không tự phản ánh ngay mà chỉ lộ dữ liệu sau khi user tương tác vào header hoặc sort.
- Rule vận hành:
  - nếu page dùng mutable list cho realtime grid, nên giữ `@ref` kiểu `IGrid`
  - sau khi mutate dữ liệu, gọi `Reload()` đúng thời điểm để UI cập nhật chủ động

## 15. Payload realtime phải bị cắt ở boundary publish, không chờ đến UI

- Màn HRM `/Adms` chỉ là `VIEW`, nên không có lý do để nhận nguyên payload sinh trắc học nhiều MB.
- Nếu chỉ cắt ở UI hoặc memory store, gateway vẫn đã phải đẩy payload lớn qua SignalR hoặc HTTP trước đó.
- Rule đúng là:
  - raw đầy đủ ở lại gateway text log
  - publisher realtime chỉ phát preview có hard-cap
  - HRM vẫn có thể cắt thêm một lớp phòng vệ nhưng không phải nơi cắt đầu tiên

## 16. Màn realtime view-only phải tự dọn sạch khi viewer rời đi

- Nếu không reset state khi viewer cuối cùng rời màn, HRM sẽ vô tình giữ monitor runtime như một background worker trá hình.
- Với màn ADMS kiểu view-only, phải coi việc không có viewer là tín hiệu dừng lưu bộ nhớ và bỏ qua feed đến sau đó.
- Quy tắc này áp dụng cho cả:
  - hub connection phía client
  - timer phía client
  - memory store và marker dedupe phía server

## 17. Batch command phải dựa trên tập selection hợp lệ

- Khi toolbar action áp dụng cho nhiều dòng, không dùng tổng số dòng đang chọn làm
  số lượng xử lý nếu command còn yêu cầu khóa nghiệp vụ như `SerialNumber`.
- Tạo một helper typed selection để:
  - lấy các row hợp lệ
  - bỏ row thiếu khóa bắt buộc
  - dùng cùng danh sách cho enablement, confirm message và vòng lặp tạo command
- Mỗi thiết bị phải tạo một command độc lập để lỗi hoặc trạng thái của thiết bị này
  không làm sai định danh thiết bị khác.
- Toast hoặc dialog phải phản ánh số thiết bị hợp lệ thực sự được xử lý.

## 18. Popup grid nhỏ nên ưu tiên search/filter và bỏ pager

- Popup chỉ đọc với tập key/value nhỏ không cần pager; pager làm tăng thao tác mà
  không giúp người dùng quan sát dữ liệu.
- Dùng `DxSearchBox` bind `SearchText`, `ShowFilterRow`, filter menu và
  `ShowAllRows` khi dữ liệu có giới hạn tự nhiên rõ ràng.
- Nếu datasource lookup lớn hoặc nghiệp vụ yêu cầu paging, phải bật pager/page-size selector
  và reset page index về trang đầu khi search text thay đổi.
- Khi component được tái sử dụng cho nhiều record, reset search state khi khóa record
  thay đổi để bộ lọc cũ không làm popup mới trông như không có dữ liệu.
- Phải phân biệt empty state nguồn dữ liệu với empty state do tìm/lọc không khớp.

## 19. DevExpress popup/form phải kiểm chứng thêm bằng runtime log

- Với popup/form DevExpress, build pass chưa đủ; nếu click không phản hồi, popup không mở
  hoặc breakpoint trong handler không chạm được, phải đọc `Logs/vnta-hrm/error-*.log`
  trước khi suy luận tiếp.
- Không dùng property DevExpress khi chưa xác nhận API đúng với version đang pin. Ví dụ
  `DxFormLayoutItem.CaptionVisible` có thể làm circuit lỗi runtime dù ý đồ UI rất nhỏ.
- Editor DevExpress nằm trong `EditForm` cần có `@bind-*` hoặc `*Expression` phù hợp.
  Với field chỉ hiển thị/read-only dùng giá trị một chiều, đặt `ValidationEnabled="false"`
  hoặc đưa field ra khỏi `EditForm`.
- Popup cần datasource phụ nên mở popup trước, bật loading bằng `HrmLoadingPanel`, rồi mới
  await load lookup; không để thao tác tải dữ liệu làm người dùng tưởng nút không hoạt động.
- Với popup tách file, ưu tiên pattern parent bind `@bind-Visible`, popup phát
  `VisibleChanged`, parent reset state khi `Visible` chuyển `false`, tương tự cách màn
  `MayChamCong` đang gọi popup.
- Nếu popup có DevExpress editor trong `EditForm` và parent reset model khi đóng, không capture
  trực tiếp parameter nullable trong `ValueExpression`/`ValidationMessage` như
  `() => CreateModel.SomeField`. Dùng biến local non-null trong block render
  (`CreateModel is { } createModel`) hoặc giữ model sống đến khi editor dispose xong; nếu không,
  runtime có thể lỗi `ArgumentNullException: model` trong `DxDropDownBox.GetFieldIdentifier()`
  và popup trông như không đóng được.
- `DxPopup` có `FooterContentTemplate` vẫn phải bật `ShowFooter="true"` nếu muốn vùng footer
  render ra UI. Khi thêm nút lưu/hủy trong footer, kiểm tra trực quan phần footer thay vì chỉ
  tin rằng template đã khai báo.

## 20. Batch sync theo kỳ phải khóa mốc nghiệp vụ ở backend

- Với thao tác kiểu `Lấy từ tháng trước`, không chỉ disable hoặc ghi chú ở UI rồi tin rằng user sẽ gửi đúng tháng.
- Backend phải tự kiểm tra mốc đích hợp lệ và từ chối request lệch kỳ nếu feature đang rollout từng bước.
- Nếu tháng nguồn luôn suy ra từ tháng đích, hãy để server tự tính để tránh caller truyền source/target lệch nhau.

## 21. Đồng bộ dữ liệu kỳ sau nên idempotent và trả summary định lượng

- Luồng copy từ tháng trước sang tháng hiện tại không nên mặc định xóa sạch dữ liệu đích rồi ghi lại.
- Cách an toàn hơn cho HRM là:
  - tạo mới nếu bản ghi đích chưa có
  - cập nhật nếu bản ghi đích đã có và dữ liệu khác
  - giữ nguyên nếu dữ liệu trùng
- Service nên trả về summary như `Created`, `Updated`, `Unchanged` để:
  - UI toast thông báo rõ ràng
  - reviewer có bằng chứng định lượng khi đọc PR
  - nghiệp vụ dễ đối chiếu kết quả chạy batch

## 22. Trước khi đóng nhánh feature, phải đồng bộ đủ 4 lớp tài liệu

- Khi một nhánh chuẩn bị mở PR đóng, không chỉ cập nhật code và một file log đơn lẻ.
- Tối thiểu phải rà và đồng bộ:
  - screen spec
  - sprint plan/tasks/implementation notes/review notes
  - implementation log theo ngày
  - implementation lessons nếu có bài học có khả năng lặp lại
- Mục tiêu là để reviewer đọc PR không phải suy đoán scope thực, trạng thái kiểm chứng hay bài học vận hành của feature.


