# LenhMayChamCong

Screen này sở hữu route `/adms/device-commands` trong HRM.

Mục tiêu của scaffold hiện tại:

- dựng khung màn hình quản lý lệnh máy chấm công;
- tách file theo screen folder riêng;
- giữ contract đủ gần với nguồn `ZktecoDeviceCommand` để thay adapter thật sau.

Nguồn tham chiếu chính:

- `src/zkteco-adms-gateway/Domain/ZktecoDeviceCommand.cs`
- `docs/02-ux-devexpress/screens/SCR-003-adms-device-command.md` ở repo tham chiếu thiết kế
- module mẫu `DeviceCommandList` và `PositionList` trong repo tham chiếu Blazor
