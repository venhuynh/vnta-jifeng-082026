# Kế Hoạch Tạo Tài Liệu Chuẩn Refactor Ứng Dụng HRM

Tài liệu này chốt kế hoạch tạo một bộ tài liệu chuẩn để nhóm có thể dựa vào đó refactor toàn bộ `Vnta.HRM2026` theo cùng một hướng kiến trúc, boundary và quy tắc triển khai.

## Mục tiêu

- Tạo ra một điểm tham chiếu chính thức cho mọi đợt refactor lớn của ứng dụng.
- Chốt rõ cách giao tiếp giữa UI, API, application service, persistence và database.
- Chốt rõ ranh giới trách nhiệm giữa `Vnta.Hrm.Web`, `Vnta.Hrm.Web.Client`, `Vnta.Hrm.Application` và `Vnta.Hrm.Infrastructure`.
- Giảm nguy cơ mỗi branch refactor tự đặt thêm một convention riêng.
- Biến tài liệu thành công cụ để review, onboarding và kiểm tra trước khi merge.

## Nguyên tắc lập kế hoạch

- Không tạo một "siêu tài liệu" trùng lặp với các đọc đang tồn tại.
- Dùng một tài liệu gốc làm chuẩn, sau đó dẫn chiếu sang các rule và blueprint đã có.
- Mỗi quy tắc mới phải có ví dụ trong source hiện hành hoặc có lý do refactor rõ ràng.
- Ưu tiên tài liệu có thể dùng ngay cho feature payroll đang phân tích, sau đó mở rộng ra toàn repo.

## Phạm vi của bộ tài liệu chuẩn

Bộ tài liệu đích cần bao phủ các nhóm quyết định sau:

- kiến trúc tổng thể của ứng dụng HRM
- boundary source và dependency direction
- pattern giao tiếp UI -> API -> service -> database
- quy tắc đặt feature folder, DTO, request, response, data provider và endpoint
- quy tắc validation, transaction, locking, audit và concurrency
- quy tắc migration và schema ownership
- quy tắc refactor màn hình theo từng đợt rollout
- checklist đánh giá một feature đã refactor đúng chuẩn hay chưa

## Đề xuất bộ deliverable

### 1. Tài liệu gốc bắt buộc

`doc/project/hrm-refactor-standard.md`

Vai trò:

- là "nguồn sự thật" cấp dự án cho các đợt refactor
- tổng hợp các nguyên tắc quan trọng nhất
- dẫn chiếu đến rule chi tiết thay vì viết lại tất cả

### 2. Tài liệu vệ tinh hỗ trợ rollout

`doc/checklists/refactor-feature-checklist.md`

- checklist ngắn để review một feature trước khi coi là refactor đạt chuẩn

`doc/templates/refactor-feature-template.md`

- mẫu ngắn để mở từng feature refactor mới mà không phải nghĩ lại bộ khung

`doc/project/refactor-gap-register.md`

- danh sách khoảng cách giữa source hiện tại và chuẩn đích
- dùng để ưu tiên hóa theo phase, không nhầm với sprint task chi tiết

Nếu cần giảm phạm vi đợt đầu, có thể chỉ tạo tài liệu gốc trước, rồi bổ sung ba tài liệu vệ tinh ở đợt kế tiếp.

## Các tài liệu nên tái sử dụng, không viết lại

Tài liệu gốc mới phải dẫn chiếu rõ tới:

- `doc/project/architecture.md`
- `doc/project/target-solution-structure.md`
- `doc/project/refactor-roadmap.md`
- `doc/project/hrm-list-screen-blueprint.md`
- `doc/rules/source-boundary-rules.md`
- `doc/rules/database-rules.md`
- `doc/rules/code-rules.md`
- `doc/rules/verification-rules.md`

Mục tiêu là biến tài liệu mới thành lớp điều phối, không biến nó thành một bản sao của các đọc trên.

## Cấu trúc đề xuất cho tài liệu gốc

Tài liệu `hrm-refactor-standard.md` nên có các chương sau:

1. Mục đích và phạm vi áp dụng
2. Hiện trạng source và các vấn đề cần chỉnh
3. Kiến trúc đích cho một feature HRM chuẩn
4. Quy tắc giao tiếp giữa UI và database
5. Quy tắc tổ chức feature theo layer
6. Quy tắc request/response/DTO/view model
7. Quy tắc validation, business rule và error handling
8. Quy tắc transaction, locking, audit và concurrency
9. Quy tắc schema, migration và ownership dữ liệu
10. Quy tắc test và verification trước khi merge
11. Playbook refactor từng feature
12. Danh sách anti-pattern cần cấm

## Nội dung trong chương "UI và database"

Đây là chương cần ưu tiên viết kỹ vì sẽ dẫn đường cho nhiều màn payroll và attendance:

- UI không truy cập `DbContext` trực tiếp
- UI chỉ gọi data provider hoặc typed client
- endpoint là boundary HTTP và contract
- business service là nơi chứa orchestration và rule chính
- `Infrastructure` là nơi sở hữu EF Core, SQL, migration và external integration
- không trả EF entity trực tiếp cho UI
- server là nơi kết luận cuối cùng cho validation, calculation và lock state
- search/filter/paging ưu tiên xử lý server-side
- multi-table update phải có transaction

## Lộ trình tạo tài liệu

### Phase 1. Khảo sát hiện trạng

Mục tiêu:

- tổng hợp các rule và tài liệu đã có
- xác định điểm đúng và điểm chồng chéo
- chọn 1 đến 2 feature pilot để đối chiếu

Đầu việc:

- rà soát `doc/project/`, `doc/rules/`, `doc/checklists/`
- rà soát ít nhất một feature CRUD list và một feature payroll snapshot
- ghi nhậnh các vấn đề lặp lại trong source hiện tại

Kết quả mong đợi:

- có danh sách "giữ nguyên", "gom lại", "viết mới"

### Phase 2. Chốt khung chuẩn đích

Mục tiêu:

- xác định tài liệu gốc sẽ ra quyết định về những gì
- tách những gì nên dẫn chiếu sang tài liệu khác

Đầu việc:

- chốt dependency direction
- chốt pattern giao tiếp UI -> API -> service -> database
- chốt quy tắc đặt tên contract, service, provider, row model

Kết quả mong đợi:

- có skeleton mục lục cho tài liệu gốc

### Phase 3. Viết bản nháp tài liệu gốc

Mục tiêu:

- tạo bản nháp đầu tiên đủ chi tiết để dùng cho feature pilot

Đầu việc:

- viết các chương 1 đến 5 trước
- đưa ví dụ thực tế từ `PhuCapCom` và ít nhất một màn danh mục
- liệt kê anti-pattern đang tồn tại trong repo

Kết quả mong đợi:

- có draft có thể review bằng comment

### Phase 4. Đối chiếu với feature pilot

Mục tiêu:

- kiểm tra tài liệu có dùng được trong refactor thật hay không

Đầu việc:

- đối chiếu với `PhuCapCom`
- đối chiếu với một màn `Master Data List Page`
- ghi lại điểm nào trong chuẩn còn mơ hồ hoặc khó áp dụng

Kết quả mong đợi:

- có danh sách điều chỉnh tài liệu trước khi rollout rộng

### Phase 5. Bổ sung checklist và template

Mục tiêu:

- biến tài liệu từ "đọc để biết" thành "đọc để làm"

Đầu việc:

- tạo checklist review feature refactor
- tạo template mở feature refactor mới
- bổ sung tiêu chí done cho từng đợt chuyển đổi

Kết quả mong đợi:

- mỗi sprint refactor mới có thể mở theo một khuôn ổn định

### Phase 6. Chốt bản 1.0 và rollout

Mục tiêu:

- khóa bộ khung để các branch sau cùng bám theo

Đầu việc:

- review lại với source hiện tại
- cập nhật `doc/index.md`
- nếu cần, cập nhật thêm `doc/project/overview.md`

Kết quả mong đợi:

- có tài liệu version 1.0 được dùng làm chuẩn cho refactor toàn app

## Thứ tự viết đề xuất

Để tránh viết quá dài nhưng vẫn không dùng được ngay, thứ tự nên là:

1. viết chương boundary và dependency trước
2. viết chương giao tiếp UI và database
3. viết chương feature folder và contract pattern
4. viết chương validation, transaction, audit
5. viết playbook refactor từng feature
6. viết checklist và template

## Feature pilot nên dùng để tham chiếu

Khuyến nghị dùng hai nhóm màn hình:

- `PhuCapCom`
  - có đủ luồng UI -> endpoint -> service -> EF Core -> PostgreSQL
  - phù hợp để chốt pattern payroll snapshot
- một màn CRUD list ổn định theo blueprint
  - phù hợp để chốt pattern page shell, toolbar, popup edit form và provider

## Tiêu chí hoàn thành tài liệu gốc

Tài liệu có thể được coi là sẵn sàng cho rollout khi đạt đủ các điều kiện sau:

- người mới vào repo đọc xong có thể vẽ lại luồng của một feature
- có thể trả lời rõ logic nào đặt ở UI, logic nào đặt ở server
- có thể review một PR refactor theo checklist mà không cần tự đặt thêm convention
- có ít nhất một feature pilot đối chiếu thành công với tài liệu
- tài liệu không trùng lặp lớn với các file rule đã tồn tại

## Rủi rõ nếu không lập kế hoạch theo hướng này

- tài liệu mới trùng lặp với tài liệu cũ và sớm bị lệch
- mỗi branch refactor tiếp tục tự đặt một bộ quy tắc riêng
- refactor UI nhanh nhưng boundary và persistence vẫn rời
- review khó thống nhất vì không có một điểm tham chiếu chính

## Đề xuất cách thực hiện trên nhánh hiện tại

Trong nhánh `codex/sprint-016-phu-cap-com-ui-db-analysis`, thứ tự hợp lý nhất là:

1. tạo kế hoạch này
2. viết draft `doc/project/hrm-refactor-standard.md`
3. đưa `PhuCapCom` vào làm ví dụ pilot đầu tiên
4. bổ sung checklist review feature refactor
5. sau đó mới mở đợt refactor rộng hơn

## Ghi chú quyết định

Kế hoạch này giả định rằng repo sẽ dùng mô hình:

- một tài liệu gốc để chốt tiêu chuẩn
- các rule và blueprint hiện có tiếp tục song song
- không có ý định thay thế hoàn toàn `doc/rules/` bằng một file duy nhất

Nếu sau này nhóm muốn gom toàn bộ về một file duy nhất, cần xem đó là một quyết định riêng vì nó sẽ tăng mạnh chi phí bảo trì tài liệu.



