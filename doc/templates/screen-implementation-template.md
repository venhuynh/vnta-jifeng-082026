# Mẫu Triển Khai Màn Hình Mới

Dùng mẫu này khi mở một màn hình mới hoặc một đợt refactor lớn cho một màn
hình nghiệp vụ trong `src/Vnta.HRM2026`.

Mẫu này bổ sung cho:

- `doc/templates/feature-spec-template.md`
- `doc/checklists/screen-implementation-principles.md`
- `doc/checklists/ui-screen-checklist.md`

Nếu feature spec mô tả "chức năng cần làm gì", thì mẫu này mô tả "màn hình sẽ
được triển khai theo cách nào".

## 1. Thông tin cơ bản

- Tên nghiệp vụ hiển thị:
- Bounded context:
- Context key (tiếng Việt không dấu):
- Technical alias nếu có:
- Tên màn hình:
- Nhóm nghiệp vụ:
- Loại màn:
  - `Master Data List Page`
  - `Operational List Page`
  - `Dashboard`
  - `Popup nghiệp vụ`
  - loại khác:
- Route:
- Menu:
- Permission:
- Sprint hoặc branch liên quan:

## 2. Mục tiêu màn hình

- Người dùng nào sẽ dùng màn này:
- Vấn đề thao tác màn này giải quyết:
- Đầu ra mong đợi sau khi người dùng thao tác:

## 3. Boundary dữ liệu bắt buộc

- Folder map theo layer:
  - UI:
  - Web hoặc endpoint:
  - Application:
  - Infrastructure:

- Mapping tên:
  - Tên nghiệp vụ hiển thị:
  - Context key:
  - Technical alias:

- UI component:
  - đường dẫn source:
  - file chính:
- Data provider hoặc typed client:
  - tên lớp:
  - đường dẫn source:
- Endpoint hoặc API boundary:
  - route:
  - request:
  - response:
- Service phía server:
  - tên abstraction:
  - tên implementation:
- Persistence:
  - bảng runtime:
  - bảng tổng hợp hoặc bảng liên quan:

Mô tả luồng:

```text
UI -> provider/typed client -> endpoint -> service -> database
```

Ghi rõ:

- UI giữ view-state nào:
- Server giữ business rule nào:
- Source of truth cuối cùng nằm ở đâu:

## 4. Hợp đồng nghiệp vụ

### Search và filter

- Filter request:
- Search text:
- Paging hoặc virtual scrolling:
- Data shaping xử lý ở server như thế nào:

### CRUD

- Tạo mới:
- Cập nhật:
- Xóa:

### Command nghiệp vụ riêng

Liệt kê rõ các command không được nhét chung vào CRUD:

- `refresh`:
- `sync`:
- `sync-summary`:
- `lock/unlock`:
- `import/export`:
- command khác:

## 5. Thiết kế UI

### Layout tổng

- Page shell:
- Toolbar title:
- Primary data surface:
  - `DxGrid`
  - `DxTreeList`
  - `DxFormLayout`
  - `DxPopup`
  - khác:
- Empty state:
- Error state:
- Loading state:

### Toolbar

- Nút `Mới`:
- Nút `Điều chỉnh`:
- Nút `Xóa`:
- Nút `Làm mới`:
- Action nghiệp vụ riêng:
- `Xuất dữ liệu`:
- `Chọn cột`:
- `Tìm kiếm`:

### Grid hoặc tree

- `KeyFieldName`:
- Cột `STT`:
- Selection mode:
- Focused row:
- Column chooser:
- Search box:
- Paging hoặc virtual scrolling:
- Popup edit form hay popup độc lập:

### Popup hoặc edit form

- Tên component form:
- Validation summary:
- Nút `Lưu`:
- Nút `Hủy`:
- Field read-only:
- Field editable:

## 6. Validation và feedback

### Validation UI-level

- DataAnnotations:
- Validation field-level:
- Validation summary:
- Message tiếng Việt:

### Validation server-side

- Rule required:
- Rule duplicate:
- Rule quan hệ nhiều field:
- Rule theo kỳ, lock state hoặc period:
- Rule tồn tại employee/department/source row:

### Feedback

- Toast success:
- Toast warning:
- Toast error:
- Dialog xác nhận:
- Khi save fail, popup có đóng không:

## 7. Transaction, lock và concurrency

- Có multi-table update không:
- Có cần transaction không:
- Lock state được xử lý ở đâu:
- Có cần `RowVersion` hoặc cơ chế concurrency tương đương không:
- Nếu nhiều người cùng sửa, màn xử lý conflict như thế nào:

## 8. Database và schema

- Bảng chính:
- Khóa unique:
- Index cần có:
- Audit fields:
- Trường thời gian nghiệp vụ:
- Có migration mới không:
- Có schema guard runtime không:
  - nếu có, giải thích lý do:

## 9. Anti-pattern cần tránh cho màn này

Đánh dấu rõ các điểm để review:

- [ ] Không inject `ApplicationDbContext` vào Razor component.
- [ ] Không trả EF entity trực tiếp về UI.
- [ ] Không để `.razor.cs` tự tính kết quả nghiệp vụ cuối cùng rồi tin vào client.
- [ ] Không để UI biết quá sâu về schema bảng.
- [ ] Không trộn command nghiệp vụ vào CRUD nếu chúng có ý nghĩa riêng.
- [ ] Không chạy schema guard/database guard trong mỗi request nếu không thật sự cần.

## 10. File dự kiến tạo hoặc sửa

### UI

- `Screen.razor`:
- `Screen.razor.cs`:
- `Screen.razor.css`:
- `ScreenEditForm.razor`:
- popup khác nếu có:

### Application và contract

- request:
- response:
- filter:
- service abstraction:

### Web và endpoint

- endpoint:
- DI:

### Infrastructure

- service implementation:
- row model:
- entity configuration:
- migration:

## 11. Checklist trước khi code

- [ ] Đã đọc `doc/checklists/screen-implementation-principles.md`.
- [ ] Đã đọc `doc/checklists/ui-screen-checklist.md`.
- [ ] Đã đọc `doc/checklists/ui-state-checklist.md`.
- [ ] Đã đọc `doc/rules/source-boundary-rules.md`.
- [ ] Đã đọc `doc/rules/blazor-devexpress-rules.md`.
- [ ] Đã đọc `doc/rules/grid-rules.md` nếu có grid hoặc tree.
- [ ] Đã đọc `doc/rules/edit-form-validation-rules.md` nếu có popup form.
- [ ] Đã đọc `doc/rules/database-rules.md` nếu có persistence hoặc migration.
- [ ] Đã đối chiếu DevExpress docs qua MCP `dxdocs26_1` hoặc docs chính thức tương ứng khi dùng API mới.

## 12. Checklist verification sau khi làm

- [ ] UI -> provider -> endpoint -> service -> database thông nhau.
- [ ] Search, filter, paging và command nghiệp vụ đi đúng boundary.
- [ ] Validation hiện trong form, không chỉ ở toast.
- [ ] Loading, empty, error, success được review.
- [ ] Concurrency hoặc lock state được xác định rõ nếu cần.
- [ ] Đã cập nhật `doc/implementation-log/yyyyMMdd-<ten-nhanh-da-chuan-hoa>.md`.
- [ ] Đã cập nhật sprint doc nếu màn này nằm trong sprint đang mở.
- [ ] Đã đối chiếu `doc/checklists/done-checklist.md` trước khi báo hoàn tất.

## 13. Ghi chú review

- Rủi ro còn mở:
- Gate chưa đóng:
- Assumption đang dùng:
- Hạng mục cần theo dõi sau merge:
