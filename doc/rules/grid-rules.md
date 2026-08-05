# Quy Tắc Grid DevExpress

Áp dụng cho mọi màn hình dùng `DxGrid` hoặc biến thể danh sách dữ liệu phẳng
trong HRM.

## Mục tiêu

- Chuẩn hóa cách dựng grid cho các màn quản trị và vận hành.
- Tránh trộn lẫn nhiều pattern paging, scrolling, selection và layout.
- Giữ business logic, query và orchestration nằm ngoài Razor markup.

## 1. Trách nhiệm tải dữ liệu

- Page hoặc component chỉ bind `Data` và điều phối UI state.
- Query, filter, sort, paging contract phải nằm ở service hoặc use case.
- Không viết truy vấn EF Core, SQL hoặc logic phân trang trực tiếp trong `.razor`.
- Không copy nguyên pattern demo `@code` của DevExpress vào production screen.

## 2. Chọn một mode điều hướng dữ liệu

Mỗi grid chỉ được chọn một trong hai mode:

- `paging`
- `virtual scrolling`

Không được trộn cả hai trên cùng một màn.

## 3. Chuẩn cho paged grid

Dùng khi người dùng duyệt dữ liệu theo trang rõ ràng.

Baseline khuyến nghị:

```razor
<DxGrid Data="@Rows"
        PageSize="20"
        PagerPosition="GridPagerPosition.TopAndBottom"
        PageSizeSelectorVisible="true"
        PageSizeSelectorItems="@(new int[] { 10, 20, 100 })"
        PageSizeSelectorAllRowsItemVisible="true"
        PagerSwitchToInputBoxButtonCount="10"
        PagerVisibleNumericButtonCount="10"
        ColumnResizeMode="GridColumnResizeMode.NextColumn"
        TextWrapEnabled="false">
    <Columns>
        <DxGridDataColumn Caption="STT" Width="4.5rem" MinWidth="72" AllowSort="false">
            <CellDisplayTemplate>
                @(context.VisibleIndex + 1)
            </CellDisplayTemplate>
        </DxGridDataColumn>
        ...
    </Columns>
</DxGrid>
```

Quy tắc:

- `PageSize="20"` là mặc định nếu spec không yêu cầu khác.
- Toolbar nằm ngoài grid.
- Search, export, column chooser, refresh được điều phối ở page level.
- `TextWrapEnabled="false"` là mặc định cho grid vận hành dày dữ liệu.

## 4. Chuẩn cho virtual scrolling grid

Dùng khi grid có nhiều dòng và cần viewport cuộn nội bộ ổn định.

Baseline khuyến nghị:

```razor
<DxGrid Data="@Rows"
        Height="100%"
        VirtualScrollingEnabled="true"
        VirtualScrollingMode="GridVirtualScrollingMode.Rows"
        SkeletonRowsEnabled="true"
        ColumnResizeMode="GridColumnResizeMode.NextColumn"
        TextWrapEnabled="false">
    <Columns>
        <DxGridDataColumn Caption="STT" Width="4.5rem" MinWidth="72" AllowSort="false">
            <CellDisplayTemplate>
                @(context.VisibleIndex + 1)
            </CellDisplayTemplate>
        </DxGridDataColumn>
        ...
    </Columns>
</DxGrid>
```

Quy tắc:

- Grid phải nằm trong host có height thật.
- Nếu parent sở hữu chiều cao thì dùng `Height="100%"`.
- Không dùng `ShowAllRows="true"` để thay thế virtual scrolling.
- Dùng `HrmLoadingPanel` để block interaction khi reload.

## 5. Không trộn paging và virtual scrolling

Forbidden:

- paged grid nhưng bật `VirtualScrollingEnabled="true"`
- virtual scrolling grid nhưng vẫn cấu hình pager
- dựa vào `ShowAllRows="true"` để vá hành vi điều hướng dữ liệu

Nếu cần paging, bỏ virtual scrolling.

Nếu cần scrolling liên tục, bỏ toàn bộ cấu hình pager.

## 6. Selection phải có khóa ổn định

Grid có chọn dòng phải khai báo `KeyFieldName` trỏ tới field duy nhất, ổn định.

Ví dụ:

```razor
<DxGrid Data="@Rows"
        KeyFieldName="Id"
        @bind-SelectedDataItems="SelectedDataItems"
        SelectionMode="GridSelectionMode.Multiple"
        AllowSelectRowByClick="true"
        HighlightRowOnHover="true">
    <Columns>
        <DxGridSelectionColumn Width="3rem" />
        <DxGridDataColumn Caption="STT" Width="4.5rem" MinWidth="72" AllowSort="false">
            <CellDisplayTemplate>
                @(context.VisibleIndex + 1)
            </CellDisplayTemplate>
        </DxGridDataColumn>
        ...
    </Columns>
</DxGrid>
```

Không bind selection chỉ bằng object reference nếu rows được reload từ service.

## 7. Cột STT bắt buộc

Mọi `DxGrid` production trong HRM phải có cột `STT`.

Quy tắc:

- Cột `STT` dùng `context.VisibleIndex + 1` trong `CellDisplayTemplate`.
- Không bind `STT` vào DTO/entity và không thêm field `Stt`, `Index` hoặc
  `RowNumber` chỉ để phục vụ UI.
- Đặt `STT` ngay sau `DxGridSelectionColumn` hoặc `DxGridCommandColumn` nếu có.
- Nếu grid read-only không có selection/command, đặt `STT` làm cột đầu tiên.
- Cột `STT` không sort: dùng `AllowSort="false"`.

Baseline:

```razor
<DxGridDataColumn Caption="STT" Width="4.5rem" MinWidth="72" AllowSort="false">
    <CellDisplayTemplate>
        @(context.VisibleIndex + 1)
    </CellDisplayTemplate>
</DxGridDataColumn>
```

## 8. State chọn dòng nằm ở code-behind

- `SelectedDataItems` sống trong `.razor.cs`.
- Dùng helper typed selection để lấy row đang chọn.
- Không viết expression dài trong Razor để đếm hoặc lọc selection.

Ví dụ:

```csharp
private IReadOnlyList<object> selectedDataItems = [];

private IReadOnlyList<object> SelectedDataItems
{
    get => selectedDataItems;
    set
    {
        selectedDataItems = value;
        SyncSelectionState();
    }
}
```

## 9. Toolbar action phải đi qua selection helper

Enablement của action như `Sửa`, `Xóa`, `Xuất bản ghi đã chọn` phải dựa trên
helper typed selection.

Ví dụ:

```csharp
private int GetSelectedRowCount() => GetSelectedRows().Count;
```

```razor
DeleteEnabled="@(CanDelete && GetSelectedRowCount() > 0 && !IsLoading)"
```

## 10. Refresh tay phải xóa selection

Khi người dùng bấm refresh:

- clear `SelectedDataItems`
- clear focused row
- clear selected row id dùng cho restore-by-key
- sau đó mới reload dữ liệu

Lý do:

- tránh giữ action `Sửa` hoặc `Xóa` ở trạng thái bật cho selection cũ
- tránh thao tác nhầm sau khi dữ liệu đã thay đổi

## 11. Layout và CSS cho grid

Mỗi grid production phải có:

- một `CssClass` riêng cho grid
- một host class riêng để giữ height/flex context

Ví dụ:

```razor
<div class="employee-grid-host">
    <HrmLoadingPanel Visible="@IsLoading"
                     IsContentBlocked="true">
        <DxGrid CssClass="employee-grid"
                Data="@Rows">
            ...
        </DxGrid>
    </HrmLoadingPanel>
</div>
```

```css
.employee-grid-host {
    flex-grow: 1;
    height: 100%;
}

::deep .employee-grid {
    max-height: 100%;
}
```

Không dùng class quá generic như:

- `grid-host`
- `data-grid`
- `list-grid`

trừ khi đó là shared UI component có contract rõ ràng.

## 12. Skeleton rows và loading

- Grid async nên bật `SkeletonRowsEnabled="true"` khi phù hợp.
- Dùng `HrmLoadingPanel` để chặn tương tác lúc load hoặc reload.
- Không thay cả page shell bằng trạng thái trắng trơn khi chỉ đang reload grid.

## 13. Render stability bắt buộc cho DxGrid

- Mặc định phải cập nhật `Data` theo immutable pattern:
  - thêm row bằng list mới
  - sửa row bằng clone row mới hoặc list mới
  - xóa row bằng list mới
- Không `Sort`, `Add`, `Insert`, `Remove`, `Clear` trực tiếp trên collection đang bind nếu sau đó còn kỳ vọng grid diff ổn định.
- Nếu page đã tự thay `Data` hoặc đã `LoadAsync()` lại dữ liệu sau save, phải đặt `e.Reload = false` trừ khi có lý do khác được ghi rõ.
- `Grid.Reload()` chỉ là fallback kỹ thuật, không phải baseline của repo.
- Async callback liên quan grid phải đi qua `InvokeAsync(...)` và có guard dispose hoặc cancel.
- Màn có timer, realtime hoặc auto-refresh phải được smoke test với nhiều tab trước khi chốt.

## 14. Review checklist

Trước khi chốt một grid screen:

- Grid đã chọn đúng một mode: paging hoặc virtual scrolling.
- Có `CssClass` riêng cho grid.
- Có host class riêng với height context rõ ràng.
- Grid có `KeyFieldName` ổn định nếu có selection.
- Selection nằm ở `.razor.cs`.
- Toolbar action dùng typed selection helper.
- Manual refresh clear selection trước khi load.
- `TextWrapEnabled` được set có chủ đích.
- Không có query hoặc paging logic trong `.razor`.
- Loading state dùng `HrmLoadingPanel`.
- Không còn mutation trực tiếp trên data source đang bind.
- Save pipeline không chồng `LoadAsync()`, `Grid.Reload()` và `StateHasChanged()` vô cớ.
- Nếu màn có callback nền, đã có smoke test nhiều tab.
- Guardrails và checklist liên quan đã được kiểm lại.
