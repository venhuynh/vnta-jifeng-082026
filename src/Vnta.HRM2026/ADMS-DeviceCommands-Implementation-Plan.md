# ADMS Device Commands Implementation Plan

## Mục tiêu

Tài liệu này mô tả các file và quyết định triển khai cho màn `/adms/device-commands` trong solution HRM hiện tại.

## Vị trí source hiện hành

### Application

- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/IAdmsDeviceCommandService.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/AdmsDeviceCommandSummaryDto.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/AdmsDeviceCommandDetailDto.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/AdmsDeviceCommandFilter.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/UpsertAdmsDeviceCommandRequest.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/AdmsDeviceCommandStatus.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/AdmsLookupItemDto.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Application/Integrations/AttendanceGateway/AdmsDeviceCommandLookupOptionsDto.cs`

### Infrastructure

- `src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/Integrations/AttendanceGateway/MockAdmsDeviceCommandService.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/Integrations/AttendanceGateway/AttendanceGatewayIntegrationModule.cs`

### Web Client

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCong.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCong.razor.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCong.razor.css`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCongEditForm.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCongEditForm.razor.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCongEditForm.razor.css`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCongEditModel.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCongPageState.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/LenhMayChamCongExportFormat.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/LenhMayChamCong/README.md`

### Navigation

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs`

## Ghi chú về baseline menu/source

- Trước baseline ngày `2026-07-01`, màn này từng nằm dưới `Components/Implementation/Pages/AdmsDeviceCommands/`.
- Source of truth hiện hành là nhánh `Components/QuanTri/LenhMayChamCong/`.
- Caption menu hiện hành là `Quản trị > Lệnh máy chấm công`.

## Quyết định scaffold hiện tại

- UI đi qua `IAdmsDeviceCommandService`, không kéo trực tiếp source hay DbContext từ `zkteco-adms-gateway` vào page.
- Có thể bắt đầu bằng mock service rồi thay bằng adapter thật ở `Infrastructure`.
- Popup edit dùng model trung gian riêng để bind với DevExpress editor nhẹ nhàng hơn.

## Những điểm cố ý chưa làm

- Chưa có adapter thật tới nguồn dữ liệu production.
- Chưa có detail drawer riêng.
- Chưa có action retry hoặc cancel command.
- Chưa có typed form theo từng loại command.

## Tài liệu nên đọc kèm

- `doc/screens/quan-tri/lenh-may-cham-cong.md`
- `doc/project/source-map.md`
- `doc/project/menu-sync-20260701.md`
