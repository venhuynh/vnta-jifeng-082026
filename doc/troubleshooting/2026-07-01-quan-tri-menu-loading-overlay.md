# Quan Tri menu loading overlay fix

## Muc đích

Tài liệu này dùng để sửa lỗi các màn hình trong menu `Quản trị` bị treo ở global Loading khi mở nhiều tab hoặc click nhiều node menu liên tiếp.

Phạm vì menu hiện hành:

- `Components/QuanTri/MayChamCong/`
- `Components/QuanTri/GiamSatAdms/`
- `Components/QuanTri/LenhMayChamCong/`

Route lien quan:

- `/attendance/devices`
- `/Adms`
- `/adms/device-commands`

## Dau hieu

- Các node trong `UI DEMO` mở nhiều tab binh thướng.
- Các node trong `Quản trị` có thể dung ở màn hình global Loading.
- Server vẫn có thể trả HTML hoặc redirect auth, nhưng browser không thấy nội dung page do loading overlay chưa được gỡ.

## Root cause

Có ba lop nguyen nhan chinh:

1. Render mode của một so page `QuanTri` bi ep về `@rendermode InteractiveServer`, trong khi app tong dang render `Routes` bang `InteractiveAutoRenderMode(false)`.
2. Các page `QuanTri` gọi DB/API/SignalR ngay trong `OnInitializedAsync`. Khi mở nhiều tab, page chưa kip render shell `.page` thì đã đợi data load/circuit/hub.
3. Global Loading trong `App.razor` chi go overlay khi `MutationObserver` gap mutation có target khop `.page`, dieu kien nay qua hep nen để ket vinh vien.

## Huong sua chuan

### 1. Khong ep InteractiveServer trong Web.Client page

Go dong nay khối các page trong `Components/QuanTri` nếu có:

```razor
@rendermode InteractiveServer
```

Page nen di theo render mode chung của app trong `App.razor`.

### 2. Render shell trước, load data sau

Với page đọc DB/API lúc mở màn hình, không gọi load data trực tiếp trong `OnInitializedAsync`.

Mau cho màn hình danh sách:

```csharp
private bool IsLoading { get; set; } = true;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await ReloadAsync();
        await InvokeAsync(StateHasChanged);
    }
}
```

Với màn hình cần đọc auth state trước:

```csharp
protected override async Task OnInitializedAsync()
{
    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    CanView = authState.User.Identity?.IsAuthenticated == true;
    CanManage = CanView;
    IsLoading = CanView;
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && CanView)
    {
        await LoadAsync();
        await InvokeAsync(StateHasChanged);
    }
}
```

Với màn hình realtime/hub:

```csharp
protected override async Task OnInitializedAsync()
{
    monitorOptions = MonitorOptionsAccessor.Value ?? new AdmsGatewayMonitorOptions();
    deviceStatusLoopTask = RunDeviceStatusLoopAsync(disposalTokenSource.Token);
    await base.OnInitializedAsync();
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await ConnectToGatewayAsync();
    }

    await base.OnAfterRenderAsync(firstRender);
}
```

### 3. Gia có global Loading trong App.razor

Thay script loading overlay bang mau có fallback:

```html
<script>
    (() => {
        const removeLoadingPanel = () => {
            const loadingPanel = document.querySelector(".loading-panel");
            if(!loadingPanel)
                return true;

            if(!document.querySelector(".page"))
                return false;

            loadingPanel.remove();
            return true;
        };

        if(removeLoadingPanel())
            return;

        const observer = new MutationObserver(() => {
            if(removeLoadingPanel())
                observer.disconnect();
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });

        window.setTimeout(() => {
            observer.disconnect();
            document.querySelector(".loading-panel")?.remove();
        }, 15000);
    })();
</script>
```

## Kiem chung bắt buộc

```powershell
rg -n "@rendermode InteractiveServer" src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri -g "*.razor"
dotnet build src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj -c Release -v:minimal
```

Smoke test các route:

- `/attendance/devices`
- `/Adms`
- `/adms/device-commands`
- đối chiếu với `/ContactList` hoặc `/Dashboard`

Nếu chưa đăng nhập, `302` redirect đến login là chấp nhận được. Dieu cần xác nhận là request không treo lâu và global Loading không kẹt vĩnh viễn.

## Checklist review

- [ ] Khong con `@rendermode InteractiveServer` trong `Components/QuanTri`.
- [ ] `MayChamCong` load data sau first render.
- [ ] `LenhMayChamCong` chi kiểm tra auth trong `OnInitializedAsync`, load data sau first render.
- [ ] `GiamSatAdms` chi setup options/timer trong `OnInitializedAsync`, connect hub sau first render.
- [ ] Global Loading được go khi `.page` xuat hien.
- [ ] Global Loading có fallback timeout khoang 15 giay.
- [ ] Build Release thành công.
- [ ] Các route Quản trị và route đối chiếu UI DEMO không treo request.
## Prompt cho may/branch khac

```text
Bạn đang làm việc trong repo Vnta-Blazor-2026 trên một feature branch, không phải main.

Van để cần sua:
Các màn hình trong menu `Quản trị` có thể treo global Loading khi mở nhiều tab hoặc click nhiều node menu liên tiếp. UI DEMO vẫn mở bình thường.

Phạm vì cần rà soát:
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/GiamSatAdms/
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/
- src/Vnta.HRM2026/Vnta.Hrm.Web/Components/App.razor

Yêu cầu:
1. Kiểm tra `git status --short --branch` và không làm mất thay đổi hiện có.
2. Tim và go `@rendermode InteractiveServer` khối các page `.razor` trong `Components/QuanTri`.
3. Khong load DB/API/SignalR ngay trong `OnInitializedAsync` của các màn hình `Quản trị`.
4. Chuyen initial load sang `OnAfterRenderAsync(bool firstRender)`:
   - `MayChamCong`: dat `IsLoading = true`, goi `ReloadAsync()` trong first render.
   - `LenhMayChamCong`: chi lay auth state trong `OnInitializedAsync`, dat `IsLoading = CanView`, goi `LoadAsync()` trong first render nếu `CanView`.
   - `GiamSatAdms`: giữ setup option/timer trong `OnInitializedAsync`, chuyen `ConnectToGatewayAsync()` sang first render.
5. Sua script global Loading trong `App.razor`:
   - remove overlay khi tim thay `.page`
   - them fallback timeout khoang 15 giay
   - không chỉ phụ thuộc vao mutation target khop `.page`
6. Chạy:
   - `rg -n "@rendermode InteractiveServer" src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri -g "*.razor"`
   - `dotnet build src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj -c Release -v:minimal`
7. Smoke test:
   - `/attendance/devices`
   - `/Adms`
   - `/adms/device-commands`
   - đối chiếu với `/ContactList` hoặc `/Dashboard`
8. Bao cao file da sua, kết quả build, kết quả smoke test, và có commit/push hay chua.

Tieu chi chap nhan:
- Khong con `@rendermode InteractiveServer` trong `Components/QuanTri`.
- Các màn hình `Quản trị` render shell trước khi goi data/hub.
- Global Loading không kẹt vĩnh viễn khi page cham hoặc khi mở nhiều tab.
- Build Release thành công.
```





