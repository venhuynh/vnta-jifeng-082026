# Checklist Refactor Hiệu Năng Màn Hình Dữ Liệu

Áp dụng khi AI Agent Codex hoặc kỹ sư refactor màn hình dữ liệu có một hoặc
nhiều đặc điểm sau:

- dữ liệu lớn, nhiều cột hoặc ma trận theo ngày/tháng;
- paging, search hoặc filter chạy qua server;
- có nguy cơ reload chồng nhau khi đổi kỳ, search, paging hoặc refresh;
- response đang lặp dữ liệu cha trong từng row/cell;
- màn dùng `InteractiveServer` và cần quyết định rõ direct application service
  hay HTTP endpoint.

Baseline tham chiếu là refactor `Bảng công tháng` tại:

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/ChamCong/BangCongThang/`
- `doc/sprints/ChamCong/sprint-022-bang-cong-thang-performance-refactor/`

Checklist này bổ sung, không thay thế:

- `operational-list-data-processing-standard.md`
- `ui-state-checklist.md`
- `grid-rules.md`
- `source-boundary-rules.md`
- `done-checklist.md`

## 1. Chốt chiến lược dữ liệu trước khi sửa code

- [ ] Xác định dữ liệu là danh sách phẳng, tree, dashboard hay ma trận.
- [ ] Ước lượng số record, số cột động, page size và payload của kỳ dữ liệu đại diện.
- [ ] Nếu dữ liệu là ma trận `record × ngày/cột`, không tải toàn bộ kỳ về client để lọc cục bộ.
- [ ] Chọn đúng một chiến lược: server-side paging, virtual scrolling hoặc bounded all rows.
- [ ] Nếu dùng bounded all rows, ghi rõ giới hạn tự nhiên nhỏ của dataset.
- [ ] Chốt source of truth và business rule cuối cùng ở server.

## 2. Boundary và contract

- [ ] UI chỉ giữ view-state, không inject `ApplicationDbContext`, EF Core hoặc SQL.
- [ ] Luồng đã được ghi rõ: `UI -> provider -> application read service -> database` hoặc `UI -> provider -> typed client -> endpoint -> service -> database`.
- [ ] Với `InteractiveServer`, không tự thêm HTTP loopback chỉ để “đúng tầng”; ghi rõ lý do nếu provider gọi approved application read service trực tiếp.
- [ ] Nếu cần API dùng lại cho client khác, endpoint có authorization, request validation và `CancellationToken`.
- [ ] Response page trả metadata cha một lần; cell/con không lặp mã, tên, bộ phận hoặc thuộc tính cha không cần thiết.
- [ ] DTO chỉ chứa field thực sự render hoặc cần cho action của màn.
- [ ] Không trả EF entity trực tiếp về UI.
- [ ] Không giữ collection phẳng, mapping hoặc field state không có consumer sau khi đã có row/cell model.

## 3. Query backend và pagination

- [ ] Tất cả query read dùng `AsNoTracking()` khi không cần update tracking.
- [ ] Query page giới hạn `take` phía server và clamp page size theo giới hạn đã chốt.
- [ ] Sort có deterministic tie-break ổn định, thường là primary key như `EmployeeId` hoặc `Id`.
- [ ] Query dữ liệu con chỉ nhận IDs của parent trong trang hiện tại; không có N+1 query theo row/cell.
- [ ] Count, page header và page detail có thứ tự thực thi an toàn với cùng `DbContext`; không dùng `Task.WhenAll` cho EF Core operations trên cùng context.
- [ ] Search/filter/paging dùng chung ngữ nghĩa filter ở count và list query.
- [ ] Khi total count đổi làm page hiện tại vượt phạm vi, clamp page rồi tải lại theo entry point chung.

## 4. Index và đo đạc database

- [ ] Trước khi thêm hoặc đổi index, chạy `EXPLAIN ANALYZE` cho query thực tế của màn.
- [ ] Ghi query plan, row estimate/actual, thời gian và index được dùng vào sprint review notes.
- [ ] Chỉ tạo migration index khi query plan chứng minh index hiện có không đủ.
- [ ] Index mới có tên rõ nghĩa và không trùng index/composite index sẵn có.
- [ ] Không thêm schema guard hoặc DDL vào mỗi request chỉ để “tối ưu”.

## 5. Kỳ dữ liệu, filter và request snapshot

- [ ] Với màn theo kỳ, tách `RequestedPeriod` và `AppliedPeriod` hoặc khái niệm tương đương.
- [ ] Toolbar/filter đang chỉnh dở không được làm payload, loading text hoặc command dùng nhầm kỳ dataset đã tải.
- [ ] Chỉ action rõ ràng như `Xem`/`Áp dụng` mới commit `AppliedPeriod` nếu UX yêu cầu deferred load.
- [ ] Mỗi request có snapshot bất biến gồm kỳ, search/filter, page index, page size và version.
- [ ] Search debounce 300–500ms, normalize trim, chuỗi rỗng thành `null` và reset page về đầu.
- [ ] Search chỉ chạy sau khi màn có dataset/kỳ áp dụng hợp lệ nếu màn theo deferred load.

## 6. Reload, cancellation và stale result

- [ ] Mọi trigger load hội tụ về một hàm như `ReloadAsync()`.
- [ ] Màn có `CancellationTokenSource` vòng đời component và cancel/dispose đúng trong `Dispose`.
- [ ] Mỗi read request có token riêng linked với token dispose; request mới hủy request cũ.
- [ ] Có `SemaphoreSlim` hoặc cơ chế tương đương để không chạy nhiều orchestration loop song song.
- [ ] Có request version hoặc snapshot comparison để result cũ không ghi đè data, total count, loading hoặc error state mới.
- [ ] `OperationCanceledException` của request đã bị thay thế không hiện error toast cho người dùng.
- [ ] Các callback nền chỉ cập nhật state qua `InvokeAsync(...)` và có guard dispose/cancel.

## 7. Cache và single-flight

- [ ] Lookup/cache theo năm, kỳ hoặc phạm vi có key rõ ràng.
- [ ] Nếu nhiều trigger cùng cần một lookup, dùng single-flight task cache để chỉ có một request thực thi cho cùng key.
- [ ] Consumer có thể hủy việc chờ lookup mà không hủy shared lookup của consumer khác nếu phù hợp.
- [ ] Cache lỗi không bị giữ vĩnh viễn; lỗi phải cho phép retry có chủ đích.
- [ ] Khi cache trả dữ liệu cũ, chỉ commit UI nếu key vẫn khớp filter/kỳ hiện hành.

## 8. Mapping và render cost

- [ ] Mỗi DTO được map tối đa một lần trên đường tới view model.
- [ ] Không flatten row/cell rồi lại rebuild dictionary từ cùng dữ liệu nếu không có consumer riêng.
- [ ] Cell view model chỉ giữ dữ liệu hiển thị; loại bỏ property payload không dùng.
- [ ] Template grid không parse/format/lookup lặp vô ích cho mỗi render nếu có thể precompute an toàn.
- [ ] Rà `UnboundColumnData`, `CellDisplayTemplate` và grid reload để loại bỏ callback dư nhưng vẫn giữ contract DevExpress cần thiết.
- [ ] Data bind cho grid thay theo immutable collection, không mutate collection đang bind.

## 9. State, feedback và recovery

- [ ] Phân biệt ít nhất `Loading`, `Empty`, `Error`, `Success`.
- [ ] Loading text nêu đúng action: tải kỳ, đổi trang, đổi page size, retry hoặc command.
- [ ] Error state có retry qua `ReloadAsync()` với snapshot hợp lệ.
- [ ] Action và navigation bị disable theo derived state, không hard-code rải rác.
- [ ] Toast đi qua `IHrmToastService`; cancellation/stale result không tạo toast sai.
- [ ] Manual refresh hoặc command lớn clear selection/focus trước khi reload nếu màn có selection.

## 10. Kiểm chứng trước khi bàn giao

- [ ] Test hoặc smoke test đổi kỳ liên tục trong khi request cũ đang chạy.
- [ ] Test đổi page/page size liên tục và total count thay đổi ở page cuối.
- [ ] Test search debounce, clear search và retry sau lỗi.
- [ ] Xác nhận không có N+1 query bằng log/trace hoặc profiler phù hợp.
- [ ] Ghi p50/p95, số request, số SQL command và payload trước/sau khi refactor nếu mục tiêu có hiệu năng.
- [ ] Nếu chưa chạy build/test hoặc `EXPLAIN ANALYZE`, ghi rõ gate mở; không báo pass khi không có bằng chứng.
- [ ] Cập nhật sprint docs, implementation log và checklist/rule này nếu refactor tạo pattern lặp mới.

## Anti-pattern cấm áp dụng

- Tải toàn bộ dataset lớn chỉ để grid tự paging/filter ở client.
- Dùng `Task.WhenAll` cho nhiều EF Core query cùng một `DbContext`.
- Chỉ discard result cũ nhưng không hủy request cũ khi request có thể tốn tài nguyên đáng kể.
- Dùng setter fire-and-forget để khởi động I/O theo toolbar/filter.
- Thêm index hoặc migration “theo cảm giác” khi chưa xem query plan.
- Thêm HTTP loopback vào `InteractiveServer` mà không có nhu cầu boundary/reuse rõ ràng.
- Giữ DTO, mapping hoặc state thừa chỉ vì từng được dùng trong màn tiền nhiệm.
