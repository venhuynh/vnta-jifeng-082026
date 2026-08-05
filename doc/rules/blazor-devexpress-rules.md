# Quy Tắc Blazor DevExpress

Áp dụng cho mọi UI, component và page trong dự án Blazor dùng DevExpress.

## 1. Ưu tiên DevExpress Blazor

- Ưu tiên component DevExpress cho form, grid, layout, popup, tab, menu và editor.
- Không trộn thư viện UI khác nếu DevExpress đã đáp ứng được.
- BootstrapBlazor MCP chỉ dùng để tham khảo khi có yêu cầu riêng; repo hiện không provision sẵn BootstrapBlazor MCP server và đây không phải nền UI chính.

## 1.1. Icon UI bắt buộc dùng DevExpress

- Mọi icon UI phải dùng DevExpress Icon Library qua `DevExpress.Images.Blazor`.
- Component DevExpress có icon phải ưu tiên `IconUrl="@VntaDevExpressIcons...."`.
- Không dùng Bootstrap Icons, CDN `bootstrap-icons`, class `bi` hoặc `bi-*`.
- Quy tắc chi tiết: [`devexpress-icon-rules.md`](./devexpress-icon-rules.md).

## 2. Theme chuẩn toàn ứng dụng

- Theme gốc của ứng dụng là DevExpress Fluent Light hoặc Dark.
- Theme phải được đăng ký tập trung ở shell hoặc host chính.
- Stylesheet dùng chung ở cấp theme phải nằm trong `wwwroot/css/theme.css`.
- `html` của ứng dụng phải khai báo `lang="vi"` vì UI hiển thị chính bằng tiếng Việt.
- Theme switcher runtime phải dùng service chuyên trách, không reload trang chỉ để đổi sáng tối.

## 3. Quy tắc CSS toàn cục

- `wwwroot/css/theme.css` chỉ chứa design tokens HRM, override DevExpress dùng chung và utility layout thật sự toàn cục.
- `wwwroot/css/site.css` giữ reset, import và CSS nền có tính ứng dụng rộng.
- CSS riêng cho page hoặc component phải nằm trong `.razor.css` cùng component.
- Khi cần style bên trong component DevExpress từ CSS isolation, dùng `::deep` và giới hạn selector bằng wrapper class của chính page hoặc component đó.
- Không dùng `!important` trừ khi có lý do kỹ thuật thật rõ.

## 4. Design tokens HRM

- Token HRM phải có tiền tố `--hrm-*`.
- Token nên mang nghĩa semantic như `--hrm-surface`, `--hrm-border`, `--hrm-shadow-sm`.
- Nếu có thể, token HRM nên bám vào token công khai của DevExpress để đổi theme không làm lệch giao diện.

## 5. Override DevExpress

- Override grid, popup, menu, button, editor và toast nên đi qua token hoặc CSS variable công khai trước.
- Không lạm dụng selector quá sâu vào DOM nội bộ DevExpress.
- Không đặt `style="..."` inline cho màu, spacing hoặc radius của component DevExpress.

## 6. Component rõ trách nhiệm

- Mỗi component nên có một mục đích rõ ràng.
- Tách UI lặp lại thành component riêng khi có ít nhất hai nơi dùng hoặc logic đủ phức tạp.
- Không nhồi nghiệp vụ, truy vấn dữ liệu và trình bày vào cùng một file nếu có thể tách sạch.

## 6.1. File split bắt buộc cho page thực chiến

- Màn hình nghiệp vụ không nhỏ phải tách thành:
  - `*.razor`
  - `*.razor.cs`
  - `*.razor.css`
- `*.razor` chỉ nên chứa markup và binding cần thiết.
- `*.razor.cs` giữ injected service, event handler, state orchestration và helper UI.
- `*.razor.css` giữ style scoped của màn hình hoặc component đó.
- Không giữ `@code` block dài trong page production khi đã xác định đây là màn chính thức.

## 7. Binding và validation

- Dùng binding rõ ràng, tránh xử lý trạng thái vòng vo.
- Validation phải hiển thị bằng tiếng Việt.
- Lỗi nhập liệu phải thân thiện, ngắn và đúng ngữ cảnh nghiệp vụ.
- Với editor DevExpress chỉ đọc dùng one-way binding như `Text=`, phải kiểm tra yêu cầu validation của component. Nếu editor không tham gia validate model, tắt validation tường minh thay vì để popup vỡ runtime.

## 7.1. Validation và save pipeline

- Form create hoặc update phải có component edit form riêng nếu workflow không còn là demo nhỏ.
- Form phải có `ValidationSummary` hoặc cơ chế hiển thị lỗi ngay trong popup hoặc form body.
- Trước khi lưu:
  1. chuẩn hóa model
  2. đồng bộ state phụ thuộc
  3. validate rule nghiệp vụ
  4. nếu lỗi thì chặn save
  5. chỉ gọi service sau khi pass validation
- Validation input DevExpress phải tuân thủ [`devexpress-input-validation-rules.md`](./devexpress-input-validation-rules.md).
- Với editor như `DxComboBox`, `DxDateEdit`, `DxCheckBox`, ưu tiên `@bind-*`; chỉ dùng `Value`, `ValueChanged`, `ValueExpression` khi cần handler tùy chỉnh.
- Rule quan hệ như self-parent, circular hierarchy, invalid parent hoặc invalid date range phải được check trước khi persistence chạy.

## 7.2. Checklist cho DxGrid PopupEditForm

- Khi toolbar ngoài grid mở popup thêm mới hoặc chỉnh sửa, action phải gọi API của grid như `StartEditNewRowAsync()` hoặc `StartEditDataItemAsync(...)`.
- Grid dùng popup edit phải khai báo rõ `EditMode="GridEditMode.PopupEditForm"` cùng `EditFormTemplate` và các event liên quan.
- Nên giữ `DxGridCommandColumn` trong `Columns` kể cả khi ẩn toàn bộ nút command nếu luồng edit được kích từ toolbar ngoài grid.
- Popup edit form phải tự render footer action chuẩn trong `EditFormTemplate`: `DxButton Text="Lưu" IconUrl="@VntaDevExpressIcons.Save" SubmitFormOnClick="true"` và `DxButton Text="Hủy" IconUrl="@VntaDevExpressIcons.Cancel"` gọi `CancelEditAsync()` hoặc callback đóng popup tương ứng. Không để lộ nút mặc định tiếng Anh `Save`/`Cancel`.
- Nếu popup không mở, kiểm tra theo thứ tự:
  1. event toolbar có chạy không
  2. grid có vào edit mode không
  3. `EditFormTemplate` có ném exception runtime không
  4. layout cha có chặn overlay bằng `overflow` hay không

## 7.3. Render stability là tiêu chí bắt buộc

- Màn nghiệp vụ dùng DevExpress và có dữ liệu động phải mặc định đi theo `InteractiveServer`, trừ khi có screen spec hoặc kiến trúc phê duyệt khác.
- Không mutate trực tiếp collection hoặc row object đang bind cho `DxGrid`, `DxTreeList`, tab detail hoặc component con.
- Child component không được sửa trực tiếp object `[Parameter]`; phải clone hoặc dùng edit model trung gian.
- Callback từ timer, SignalR, event bus hoặc async load muộn chỉ được chạm vào UI qua `InvokeAsync(...)` và phải có guard vòng đời như `CancellationToken`, `IsDisposed` hoặc `CanMutateUi`.
- Sau create hoặc update hoặc delete, page phải chọn một cơ chế refresh chính:
  - hoặc tự thay data source
  - hoặc tự `LoadAsync()` lại
  - không chồng thêm reload không cần thiết
- `Grid.Reload()` không được dùng như phản xạ mặc định. Nếu giữ lại, phải giải thích được vì sao immutable update chưa đủ ở case đó.
- Realtime screen, auto-refresh screen và list screen có async callback phải vượt qua smoke test nhiều tab trước khi xem là ổn định.

## 8. Caption và giao diện tiếng Việt

- Caption grid, label form, button, menu, tab, dialog, toast và thông báo phải là tiếng Việt có dấu.
- Tên biến và code có thể dùng tiếng Anh theo chuẩn kỹ thuật, nhưng text hiển thị cho người dùng phải là tiếng Việt.
- Không để text mẫu như `Home`, `Counter`, `Weather`, `Submit` trong màn hình nghiệp vụ.

## 9. Notification bắt buộc cho phản hồi người dùng

- Mọi phản hồi người dùng phát sinh từ thao tác UI phải dùng notification chuẩn của HRM.
- Dùng toast cho feedback không chặn luồng.
- Dùng dialog cho xác nhận hoặc cảnh báo cần quyết định của người dùng.
- Không chỉ dùng text inline, log hoặc query string status để thay thế notification.

## 9.1. Provider và shared feedback service

- `DxToastProvider` và `DxDialogProvider` chỉ nên đặt ở layout host đang chạy.
- Với toast, đường đi chuẩn là `MainLayout.razor` -> `HrmToastProvider` -> `IHrmToastService`.
- Page nghiệp vụ, dashboard, popup nghiệp vụ và component con không tự tạo provider cục bộ cho từng màn thông thường.
- Page hoặc component UI không được inject `IToastNotificationService`, không gọi `ShowToast(...)` hoặc `CloseToast(...)`, không tự đặt `ProviderName` và không tự render `DxToastProvider`.
- Chỉ shared toast infrastructure trong `Components/Shared/Feedback/` và `Services/Ui/` được phép chạm raw DevExpress toast API.
- Khi cần custom timing, render style, theme mode hoặc action button của toast, vẫn cấu hình qua `IHrmToastService`.
- Luồng xác nhận xóa nên đi qua shared dialog service thay vì tự dựng popup xác nhận lặp lại ở từng page.
- Toast nên đi qua shared toast service để giữ semantic thống nhất: success, info, warning, error.
- Lỗi validation chặn thao tác phải hiển thị trong form hoặc gần vùng nhập liệu; toast chỉ là kênh bổ sung.
- Rule chi tiết: [`shared-toast-rules.md`](./shared-toast-rules.md).

## 9.2. Loading và trạng thái xử lý dùng chung

- Loading vùng dữ liệu trong HRM phải ưu tiên đi qua `HrmLoadingPanel` thay vì tự
  đặt `DxLoadingPanel` rải rác ở từng màn.
- Text loading mặc định phải dùng chung qua `HrmUiDefaults.LoadingText`; không
  hard-code `Loading...` hoặc tự dịch khác nhau giữa các page.
- Loading host-level hoặc account fallback cũng phải dùng cùng chuẩn caption
  tiếng Việt này để đồng bộ với shared toast/dialog.

## 10. Trải nghiệm HRM

- Màn hình nghiệp vụ phải ưu tiên nhập liệu nhanh, lọc, tìm kiếm và thao tác lặp lại.
- Danh sách nhân sự, phòng ban, chức danh, chấm công, nghỉ phép, lương thưởng cần có cách quét thông tin rõ ràng.
- Tránh giao diện kiểu landing page hoặc trang giới thiệu khi đang xây chức năng quản trị.

## 10.1. Pattern màn hình mặc định

- Màn danh mục chuẩn đi theo `toolbar + một data surface chính + popup edit form`.
- Màn vận hành chỉ thêm detail drawer hoặc detail popup khi workflow thật sự cần inspect song song với list.
- Không tự thêm status band, summary block hoặc filter band riêng ở đầu trang nếu chưa có spec nghiệp vụ yêu cầu.
- `DxGrid` ưu tiên cho dữ liệu phẳng; `DxTreeList` ưu tiên cho dữ liệu phân cấp.

## 10.2. Layout bắt buộc cho UI tiêu chuẩn

Màn UI tiêu chuẩn kiểu danh sách phải bám layout của
`src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor`.

File chính bắt buộc đi theo cấu trúc:

```text
content-root
  card toolbar
    DxToolbar Title="..." ItemRenderStyleMode="ToolbarRenderStyleMode.Plain"
  screen-root
    HasLoadError ? card error-state : HrmLoadingPanel
      DxGrid hoặc DxTreeList
        EmptyDataAreaTemplate
        EditFormTemplate -> ScreenEditForm
popup độc lập nếu có, render ngoài content-root
```

Quy tắc bắt buộc:

- File chính chỉ giữ page shell, toolbar, loading/error/empty state, grid hoặc tree list và điểm gắn component con.
- Popup edit form phải tách thành `ScreenEditForm.razor` và được gọi trong `EditFormTemplate`.
- Popup chi tiết hoặc popup nghiệp vụ độc lập phải tách thành `*Popup.razor` và render ngoài `content-root`.
- Không đặt form nhập liệu dài trực tiếp trong file chính.
- Không bọc grid hoặc tree list bằng nhiều card lồng nhau.
- Nếu dùng `DxGrid`, bắt buộc có cột `STT` theo `context.VisibleIndex + 1`; chi tiết áp dụng theo `grid-rules.md`.
- Toolbar action chuẩn theo thứ tự `Mới`, `Điều chỉnh`, `Xóa`, `Làm mới`, action nghiệp vụ riêng nếu có, `Xuất dữ liệu`, `Chọn cột`, `Tìm kiếm`.
- CSS scoped của màn phải giữ skeleton từ `MayChamCong.razor.css`: `.content-root`, `.toolbar`, `.toolbar .custom-item`, `.*-root`, `.card`, `.empty-state`, `.error-state`, `.state-title`, `.state-message`, loading panel, grid/tree, popup và search textbox.
- Class grid/loading/popup phải có tiền tố riêng theo màn; không copy nguyên class nghiệp vụ của màn khác.

Chi tiết skeleton Razor và CSS nằm tại
[`doc/project/hrm-list-screen-blueprint.md`](../project/hrm-list-screen-blueprint.md).

## Tài liệu liên quan

- [`grid-rules.md`](./grid-rules.md)
- [`devexpress-icon-rules.md`](./devexpress-icon-rules.md)
- [`edit-form-validation-rules.md`](./edit-form-validation-rules.md)
- [`implementation-lessons.md`](./implementation-lessons.md)


