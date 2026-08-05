# Quy Tắc Icon DevExpress

Áp dụng cho mọi UI Blazor trong `src/Vnta.HRM2026`.

## 1. Bắt buộc dùng DevExpress Icons

- UI phải dùng DevExpress Icon Library từ `DevExpress.Images.Blazor`.
- Với component DevExpress có hỗ trợ icon, dùng `IconUrl` trỏ tới icon DevExpress.
- Trong Web.Client, ưu tiên dùng helper tập trung `VntaDevExpressIcons` thay vì gọi rải rác `Icon.*` trong từng page.
- Không dùng Bootstrap Icons, class `bi`, class `bi-*`, CDN `bootstrap-icons` hoặc CSS icon font tương tự.

## 2. Không thêm icon library khác

- Không thêm thư viện icon mới nếu DevExpress Icon Library đã đáp ứng được.
- Không dùng lại SVG local hoặc CSS mask cho icon thao tác phổ biến như thêm, sửa, xóa, làm mới, xuất dữ liệu, chọn cột, tìm kiếm, xem chi tiết, lưu hoặc hủy.
- Ngoại lệ cho logo thương hiệu hoặc provider đăng nhập phải được ghi rõ trong tài liệu màn hình hoặc implementation notes.

## 3. Cách dùng chuẩn

Ví dụ với `DxToolbarItem`:

```razor
<DxToolbarItem Text="Làm mới"
               Click="ReloadAsync"
               IconUrl="@VntaDevExpressIcons.Refresh" />
```

Ví dụ với `DxButton`:

```razor
<DxButton Text="Lưu"
          SubmitFormOnClick="true"
          IconUrl="@VntaDevExpressIcons.Save" />
```

Ví dụ trong template HTML:

```razor
<img class="toolbar-search-icon"
     src="@VntaDevExpressIcons.Search"
     alt=""
     aria-hidden="true" />
```

## 4. Khi cần icon mới

- Tìm icon trong DevExpress Icon Explorer hoặc metadata package `DevExpress.Images.Blazor`.
- Nếu icon sẽ dùng lại từ hai nơi trở lên, thêm property mới vào `VntaDevExpressIcons`.
- Tên property trong helper phải mô tả hành động hoặc vai trò UI, không phải tên thư viện cũ.
- Sau khi thêm icon mới, chạy build và rà:

```powershell
rg -n 'bootstrap-icons|IconCssClass="bi|class="bi|bi-|bootstrap-icon' src\Vnta.HRM2026 -S
```

## 5. Tiêu chí review

- Không có link CDN Bootstrap Icons trong host.
- Không có `IconCssClass="bi ..."` hoặc `class="bi ..."` trong source runtime.
- Không thêm CSS pseudo-element chỉ để render icon font.
- Icon mới có ý nghĩa đúng với hành động và không làm lệch layout ở toolbar/menu/popup.
