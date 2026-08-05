# Blueprint Màn Danh Sách HRM

Tài liệu này chuẩn hóa bố cục màn hình danh sách cho toàn bộ ứng dụng HRM theo
mẫu quản trị hiện đại, rõ thao tác và dễ mở rộng.

## Mục tiêu

- Dùng một bố cục thống nhất cho các màn danh sách nghiệp vụ HRM.
- Ưu tiên thao tác nhanh trên desktop nhưng vẫn có giãn tốt khi màn hình hẹp hơn.
- Chuẩn hóa cách kết hợp `DxToolbar`, `DxGrid` hoặc `DxTreeList` và popup form để khi triển khai module mới không phải thiết kế lại từ đầu.

## Phạm vi áp dụng

Áp dụng cho các màn hình kiểu danh sách như:

- Nhân sự
- Phòng ban
- Chức danh
- Vị trí
- Hợp đồng
- Nghỉ phép
- Ca làm việc
- Danh mục dùng chung có cấu trúc cây

## Phân loại pattern màn hình

### 1. Master Data List Page

Áp dụng cho:

- phòng ban
- chức danh
- vị trí
- máy chấm công
- danh mục dùng chung

Đặc điểm:

- toolbar rõ ràng
- một data surface chính
- CRUD qua popup edit form
- không thêm detail drawer nếu workflow không cần inspect song song

### 2. Operational List Page

Áp dụng cho:

- chấm công raw log
- hàng chờ phê duyệt
- danh sách nhân sự có nhu cầu xem detail bên cạnh list
- các queue vận hành

Đặc điểm:

- vẫn có toolbar và primary list
- được phép có detail drawer hoặc popup detail
- có thể có filter area rõ hơn nếu nghiệp vụ cần review song song với danh sách

### 3. Dashboard Page

Không áp dụng đầy đủ blueprint list này.

Dashboard nên tổ chức theo:

- KPI
- filter theo kỳ hoặc đơn vị
- chart
- alert list hoặc operational shortcuts

### 4. Approval Page

Approval page thường dùng:

- queue grid
- detail preview
- approve hoặc reject action
- comment popup

Không nên ép approval page thành CRUD list đơn thuần.

## Bố cục chuẩn

```text
+----------------------------------------------------------------------------------+
| Tên màn hình   [Mới] [Điều chỉnh] [Xóa] [Làm mới] [Xuất dữ liệu v] [Chọn cột]   |
+----------------------------------------------------------------------------------+
| [Badge 1] [Badge 2] [Badge 3] [Badge 4]                          [Tìm kiếm     ] |
+----------------------------------------------------------------------------------+
|                                                                                  |
|  Grid hoặc TreeList danh sách chính                                              |
|                                                                                  |
|  - Chọn dòng                                                                     |
|  - Sắp xếp / lọc / đổi cột                                                       |
|  - Dữ liệu nghiệp vụ                                                             |
|                                                                                  |
+----------------------------------------------------------------------------------+

Khi thêm hoặc sửa:

+--------------------------------------------------------------+
| Tiêu đề popup                                      [X]       |
+--------------------------------------------------------------+
| Form nhập liệu theo nhóm trường                             |
|                                                              |
| [Thông tin chính] [Thông tin tổ chức] [Trạng thái] ...      |
|                                                              |
+--------------------------------------------------------------+
|                                             [Lưu] [Hủy]      |
+--------------------------------------------------------------+
```

## Layout Razor bắt buộc theo màn `Máy chấm công`

Với màn UI tiêu chuẩn kiểu danh sách, file chính phải tuân thủ skeleton layout
đã chốt từ `MayChamCong.razor`. Không tự tạo biến thể layout mới nếu chưa có lý
do nghiệp vụ rõ ràng.

### Bộ file bắt buộc

Một màn production chuẩn phải tách tối thiểu:

- `Screen.razor`: file chính, chỉ giữ page shell, toolbar, data surface, state
  template và điểm gắn popup/component con.
- `Screen.razor.cs`: state, injected service, permission, selection helper,
  toolbar callback, load/save/export orchestration.
- `Screen.razor.css`: scoped CSS của page theo skeleton bên dưới.
- `ScreenEditForm.razor`: form dùng trong `EditFormTemplate` của
  `DxGrid` hoặc `DxTreeList` popup edit form.

Nếu màn có popup chi tiết hoặc popup nghiệp vụ độc lập, phải tách thêm:

- `ScreenDetailPopup.razor` hoặc `ScreenActionPopup.razor`
- `ScreenDetailPopup.razor.cs` và `ScreenDetailPopup.razor.css` nếu popup có
  logic hoặc style riêng

File chính không được nhồi markup dài của popup chi tiết hoặc form nhập liệu.
File chính chỉ gọi component con và truyền state/callback cần thiết.

### Skeleton file chính

Skeleton này được trích từ `MayChamCong.razor` và là layout bắt buộc cho
`Master Data List Page` hoặc list CRUD tiêu chuẩn:

```razor
@page "/route-của-man"

@using Microsoft.AspNetCore.Authorization
@using Vnta.Hrm.Web.Client.Models

@attribute [Authorize]

<PageTitle>VNTA - Tên màn hình</PageTitle>

<div class="content-root">
    <div class="card toolbar">
        <DxToolbar Title="Tên màn hình" ItemRenderStyleMode="ToolbarRenderStyleMode.Plain">
            <DxToolbarItem Text="Mới"
                           Click="OnAddClick"
                           Enabled="@CanCreate"
                           Alignment="ToolbarItemAlignment.Right"
                           IconUrl="@VntaDevExpressIcons.Add" />
            <DxToolbarItem Text="Điều chỉnh"
                           Click="OnEditClick"
                           Enabled="@CanEditSelected"
                           Alignment="ToolbarItemAlignment.Right"
                           IconUrl="@VntaDevExpressIcons.Edit" />
            <DxToolbarItem Text="Xóa"
                           Click="OnDeleteClick"
                           Enabled="@CanDeleteSelected"
                           Alignment="ToolbarItemAlignment.Right"
                           IconUrl="@VntaDevExpressIcons.Delete" />
            <DxToolbarItem Tooltip="Làm mới"
                           Enabled="@CanInteract"
                           Click="ReloadAsync"
                           Alignment="ToolbarItemAlignment.Right"
                           IconUrl="@VntaDevExpressIcons.Refresh" />

            @* Action nghiệp vụ riêng, nếu có, đặt sau Làm mới và trước Xuất dữ liệu. *@

            <DxToolbarItem Text="Xuất dữ liệu"
                           Enabled="@CanExport"
                           Alignment="ToolbarItemAlignment.Right"
                           IconUrl="@VntaDevExpressIcons.Export">
                <Items>
                    <DxToolbarItem Text="Xuất Excel" Click="ExportAllDataToExcel" Enabled="@CanExport" IconUrl="@VntaDevExpressIcons.Excel" />
                    <DxToolbarItem Text="Xuất PDF" Click="ExportAllDataToPdf" Enabled="@CanExport" IconUrl="@VntaDevExpressIcons.Pdf" />
                    <DxToolbarItem Text="Xuất dòng đã chọn ra Excel" Click="ExportSelectedRowsToExcel" Enabled="@CanExportSelected" IconUrl="@VntaDevExpressIcons.Excel" />
                    <DxToolbarItem Text="Xuất dòng đã chọn ra PDF" Click="ExportSelectedRowsToPdf" Enabled="@CanExportSelected" IconUrl="@VntaDevExpressIcons.Pdf" />
                </Items>
            </DxToolbarItem>
            <DxToolbarItem Tooltip="Chọn cột"
                           Alignment="ToolbarItemAlignment.Right"
                           Enabled="@CanInteract"
                           Click="OnColumnChooserItemClick"
                           IconUrl="@VntaDevExpressIcons.ColumnChooser" />
        </DxToolbar>
    </div>

    <div class="screen-root">
        @if(HasLoadError) {
            <div class="card error-state">
                <div class="state-title">Không thể tải danh sách ...</div>
                <div class="state-message">@LoadErrorMessage</div>
                <DxButton Text="Thử lại"
                          Click="ReloadAsync"
                          RenderStyle="ButtonRenderStyle.Primary"
                          RenderStyleMode="ButtonRenderStyleMode.Contained" />
            </div>
        } else {
            <HrmLoadingPanel Visible="@IsLoading"
                             IsContentBlocked="true"
                             IsContentVisible="true"
                             IndicatorVisible="true"
                             IndicatorAreaVisible="true"
                             CssClass="screen-loading-panel">
                <DxGrid @ref="Grid"
                        Data="@Rows"
                        KeyFieldName="Id"
                        CssClass="screen-grid"
                        SearchText="@SearchText"
                        EditMode="GridEditMode.PopupEditForm"
                        PopupEditFormCssClass="screen-popup"
                        PopupEditFormHeaderText="Thông tin ..."
                        PageSize="20"
                        PagerPosition="GridPagerPosition.TopAndBottom"
                        PageSizeSelectorVisible="true"
                        PageSizeSelectorItems="@(new int[] { 10, 20, 50 })"
                        SkeletonRowsEnabled="true"
                        ColumnResizeMode="GridColumnResizeMode.NextColumn"
                        FilterMenuButtonDisplayMode="GridFilterMenuButtonDisplayMode.Always"
                        TextWrapEnabled="false"
                        FocusedRowEnabled="true"
                        HighlightRowOnHover="true"
                        AllowSelectRowByClick="true"
                        SelectionMode="GridSelectionMode.Multiple"
                        SelectedDataItems="@SelectedDataItems"
                        SelectedDataItemsChanged="@OnSelectedDataItemsChanged"
                        CustomizeEditModel="OnCustomizeEditModel"
                        EditModelSaving="OnEditModelSaving">
                    <Columns>
                        <DxGridSelectionColumn Width="3rem" />

                        <DxGridDataColumn Caption="STT" Width="4.5rem" MinWidth="72" AllowSort="false">
                            <CellDisplayTemplate>
                                @(context.VisibleIndex + 1)
                            </CellDisplayTemplate>
                        </DxGridDataColumn>

                        @* Các cột nghiệp vụ của màn. *@

                        <DxGridCommandColumn Visible="false"
                                             NewButtonVisible="false"
                                             EditButtonVisible="false"
                                             DeleteButtonVisible="false"
                                             SaveButtonVisible="false"
                                             CancelButtonVisible="false" />
                    </Columns>
                    <EmptyDataAreaTemplate>
                        <div class="empty-state">
                            <div class="state-title">Chưa có dữ liệu</div>
                            <div class="state-message">Mô tả ngắn theo nghiệp vụ.</div>
                            <DxButton Text="Tạo mới"
                                      Click="OnAddClick"
                                      RenderStyle="ButtonRenderStyle.Primary"
                                      RenderStyleMode="ButtonRenderStyleMode.Contained" />
                        </div>
                    </EmptyDataAreaTemplate>
                    <EditFormTemplate Context="editFormContext">
                        @{
                            var editModel = (ScreenRecord)editFormContext.EditModel;
                        }
                        <ScreenEditForm Model="@editModel"
                                        EditFormContext="@editFormContext"
                                        ErrorMessage="@EditErrorMessage"
                                        IsCreatingNew="@IsCreatingNew" />
                    </EditFormTemplate>
                </DxGrid>
            </HrmLoadingPanel>
        }
    </div>
</div>

@* Popup chi tiết hoặc popup nghiệp vụ độc lập, nếu có, đặt ngoài content-root. *@
<ScreenDetailPopup @bind-Visible="IsDetailPopupVisible"
                   Model="@DetailModel"
                   RetryRequested="RetryDetailAsync" />
```

Với màn dùng `DxTreeList`, giữ nguyên shell `content-root -> card toolbar ->
screen-root -> HrmLoadingPanel`, thay `DxGrid` bằng `DxTreeList` và vẫn giữ
popup edit form/component con tương ứng.

### Biến thể `Operational List Page` theo chuẩn `NhanVien`

Với các màn vận hành có `summary badge`, `search` server-side hoặc action như
`refresh/sync`, data surface nên có header riêng nằm trong cùng card với grid:

```razor
<div class="card screen-grid-card">
    <HrmLoadingPanel Visible="@ShowLoadingPanel" ...>
        <div class="screen-grid-content">
            <div class="screen-grid-header">
                <div class="summary-strip">
                    @foreach (var badge in SummaryBadges)
                    {
                        <button type="button"
                                class="summary-badge @(badge.Key == ActiveSummaryBadgeKey ? "is-active" : null)"
                                @onclick="() => OnSummaryBadgeClick(badge.Key)">
                            <span class="summary-badge-label">@badge.Label</span>
                            <span class="summary-badge-value">@badge.Count</span>
                        </button>
                    }
                </div>
                <div class="screen-grid-search">
                    <DxSearchBox CssClass="search-textbox"
                                 NullText="Tìm kiếm"
                                 BindValueMode="BindValueMode.OnDelayedInput"
                                 InputDelay="500"
                                 Text="@SearchText"
                                 TextChanged="OnSearchTextChanged" />
                </div>
            </div>

            <DxGrid Data="@Rows"
                    ShowSearchBox="false"
                    ...>
            </DxGrid>
        </div>
    </HrmLoadingPanel>
</div>
```

Quy ước của biến thể này:

- toolbar chỉ giữ action
- `search`, `summary badge` và filter nhanh nằm sát data surface
- `DxGrid.ShowSearchBox` không dùng cho flow search server-side
- mọi trigger `search`, `summary`, `refresh`, `save`, `retry` hội tụ về
  `ReloadAsync()` hoặc entry point tương đương

### Skeleton CSS bắt buộc

CSS scoped của màn phải đi theo layout của `MayChamCong.razor.css`. Đổi
`screen` thành tiền tố riêng của màn, ví dụ `attendance-devices`,
`attendance-shifts`, `attendance-positions`.

```css
.content-root {
    height: 100%;
    display: flex;
    flex-direction: column;
    min-height: 0;
}

.toolbar {
    padding: 0.5rem;
    margin-bottom: 1rem;
}

.toolbar .custom-item {
    display: flex;
    align-items: center;
    margin: 0 0.5rem;
}

.screen-root {
    flex: 1 1 auto;
    min-height: 0;
}

.card {
    position: relative;
    display: flex;
    flex-direction: column;
    min-width: 0;
    background-color: var(--dxds-color-surface-neutral-default-rest);
    background-clip: border-box;
    border: 1px solid var(--dxds-color-border-neutral-default-rest);
    border-radius: 0.25rem;
}

.empty-state,
.error-state {
    height: 100%;
    min-height: 18rem;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    padding: 2rem;
    text-align: center;
}

.state-title {
    font-size: 1.125rem;
    font-weight: 600;
    color: var(--dxds-color-content-neutral-default-rest);
}

.state-message {
    max-width: 32rem;
    color: var(--dxds-color-content-neutral-subtle-rest);
}

::deep .screen-loading-panel {
    height: 100%;
}

::deep .screen-grid {
    max-height: 100%;
    border-radius: 0.25rem;
}

::deep .screen-popup {
    width: min(62rem, calc(100vw - 2rem));
    max-height: min(80vh, 44rem);
    overflow-y: auto;
}

::deep .search-textbox-item {
    max-width: 14rem;
}
```

Rule bắt buộc:

- Không thay `content-root`, `card toolbar`, `screen-root`, `HrmLoadingPanel`,
  `DxGrid` hoặc `DxTreeList`, `EmptyDataAreaTemplate`, `EditFormTemplate` bằng
  layout tự nghĩ nếu màn là UI tiêu chuẩn.
- Không bọc grid trong nhiều card lồng nhau.
- Không để form nhập liệu dài trong file chính; form phải nằm trong
  `ScreenEditForm.razor`.
- Popup độc lập phải là component riêng và được render sau `</div>` của
  `content-root`.
- Action nghiệp vụ riêng được phép thêm vào toolbar nhưng phải đặt sau
  `Làm mới` và trước `Xuất dữ liệu`.
- Tất cả icon toolbar dùng `VntaDevExpressIcons` qua `IconUrl`.

## Thành phần bắt buộc

### 1. Toolbar trên cùng

Toolbar là vùng điều khiển chính của màn hình.

#### Bố cục

- Bên trái: tên màn hình.
- Bên phải: toàn bộ action hiển thị trên một hàng theo thứ tự `Mới`,
  `Điều chỉnh`, `Xóa`, `Làm mới`, action nghiệp vụ riêng, `Xuất dữ liệu`,
  `Chọn cột`.
- Search box:
  - với `Master Data List Page`, có thể nằm trên toolbar
  - với `Operational List Page`, ưu tiên nằm ở header của data surface cùng
    summary badge hoặc filter nhanh

#### Thành phần chi tiết

- Tên màn hình:
  - Hiển thị rõ thực thể đang quản lý như `Nhân sự`, `Phòng ban`, `Hợp đồng lao động`.
  - Không thêm mô tả dài trong toolbar.
- Nhóm CRUD:
  - `Mới`
  - `Điều chỉnh`
  - `Xóa`
- Dropdown `Xuất dữ liệu`:
  - `Xuất Excel`
  - `Xuất PDF`
- Nút `Chọn cột`:
  - Mở column chooser cho `DxGrid` hoặc bộ chọn cột tương đương cho `DxTreeList`.
- Ô `Tìm kiếm`:
  - Tìm nhanh theo từ khóa trên toàn danh sách.
  - Dùng `DxSearchBox` hoặc control tương đương đã chuẩn hóa.
  - Nếu màn search server-side, dùng `BindValueMode.OnDelayedInput` và không
    dựa vào `DxGrid.ShowSearchBox`.

#### Quy tắc hành vi

- Nhóm action bên phải luôn nằm trên một hàng ở chế độ desktop.
- `Điều chỉnh` và `Xóa` chỉ bật khi đang có dòng được chọn hoặc được focus.
- `Mới` luôn khả dụng nếu người dùng có quyền tạo mới.
- `Tìm kiếm` phải phản hồi theo kiểu nhập tới đâu lọc tới đó khi workflow cho
  phép, nhưng với màn server-side phải có debounce hoặc delay hợp lý.
- Caption nút và menu đều là tiếng Việt có dấu.

## 2. Vùng danh sách chính

Vùng chính luôn chiếm phần lớn diện tích màn hình và chỉ có một khối dữ liệu trung tâm.

### Khi dùng `DxGrid`

Dùng cho dữ liệu phẳng, không có quan hệ cha con trực tiếp trong cùng danh sách.

Ví dụ:

- Nhân sự
- Hợp đồng
- Nghỉ phép
- Chấm công
- Quyết định

#### Thành phần tối thiểu

- Cột chọn dòng.
- Cột `STT` bắt buộc, đặt ngay sau cột chọn dòng hoặc cột command; nếu grid
  không có selection/command thì đặt `STT` làm cột đầu tiên.
- Cột định danh chính.
- Cột thông tin phụ quan trọng.
- Cột trạng thái.
- Hỗ trợ sort, filter menu, column resize, paging hoặc virtual scrolling tùy spec.

#### Cột `STT` bắt buộc

Mọi `DxGrid` production phải có cột `STT`. Cột này là cột hiển thị số thứ tự
theo thứ tự đang render của grid, không bind vào DTO và không dùng để sort.

```razor
<DxGridDataColumn Caption="STT" Width="4.5rem" MinWidth="72" AllowSort="false">
    <CellDisplayTemplate>
        @(context.VisibleIndex + 1)
    </CellDisplayTemplate>
</DxGridDataColumn>
```

Không thêm property `Stt`, `Index` hoặc `RowNumber` vào DTO chỉ để phục vụ UI.

### Khi dùng `DxTreeList`

Dùng cho dữ liệu phân cấp hoặc cần nhìn cấu trúc cây.

Ví dụ:

- Cơ cấu tổ chức
- Phòng ban nhiều cấp
- Danh mục nhóm chức năng
- Cây vị trí quản lý

#### Thành phần tối thiểu

- Cột cây làm cột chính.
- Cột mã hoặc tên ngắn.
- Cột trạng thái.
- Hỗ trợ expand/collapse, focus dòng, tìm kiếm và chọn cột.

### Quy tắc trình bày chung

- Không đặt nhiều card nhỏ cạnh nhau trong vùng danh sách chính.
- Không nhét form nhập liệu trực tiếp dưới grid.
- Không dùng inline edit làm luồng chính.
- Trạng thái nên hiển thị bằng badge hoặc text có màu semantic thống nhất.
- Dòng đang chọn phải có trạng thái highlight rõ.
- Với `Master Data List Page`, mặc định không thêm status band hoặc summary
  block riêng phía trên data surface nếu chưa có lý do nghiệp vụ rõ ràng.
- Với `Operational List Page`, được phép có header trong cùng data surface để
  chứa `summary badge`, `search` hoặc filter nhanh, theo chuẩn `NhanVien`.
- Search server-side, summary badge và filter cohort nên đặt gần primary data
  surface thay vì trộn lẫn với toolbar action.

## 3. Popup edit form khi thêm hoặc sửa

Popup là nơi nhập liệu chuẩn cho toàn bộ màn CRUD kiểu danh sách.

### Thành phần popup

- Header:
  - Tiêu đề theo ngữ cảnh như `Tạo mới nhân sự`, `Điều chỉnh phòng ban`.
  - Nút đóng ở góc phải.
- Body:
  - `DxFormLayout` làm khung chính.
  - Chia nhóm trường rõ ràng theo nghiệp vụ.
  - Dùng editor DevExpress phù hợp từng kiểu dữ liệu.
- Footer:
  - `Lưu`
  - `Hủy`

### Quy tắc form

- Không dùng popup quá hẹp làm vỡ form.
- Với form trung bình, ưu tiên chiều rộng khoảng `720px` đến `960px`.
- Với dữ liệu phân nhóm rõ, dùng `DxFormLayoutGroup`.
- Validation hiển thị tiếng Việt, ngắn gọn và đúng ngữ cảnh.
- Sau khi lưu thành công hoặc thất bại, phải có notification chuẩn của dự án.

### Nội dung form theo nhóm

Tùy module, popup có thể chia theo các nhóm sau:

- Thông tin chính
- Thông tin tổ chức
- Thông tin liên hệ
- Trạng thái hiệu lực
- Ghi chú

## Luồng tương tác chuẩn

### Thêm mới

1. Người dùng bấm `Mới`.
2. Mở popup form trống.
3. Người dùng nhập dữ liệu và bấm `Lưu`.
4. Hệ thống validate, lưu dữ liệu, đóng popup.
5. Grid hoặc TreeList tải lại hoặc cập nhật dòng mới.

### Chỉnh sửa

1. Người dùng chọn một dòng.
2. Bấm `Điều chỉnh`.
3. Mở popup với dữ liệu hiện có.
4. Người dùng cập nhật và bấm `Lưu`.
5. Hệ thống cập nhật dữ liệu và làm mới danh sách.

### Xóa

1. Người dùng chọn một dòng.
2. Bấm `Xóa`.
3. Hiển thị hộp thoại xác nhận.
4. Sau khi xác nhận, thực hiện xóa và thông báo kết quả.

### Xem chi tiết song song

Chỉ áp dụng cho operational list page khi workflow cần giữ ngữ cảnh list.

1. Người dùng chọn hoặc mở một bản ghi.
2. Hệ thống mở drawer hoặc popup detail.
3. Người dùng xem thêm thông tin hoặc thao tác trong ngữ cảnh hiện tại.
4. Danh sách chính vẫn giữ được vùng nhìn và selection theo rule của màn đó.

## Chuẩn triển khai component

Khi hiện thực trong source, nên tách thành các khối có thể tái sử dụng:

- `HrmListPageToolbar`
- `HrmEntityGrid`
- `HrmEntityTreeList`
- `HrmEditPopup`
- `HrmStatusBadge`

Nếu một màn là production screen có logic rõ ràng, ưu tiên tách theo bộ ba:

- `Screen.razor`
- `Screen.razor.css`
- `Screen.razor.cs`

State, selection, orchestration filter và action không nên nằm inline trong `@code` dài ở page chính.

## Gợi ý áp dụng cho các module HRM

### Nên dùng `DxGrid`

- Nhân sự
- Hợp đồng
- Yêu cầu nghỉ phép
- Bảng chấm công
- Quyết định nhân sự

### Nên dùng `DxTreeList`

- Phòng ban nhiều cấp
- Cơ cấu tổ chức
- Cây đơn vị
- Danh mục phân cấp

## Kết luận

Chuẩn bố cục danh sách cho HRM là:

1. Toolbar trên cùng với tên màn hình bên trái và một hàng action bên phải gồm
   `Mới`, `Điều chỉnh`, `Xóa`, `Làm mới`, action nghiệp vụ riêng, `Xuất dữ liệu`,
   `Chọn cột`.
2. Một vùng danh sách chính duy nhất dùng `DxGrid` hoặc `DxTreeList`; nếu là
   `Operational List Page`, vùng này có thể có header `summary badge + search`
   theo chuẩn `NhanVien`.
3. Một popup edit form dùng chung cho thao tác thêm và sửa.

Đây là blueprint mặc định khi thiết kế các màn quản trị danh sách trong ứng dụng HRM, trừ khi có yêu cầu nghiệp vụ đặc biệt khác.

