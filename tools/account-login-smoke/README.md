# Account Login Smoke

## Mục đích

Tool này chạy transactional smoke cho luồng account nội bộ mà không để lại dữ liệu test trong database.

Luồng đang kiểm:

- `Open`
- `Approve`
- `ResetPassword`
- `Deactivate`
- `Activate`

Cuối lượt chạy, tool luôn `ROLLBACK` transaction và kiểm lại `persistedAfterRollback = false`.

## Cách chạy

Từ repo root:

```powershell
dotnet run --project tools/account-login-smoke/AccountLoginSmoke.csproj -p:UseSharedCompilation=false
```

Chạy cho một employee cụ thể:

```powershell
dotnet run --project tools/account-login-smoke/AccountLoginSmoke.csproj -p:UseSharedCompilation=false -- --employee-code 00001
```

Chỉ định reviewer khác:

```powershell
dotnet run --project tools/account-login-smoke/AccountLoginSmoke.csproj -p:UseSharedCompilation=false -- --reviewer admin
```

Chỉ định connection string khác:

```powershell
dotnet run --project tools/account-login-smoke/AccountLoginSmoke.csproj -p:UseSharedCompilation=false -- --connection "Host=...;Port=...;Database=...;Username=...;Password=..."
```

## Quy tắc an toàn

- Mặc định tool chọn employee chưa có account.
- Nếu employee đã có account, tool dừng ngay với exit code khác `0`.
- Nếu không truyền `--connection`, tool đọc `VNTA_DB` trước, rồi mới fallback về connection string mặc định giống infrastructure hiện tại.
- Đây là smoke mức service/data layer, không thay thế browser smoke.
