# Feature Folder Standard

Tài liệu này chốt quy ước quản lý file theo folder và naming theo cùng một ngữ
cảnh nghiệp vụ bằng tiếng Việt để dễ tìm, dễ review và dễ refactor.

Mục tiêu:

- dễ tìm toàn bộ file liên quan đến một feature trong thời gian ngắn
- giảm việc một màn hình dùng tên A ở UI, tên B ở service, tên C ở database
- giữ cấu trúc dễ mở rộng, dễ refactor và đúng theo SOLID

Tài liệu này được đọc cùng với:

- `doc/project/hrm-refactor-standard.md`
- `doc/project/target-solution-structure.md`
- `doc/checklists/screen-implementation-principles.md`

## 1. Nguyên tắc cốt lõi

Mỗi feature phải có 3 lớp tên rõ ràng:

- `Tên nghiệp vụ hiển thị`:
  - là tên tiếng Việt có dấu cho UI và tài liệu
  - ví dụ: `Nhân viên`, `Phụ cấp cơm`, `Máy chấm công`
- `Context key`:
  - là tên code chính thức bằng tiếng Việt không dấu
  - được dùng xuyên suốt cho folder, file, class và interface
  - ví dụ: `NhanVien`, `PhuCapCom`, `MayChamCong`
- `Technical alias`:
  - chỉ là tên phụ trợ bằng tiếng Anh nếu cần map với schema, legacy code, external
    integration hoặc package
  - ví dụ: `Employee`, `MealAllowance`, `AttendanceDevice`

Quy tắc ưu tiên:

1. nghiệp vụ và tài liệu đọc theo `Tên nghiệp vụ hiển thị`
2. code và folder feature đọc theo `Context key`
3. `Technical alias` chỉ được dùng khi thật sự cần, không được trở thành tên
   quản lý chính của feature

## 2. Context key phải dùng ở đâu

`Context key` là tên code chính thức được dùng xuyên suốt cho:

- tên folder feature
- tên file
- tên class
- tên interface
- tên endpoint file
- tên data provider

Caption UI vẫn là tiếng Việt có dấu. `Context key` dùng PascalCase, không dấu,
không viết tắt mơ hồ và bám sát nghiệp vụ mà người dùng hiện đang quen gọi.

## 3. Quy tắc đặt folder

Quản lý theo chiều dọc của từng project, nhưng cùng một `Context key` phải lặp lại
ở mỗi layer.

Mẫu khuyến nghị:

```text
src/Vnta.HRM2026/
  Vnta.Hrm.Web.Client/
    Components/
      {NhomNghiepVu}/
        {ContextKey}/
    Models/
      {NhomNghiepVu}/
        {ContextKey}/
    Services/
      Api/{NhomNghiepVu}/{ContextKey}/
      DataProviders/{NhomNghiepVu}/{ContextKey}/

  Vnta.Hrm.Web/
    Endpoints/
      {NhomNghiepVu}/
        {ContextKey}/
    Services/
      {NhomNghiepVu}/
        {ContextKey}/

  Vnta.Hrm.Application/
    {NhomNghiepVu}/
      {ContextKey}/

  Vnta.Hrm.Domain/
    {NhomNghiepVu}/
      {ContextKey}/

  Vnta.Hrm.Infrastructure/
    {NhomNghiepVu}/
      {ContextKey}/
```

Với persistence service phục vụ trực tiếp một màn hình, ưu tiên mirror nhóm nghiệp vụ
của UI để việc tra cứu xuyên layer không phụ thuộc tên technical alias:

```text
Vnta.Hrm.Infrastructure/
  {NhomNghiepVu}/
    {ContextKey}/
```

Ví dụ UI `Components/NhanSu/ChiTietNhanVien/` có implementation tại
`Vnta.Hrm.Infrastructure/NhanSu/ChiTietNhanVien/`. Các thành phần cross-cutting
như `Data`, migrations, Identity, module DI và external gateway dùng chung vẫn ở
root kỹ thuật phù hợp; không tạo folder màn hình chỉ để chứa các thành phần này.

Ghi chú:

- `NhomNghiepVu` ưu tiên tiếng Việt không dấu như:
  - `NhanSu`
  - `ChamCong`
  - `PhuCap`
  - `TinhLuong`
- `ModuleRootHienTai` chỉ được giữ ở source legacy chưa refactor trong giai đoạn
  chuyển tiếp, ví dụ:
  - `Employees`
  - `Attendance`
  - `Payroll`

Nếu implementation chỉ là adapter cho external gateway, được phép giữ technical
alias trong file/class hoặc chèn thêm tầng integration khi điều đó làm boundary rõ hơn:

```text
Vnta.Hrm.Infrastructure/
  Integrations/
    AttendanceGateway/
      {ModuleRootHienTai}/
        {ContextKey}/
```

## 4. Quy tắc đặt tên file

Dùng cùng một `Context key`, sau đó thêm hậu tố theo vai trò file.

### UI

```text
{ContextKey}.razor
{ContextKey}.razor.cs
{ContextKey}.razor.css
{ContextKey}EditForm.razor
{ContextKey}DetailPopup.razor
{ContextKey}Status.razor
{ContextKey}Record.cs
{ContextKey}DataProvider.cs
```

### Web host hoặc endpoint

```text
{ContextKey}Endpoints.cs
{ContextKey}EndpointMapper.cs
```

Nếu chưa tách được endpoint mapper riêng, tối thiểu phải có file
`{ContextKey}Endpoints.cs`, không tiếp tục dồn thêm vào mega file liên module.

### Application

```text
{ContextKey}Filter.cs
{ContextKey}ListItemDto.cs
Create{ContextKey}Request.cs
Update{ContextKey}Request.cs
Upsert{ContextKey}Request.cs
Delete{ContextKey}Request.cs
I{ContextKey}Service.cs
I{ContextKey}RefreshService.cs
I{ContextKey}WorkflowService.cs
```

Không cần tạo đủ tất cả file trên. Chỉ tạo file phù hợp với workflow thật sự.

### Infrastructure

```text
{ContextKey}Row.cs
{ContextKey}RowConfiguration.cs
Database{ContextKey}Service.cs
Database{ContextKey}RefreshService.cs
{ContextKey}Module.cs
```

Nếu feature có implementation HTTP client:

```text
Http{ContextKey}Service.cs
```

## 5. Quy tắc "giống nhau về ngữ cảnh"

"Giống nhau" không có nghĩa mọi file phải trùng 100 phần trăm chuỗi ký tự.
Nghĩa đúng là cùng một nghiệp vụ phải dùng cùng một `Context key` bằng tiếng Việt
không dấu.

Ví dụ đúng:

- `NhanVien.razor`
- `NhanVienDataProvider.cs`
- `HttpNhanVienService.cs`
- `INhanVienService.cs`
- `DatabaseNhanVienService.cs`
- `NhanVienRow.cs`
- `NhanVienEndpoints.cs`

Ví dụ không đúng:

- UI dùng `NhanVien`
- data provider dùng `Employee`
- endpoint dùng `PayrollEmployee`
- row dùng `AttendanceGatewayEmployee`

Đó là cùng nghiệp vụ nhưng 4 tên khác nhau, rất khó tìm và khó review.

## 6. Technical alias được dùng khi nào

`Technical alias` được phép xuất hiện ở:

- tên bảng và tên cột schema đã ổn định
- lớp map external integration
- migration cũ
- contract giao tiếp với hệ thống khác nếu đổi tên sẽ gây vỡ boundary

`Technical alias` không nên là tên quản lý chính cho:

- folder feature mới
- file UI mới
- screen đọc mới
- checklist và template mới

## 7. Quy tắc theo SOLID

### S - Single Responsibility Principle

- mỗi folder `{ContextKey}` chỉ chứa một feature context
- mỗi file chỉ giữ một vai trò rõ ràng:
  - UI screen
  - popup
  - provider
  - endpoint
  - contract
  - service
  - row model

### Ở - Open/Closed Principle

- khi thêm feature mới, ưu tiên tạo folder `{ContextKey}` mới
- tránh sửa một file tổng quá lớn chỉ để nhận thêm feature khác
- endpoint, provider, service nên tách theo context để mở rộng mà không làm nó
  thành một file trung tâm

### L - Liskov Substitution Principle

- UI và endpoint phụ thuộc vào abstraction như `I{ContextKey}Service`
- implementation `Database{ContextKey}Service` hoặc `Http{ContextKey}Service`
  có thể thay nhau mà không đổi contract cấp trên

### I - Interface Segregation Principle

- không tạo interface to vừa đọc, vừa save, vừa refresh, vừa lock nếu workflow
  có thể tách nhỏ hơn
- ưu tiên interface hẹp theo nghiệp vụ:
  - `INhanVienService`
  - `INhanVienRefreshService`
  - `IPhuCapComService`

### D - Dependency Inversion Principle

- UI phụ thuộc vào provider hoặc interface, không phụ thuộc `DbContext`
- endpoint phụ thuộc interface application
- infrastructure là nơi implement abstraction

## 8. Cấu trúc mẫu khuyến nghị

### Ví dụ 1 - `NhanVien`

```text
Vnta.Hrm.Web.Client/
  Components/
    NhanSu/
      NhanVien/
        NhanVien.razor
        NhanVien.razor.cs
        NhanVien.razor.css
        NhanVienEditForm.razor
        NhanVienDetailPopup.razor
  Models/
    NhanSu/
      NhanVien/
        NhanVienRecord.cs
  Services/
    Api/NhanSu/NhanVien/
      HttpNhanVienApiService.cs
    DataProviders/NhanSu/NhanVien/
      NhanVienDataProvider.cs

Vnta.Hrm.Web/
  Endpoints/
    NhanSu/
      NhanVien/
        NhanVienEndpoints.cs
  Services/
    NhanSu/
      NhanVien/
        ServerNhanVienApiService.cs

Vnta.Hrm.Application/
  NhanSu/
    NhanVien/
      NhanVienFilter.cs
      NhanVienListItemDto.cs
      CreateNhanVienRequest.cs
      UpdateNhanVienRequest.cs
      INhanVienService.cs
      INhanVienRefreshService.cs

Vnta.Hrm.Infrastructure/
  NhanSu/
    NhanVien/
      NhanVienRow.cs
      NhanVienRowConfiguration.cs
      DatabaseNhanVienService.cs
      DatabaseNhanVienRefreshService.cs
```

### Ví dụ 2 - `PhuCapCom`

```text
Vnta.Hrm.Web.Client/
  Components/
    PhuCap/
      PhuCapCom/
        PhuCapCom.razor
        PhuCapCom.razor.cs
        PhuCapCom.razor.css
        PhuCapComEditForm.razor
  Models/
    PhuCap/
      PhuCapCom/
        PhuCapComRecord.cs
  Services/
    Api/PhuCap/PhuCapCom/
      HttpPhuCapComApiService.cs
    DataProviders/PhuCap/PhuCapCom/
      PhuCapComDataProvider.cs

Vnta.Hrm.Web/
  Endpoints/
    PhuCap/
      PhuCapCom/
        PhuCapComEndpoints.cs

Vnta.Hrm.Application/
  PhuCap/
    PhuCapCom/
      PhuCapComFilter.cs
      PhuCapComListItemDto.cs
      UpsertPhuCapComRequest.cs
      IPhuCapComService.cs

Vnta.Hrm.Infrastructure/
  PhuCap/
    PhuCapCom/
      PhuCapComRow.cs
      PhuCapComRowConfiguration.cs
      DatabasePhuCapComService.cs
```

## 9. Quy tắc chuyển đổi từ hiện trạng repo

Repo hiện tại đang có nhiều tên cặp UI và backend chưa thống nhất.

Hướng chuyển đổi:

1. Chọn `Tên nghiệp vụ hiển thị`.
2. Chọn `Context key` bằng tiếng Việt không dấu.
3. Ghi rõ `Technical alias` nếu feature đang map với schema hay service cũ.
4. Khi refactor feature, đổi folder và file theo `Context key` mới trong cùng một đợt.
5. Không đổi nửa này nửa kia, tránh để tên cũ và tên mới song song quá lâu.

Ví dụ mapping đích khuyến nghị:

- `NhanVien`
  - `Context key`: `NhanVien`
  - `Technical alias`: `Employee`
- `PhuCapCom`
  - `Context key`: `PhuCapCom`
  - `Technical alias`: `MealAllowance`

Không bắt buộc đổi tên ngay lập tức cho toàn repo. Nhưng feature mới hoặc feature
được refactor lớn phải bám theo `Context key` tiếng Việt không dấu.

## 10. Anti-pattern cần cấm

- một feature nằm trong nhiều folder toàn cục không có `Context key` chung
- tiếp tục dồn endpoint vào `PayrollEndpoints.cs` vô hạn
- để model UI ở một nơi, provider ở một nơi, contract ở một nơi mà không có tài
  liệu mapping
- để `Technical alias` trở thành tên quản lý chính của feature
- tạo interface quá to không theo workflow

## 11. Checklist tối thiểu khi mở feature mới

- [ ] Đã chốt `Tên nghiệp vụ hiển thị`.
- [ ] Đã chốt `Context key` bằng tiếng Việt không dấu.
- [ ] Đã xác định có cần `Technical alias` không.
- [ ] Đã vẽ được folder map từ UI đến infrastructure.
- [ ] Đã đổi tên file theo cùng `Context key`.
- [ ] Đã xác định interface nào cần tách riêng để đúng theo SOLID.
- [ ] Đã ghi rõ mapping tên cũ nếu đây là đợt refactor.

## 12. Tài liệu phải cập nhật cùng

Khi một feature áp dụng chuẩn này, cập nhật thêm:

- `doc/templates/screen-implementation-template.md`
- `doc/checklists/screen-implementation-principles.md`
- `doc/project/refactor-gap-register.md` nếu repo hiện trạng chưa đạt
- `doc/implementation-log/yyyyMMdd.md`


