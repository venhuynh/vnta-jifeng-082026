# Chuẩn xử lý hành động từ UI đến backend

Tài liệu này là chuẩn chung để thiết kế, review và refactor một hành động từ Blazor UI đến backend. Màn hình tham chiếu là Bảng công tháng:

- UI: src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/ChamCong/BangCongThang/BangCongThang.razor
- Code-behind: src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/ChamCong/BangCongThang/BangCongThang.razor.cs
- Provider: src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Services/DataProviders/MonthlyWorkSummaryDataProvider.cs
- Contract đọc: src/Vnta.HRM2026/Vnta.Hrm.Application/ChamCong/BangCongThang/IAttendanceMonthlyWorkSummaryGridReadService.cs
- Persistence: src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/ChamCong/BangCongThang/DatabaseAttendanceMonthlyWorkSummaryGridReadService.cs

Mục tiêu là để mỗi action có một đường đi rõ ràng, không đưa persistence hay business rule cuối cùng vào component, và không để kết quả request cũ ghi đè state mới hơn của người dùng.

## 1. Chuẩn bắt buộc

Mỗi action phải được mô tả bằng sáu phần, theo đúng thứ tự:

1. Ý định người dùng: ví dụ Xem, Lưu, Khóa công, đổi trang.
2. State UI sở hữu: input đang nhập, filter đã áp dụng, loading, lỗi, selection và phiên bản request.
3. Contract: filter/query hoặc request/command có tên nghiệp vụ, DTO request/response rõ ràng và CancellationToken.
4. Boundary vận chuyển: gọi trực tiếp qua DI trong Interactive Server hoặc đi qua HTTP endpoint. Chọn một đường cho từng runtime và ghi đúng đường đó.
5. Backend: validate cuối cùng, authorize, business rule, concurrency, transaction và persistence.
6. Kết quả UI: chỉ áp dụng kết quả nếu action vẫn còn hiệu lực; hiển thị loading, empty, error hoặc success nhất quán.

| Thành phần | Được sở hữu | Không được sở hữu |
| --- | --- | --- |
| Razor component | Markup, view-state, điều phối action, loading, hủy request, toast/dialog và validation nhẹ | DbContext, SQL, EF entity, transaction, business rule cuối cùng |
| Data provider / typed client | Đổi DTO backend thành view model, chuẩn hóa dữ liệu hiển thị, gọi abstraction | Quy tắc nghiệp vụ, truy vấn EF Core, authorization cuối cùng |
| Endpoint (nếu có) | HTTP binding, status code, gọi application contract | Logic tính toán nghiệp vụ hoặc persistence |
| Application contract | Use case, filter/request/DTO, abstraction hẹp theo action | Chi tiết EF Core, HTTP hoặc UI |
| Infrastructure | EF Core, truy vấn, transaction, persistence, integration kỹ thuật | View-state, caption hoặc toast UI |

## 2. Chọn đúng đường vận chuyển

Không mặc định mọi action đều phải qua HTTP. Transport phụ thuộc render mode và consumer, còn application contract là boundary nghiệp vụ ổn định.

### 2.1 Interactive Server: gọi backend qua DI

Đây là luồng runtime hiện tại của phần tải bảng công tháng:

~~~text
Người dùng
  -> BangCongThang.razor.cs
  -> MonthlyWorkSummaryDataProvider
  -> IAttendanceMonthlyWorkSummaryGridReadService
  -> DatabaseAttendanceMonthlyWorkSummaryGridReadService
  -> ApplicationDbContext / PostgreSQL
~~~

BangCongThang khai báo InteractiveServer. Program.cs đăng ký IAttendanceMonthlyWorkSummaryGridReadService với implementation database, vì vậy action Xem, phân trang và đổi page size không đi qua HTTP trong runtime này. Đây là đường hợp lệ; vẫn phải giữ provider và application interface để UI không biết EF Core hay schema.

Lịch làm việc của màn cũng được gọi qua AttendanceWorkCalendarDataProvider. Khi chạy trong host hiện tại, interface IAttendanceWorkCalendarService được Infrastructure đăng ký với DatabaseAttendanceWorkCalendarService, nên cũng được resolve qua DI server.

### 2.2 Browser/API: gọi qua endpoint

Khi component chạy browser hoặc backend cần phục vụ consumer khác, dùng đường sau:

~~~text
Người dùng
  -> component
  -> data provider
  -> Http{ContextKey}Service
  -> /api/... endpoint
  -> I{ContextKey}Service hoặc I{ContextKey}ReadService
  -> Database{ContextKey}Service
  -> database
~~~

Ví dụ adapter HttpAttendanceWorkCalendarService gọi GET /api/attendance/work-calendar?year={year}; endpoint trong AttendanceGatewayIntegrationEndpoints chỉ bind HTTP rồi gọi IAttendanceWorkCalendarService.

Không mô tả UI -> API cho action đang chạy qua DI, và không để component tự tạo HttpClient để bỏ qua provider/contract. Nếu cần cả hai transport, hai adapter cùng implement một application interface; business rule không được nhân bản ở endpoint hay UI.

## 3. Phân loại action trước khi viết code

| Loại | Ví dụ ở Bảng công tháng | Chuẩn xử lý |
| --- | --- | --- |
| Local view-state | Dựng cột ngày ban đầu | Không gọi server nếu dữ liệu hiện có đủ; render lại state rõ ràng |
| Lookup/read phụ trợ | Tải lịch làm việc theo năm | Cache theo key năm, hủy được, lỗi có degraded state an toàn |
| Query chính | Bấm Xem, đổi trang, đổi page size | Gửi filter đã áp dụng, paging/sort ở server, chống stale response |
| Command | Khóa/mở khóa/tính lại công (tương lai) | Request riêng, authorize/validate/transaction/concurrency ở server, sau thành công reload dữ liệu đã áp dụng |

Không dùng query để tạo side effect. Không nhét command dài hạn vào save CRUD chung hoặc chỉ disable nút ở UI để thay cho rule backend.

## 4. Luồng chuẩn của Bảng công tháng

### 4.1 Mở màn hình

OnInitialized chỉ gọi RebuildGridStructure để tạo cột ngày theo tháng mặc định. Không tải bảng công và không gọi database. Đây là lựa chọn đúng khi người dùng chưa xác nhận kỳ cần xem.

### 4.2 Đổi tháng hoặc năm

OnToolbarMonthChangedAsync và OnToolbarYearChangedAsync:

1. chuẩn hóa tháng/năm bằng NormalizePeriod;
2. ghi vào requestedPeriod, không ghi đè appliedPeriod;
3. tăng summaryReloadRequestedVersion, hủy request bảng công đang chạy;
4. reset danh sách, tổng số và lỗi query; dựng lại cột;
5. gọi EnsureWorkCalendarYearAsync để nạp/cached lịch theo năm;
6. chưa tải attendance_workday_summaries cho đến khi người dùng bấm Xem.

Tách requestedPeriod và appliedPeriod là bắt buộc với màn cần nút xác nhận: grid không được vô tình hiển thị dữ liệu cũ như thể thuộc kỳ mới.

### 4.3 Bấm Xem

OnViewClickAsync đặt appliedPeriod bằng requestedPeriod, reset về trang đầu rồi gọi ReloadAsync. Trong ReloadSummaryCoreAsync:

1. tạo linked cancellation token cho riêng request;
2. bảo đảm calendar của năm đã có trước khi cần nó;
3. tạo filter ngày đầu/cuối tháng và skip/take từ trang hiện tại;
4. gọi MonthlyWorkSummaryDataProvider.LoadPageAsync;
5. provider gọi IAttendanceMonthlyWorkSummaryGridReadService.SearchAsync;
6. Infrastructure chuẩn hóa khoảng ngày, giới hạn take tối đa 200, đếm và page nhân viên trước, rồi chỉ đọc workday summary của nhân viên trong page;
7. provider map DTO sang record UI; component chỉ nhận rows và total count;
8. chỉ commit GridRows và totalEmployeeCount nếu version, kỳ áp dụng và trang request vẫn khớp state hiện tại.

summaryReloadGate, version request và cancellation phải được giữ khi action có thể bị trigger liên tiếp. Không áp dụng response chỉ dựa vào thứ tự request bắt đầu; một response cũ có thể hoàn thành sau response mới.

### 4.4 Phân trang, page size và dispose

- Đổi trang chỉ nhận giá trị trong giới hạn rồi gọi lại entry point reload.
- Đổi page size giữ bản ghi đầu đang thấy nếu có thể, rồi reload khi đã có data.
- Khi tổng số giảm làm trang hiện tại không còn tồn tại, component điều chỉnh về trang cuối hợp lệ và reload lại.
- Dispose hủy request đang chạy và disposal token trước khi giải phóng resource. Không gọi StateHasChanged sau khi component đã bị dispose.

## 5. Hợp đồng query

Query phải dùng filter và DTO riêng, không gửi entity persistence lên UI.

~~~csharp
public sealed record {ContextKey}Filter(
    DateOnly FromDate,
    DateOnly ToDate,
    string? SearchText,
    int Skip,
    int Take);

public interface I{ContextKey}ReadService
{
    Task<{ContextKey}PageDto> SearchAsync(
        {ContextKey}Filter filter,
        CancellationToken cancellationToken = default);
}
~~~

Yêu cầu:

- filter phải được normalize ở backend; UI normalize chỉ để trải nghiệm tốt hơn;
- skip, take, sort và filter xử lý server-side; service đặt default và maximum;
- thứ tự phân trang phải ổn định, có tie-breaker như Id;
- response trả Rows và TotalCount, không trả entity EF;
- provider chỉ map data contract thành view record cần để render;
- query read-only dùng AsNoTracking khi phù hợp.

## 6. Hợp đồng command

Mỗi action nghiệp vụ có command riêng, ví dụ Lock, Unlock, Rebuild, Approve, Reject hoặc Import; không tái sử dụng một request CRUD mơ hồ.

~~~text
UI confirm (nếu destructive)
  -> IsBusy = true và disable action liên quan
  -> DataProvider.Execute{Action}Async(request, cancellationToken)
  -> [HTTP endpoint nếu consumer là browser]
  -> I{ContextKey}{Action}Service.ExecuteAsync(...)
  -> authorize + normalize + validate cuối + rule + transaction/concurrency
  -> response outcome
  -> toast success và ReloadAsync theo filter đã áp dụng
~~~

Backend là nguồn xác nhận cuối cùng. Bắt buộc ở backend:

- authorization theo capability/policy hoặc actor context;
- validation request và precondition (kỳ khóa, trạng thái workflow, ownership);
- transaction cho thay đổi nhiều bảng hoặc snapshot;
- optimistic concurrency bằng RowVersion, UpdatedAtUtc hay cơ chế tương đương;
- outcome rõ ràng cho success, validation, conflict, not found và lỗi hệ thống;
- audit actor/thời gian khi nghiệp vụ yêu cầu.

UI chỉ được dùng disable button, confirm dialog và validation nhẹ để giảm thao tác sai; các biện pháp này không thay thế backend guard.

## 7. State, lỗi và phản hồi người dùng

- Có một entry point reload/execute cho mỗi workflow; mọi trigger hợp lệ gọi vào entry point đó thay vì copy query logic.
- Bật loading trước await và render sớm khi cần để người dùng thấy phản hồi.
- Loading phải chặn đúng vùng/action liên quan, không khóa toàn màn hình vô cớ.
- OperationCanceledException do action mới hoặc dispose không hiện toast lỗi.
- Lỗi kỹ thuật được log/giữ chi tiết ở server; UI hiện thông báo tiếng Việt an toàn qua error state và IHrmToastService khi action thất bại.
- Empty state phải phân biệt: chưa chạy query, không có dữ liệu và query lỗi.
- Chỉ toast success sau khi backend trả outcome thành công; sau command phải reload hoặc cập nhật state từ response, không tự đoán kết quả.

## 8. DI và điểm kiểm tra khi review

Mỗi action mới phải xác định các điểm wiring sau:

1. provider được đăng ký scoped ở Web.Client/Services/ServiceExtensions.cs;
2. application interface được đăng ký với implementation Infrastructure ở composition root của Vnta.Hrm.Web hoặc Infrastructure module;
3. nếu có HTTP, typed HTTP adapter được đăng ký cho browser runtime và endpoint được map ở Program.cs;
4. route/page có authorization policy phù hợp; endpoint vẫn yêu cầu authorization độc lập nếu là API;
5. request path, verb, status code, DTO và cancellation được kiểm tra bằng test hoặc smoke test đúng transport.

Với Bảng công tháng, Program.cs đăng ký IAttendanceMonthlyWorkSummaryGridReadService với DatabaseAttendanceMonthlyWorkSummaryGridReadService. Không có monthly-summary HTTP endpoint hiện tại; không tự thêm endpoint chỉ để làm tài liệu khớp với một sơ đồ tổng quát.

## 9. Anti-pattern cấm

- Inject ApplicationDbContext hoặc chạy LINQ/SQL từ component/provider UI.
- Đặt business rule cuối, lock check hoặc permission check chỉ trong Razor.
- Gọi backend mỗi lần thay đổi input khi UX yêu cầu nút Xem xác nhận kỳ.
- Dùng requested filter để gắn nhãn cho dữ liệu đang được load bằng applied filter.
- Cho response đã bị hủy hoặc stale cập nhật grid, total, loading hoặc toast.
- Bỏ giới hạn paging, sort không ổn định hoặc nạp toàn bộ dữ liệu chỉ để grid tự phân trang.
- Để endpoint chứa truy vấn EF hoặc business workflow vốn phải nằm sau application contract.
- Nhân bản logic giữa Interactive Server adapter, HTTP endpoint và UI.

## 10. Checklist áp dụng cho action mới

- [ ] Đã phân loại local state, lookup/read, query hay command.
- [ ] Đã ghi rõ render mode và transport thực tế.
- [ ] UI chỉ giữ view-state/orchestration; không truy cập persistence.
- [ ] Có DTO/filter/request và interface hẹp theo action.
- [ ] Paging/filter/sort/limit được thực hiện và validate ở server.
- [ ] Có loading, empty, error, success và cancellation phù hợp.
- [ ] Action chồng nhau không làm stale response ghi đè state mới.
- [ ] Command có authorization, validation cuối, transaction/concurrency và audit phù hợp ở backend.
- [ ] DI, endpoint (nếu có), permission và route đã được wire đầy đủ.
- [ ] Đã kiểm chứng đường đi bằng build, test hoặc smoke test đúng runtime.

## 11. Tài liệu đọc cùng

- doc/project/architecture.md
- doc/project/feature-folder-standard.md
- doc/checklists/screen-implementation-principles.md
- doc/checklists/operational-list-data-processing-standard.md
- doc/checklists/done-checklist.md
- doc/screens/cham-cong/bang-cong-thang.md
