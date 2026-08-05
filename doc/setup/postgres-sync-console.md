# Thiết Lập Và Vận Hành Console Đồng Bộ PostgreSQL

Tài liệu này mô tả cách dùng solution console độc lập `src/Vnta.PostgresSync` để kiểm tra schema và đồng bộ dữ liệu từ PostgreSQL nguồn sang PostgreSQL đích.

## Vị trí source

- Solution: `src/Vnta.PostgresSync/Vnta.PostgresSync.slnx`
- Project console: `src/Vnta.PostgresSync/Vnta.PostgresSync.Console/Vnta.PostgresSync.Console.csproj`
- File cấu hình mặc định: `src/Vnta.PostgresSync/Vnta.PostgresSync.Console/appsettings.json` (được commit, không chứa connection string)
- File mẫu local: `src/Vnta.PostgresSync/Vnta.PostgresSync.Console/appsettings.Local.example.json`
- File local: `src/Vnta.PostgresSync/Vnta.PostgresSync.Console/appsettings.Local.json` (đã ignore, không commit)

## Mục đích

Solution này phục vụ hai nhu cầu:

- kiểm tra schema thực tế của database nguồn và database đích trước khi cấu hình sync
- chạy đồng bộ dữ liệu theo pha, ưu tiên `master data` trước rồi mới đến dữ liệu chấm công hằng ngày

## Cấu hình cục bộ Jifeng

`appsettings.json` được commit với connection string rỗng. Không sửa hoặc commit file
này. Console nạp cấu hình theo thứ tự `appsettings.json`,
`appsettings.{Environment}.json`, `appsettings.Local.json`, rồi biến môi trường.

Tạo file local từ mẫu:

```powershell
Copy-Item src\Vnta.PostgresSync\Vnta.PostgresSync.Console\appsettings.Local.example.json src\Vnta.PostgresSync\Vnta.PostgresSync.Console\appsettings.Local.json
```

Dùng `ConnectionStrings:SourcePostgres` cho database nguồn được vận hành cấp quyền và
`ConnectionStrings:TargetPostgres` cho database đích Jifeng, với `Database=jifeng_hrm`.
Hai key này dùng được cho cả `inspect` và các lệnh `sync-*`.

Có thể dùng biến môi trường `ConnectionStrings__SourcePostgres` và
`ConnectionStrings__TargetPostgres`; chúng ghi đè file local. Không ghi credential vào
source, log hoặc tài liệu.

Trước khi chạy đồng bộ, chạy `inspect`. Lệnh này chỉ đọc schema; các lệnh `sync-*` có
thể upsert dữ liệu vào database đích và chỉ được chạy khi source/target đã được phê duyệt.

## Các lệnh hỗ trợ

### Kiểm tra schema

```powershell
dotnet run --project src/Vnta.PostgresSync/Vnta.PostgresSync.Console/Vnta.PostgresSync.Console.csproj -- inspect
```

Lệnh này đọc metadata bảng và cột từ cả hai database, sau đó in ra:

- danh sách bảng nguồn
- danh sách bảng đích
- danh sách bảng trùng tên
- danh sách bảng chỉ có ở nguồn hoặc chỉ có ở đích
- nhóm bảng ứng viên liên quan đến chấm công

### Đồng bộ master data

```powershell
dotnet run --project src/Vnta.PostgresSync/Vnta.PostgresSync.Console/Vnta.PostgresSync.Console.csproj -- sync-master
```

Pha này dùng để đồng bộ dữ liệu nền trước, hiện cấu hình theo thứ tự:

1. `public.departments`
2. `public.positions`
3. `public.employees`
4. `public.devices`
5. `public.shifts`
6. `public.device_user_profiles`
7. `public.biodata`
8. `public.face_templates`
9. `public.fingerprint_templates`
10. `public.fvein_templates`
11. `public.bio_photos`
12. `public.user_pictures`

## Đồng bộ dữ liệu chấm công hằng ngày

```powershell
dotnet run --project src/Vnta.PostgresSync/Vnta.PostgresSync.Console/Vnta.PostgresSync.Console.csproj -- sync-attendance
```

Pha này hiện đồng bộ:

1. `public.attendance_logs`
2. `public.attendance_daily_summaries`
3. `public.attendance_workday_summaries`

Mặc định, cả ba query đều lọc theo ngày chạy hiện tại:

- `attendance_logs` lọc theo `AttTime`
- `attendance_daily_summaries` lọc theo `WorkDate`
- `attendance_workday_summaries` lọc theo `WorkDate`

Có thể backfill theo khoảng ngày bằng `--from` và `--to`:

```powershell
dotnet run --project src/Vnta.PostgresSync/Vnta.PostgresSync.Console/Vnta.PostgresSync.Console.csproj -- sync-attendance --from 2026-05-01 --to 2026-06-30
```

Lệnh trên vẫn chạy kèm `master data` liên quan của pha attendance, đặc biệt là `public.employees`, trước khi đẩy dữ liệu chấm công và `attendance_workday_summaries`.

Từ cấu hình hiện tại, pha attendance cũng kéo `public.shifts` và `public.attendance_status_codes` như dependency để giữ FK và dữ liệu diễn giải đồng bộ với `attendance_workday_summaries`.

Nếu cần chỉ định riêng `public.attendance_workday_summaries`, console vẫn tự động kéo thêm `public.attendance_logs`, `public.attendance_daily_summaries` và các bảng dependency attendance để giữ dữ liệu đồng bộ theo cùng khoảng ngày.

Các token thời gian đang hỗ trợ trong `SourceQuery`:

- `{{today}}`
- `{{yesterday}}`
- `{{tomorrow}}`
- `{{today_start}}`
- `{{yesterday_start}}`
- `{{tomorrow_start}}`
- `{{from_date}}`
- `{{to_date}}`
- `{{to_exclusive_date}}`
- `{{from_start}}`
- `{{to_exclusive_start}}`

## Đồng bộ toàn bộ theo thứ tự pha

```powershell
dotnet run --project src/Vnta.PostgresSync/Vnta.PostgresSync.Console/Vnta.PostgresSync.Console.csproj -- sync-all
```

Lệnh này chạy:

1. `MasterData`
2. `AttendanceDaily`

## Ghi chép vận hành lịch sử

Các số liệu và ngày tháng trong mục này là bằng chứng của một lần kiểm chứng trước đây,
không phải baseline dữ liệu hiện tại. Khi chạy lại, thay ngày, khoảng lọc và số liệu bằng
giá trị thực tế của môi trường Jifeng; không dùng lại connection string trong lịch sử.

Đã kiểm chứng ngày `2026-07-13`:

- `inspect` chạy được khi gọi DLL đã build sẵn:

```powershell
dotnet src/Vnta.PostgresSync/Vnta.PostgresSync.Console/bin/Debug/net10.0/Vnta.PostgresSync.Console.dll inspect
```

- sync kiểm chứng end-to-end theo ngày:

```powershell
dotnet src/Vnta.PostgresSync/Vnta.PostgresSync.Console/bin/Debug/net10.0/Vnta.PostgresSync.Console.dll sync-attendance --from 2026-05-01 --to 2026-05-01 --table public.attendance_workday_summaries
```

- kết quả kiểm chứng trên dữ liệu thật:
  - `departments=73`
  - `positions=51`
  - `employees=1115`
  - `devices=8`
  - `shifts=2`
  - `attendance_status_codes=42`
  - `attendance_logs=0`
  - `attendance_daily_summaries=0`
  - `attendance_workday_summaries=1`

Nếu `dotnet run` lỗi quyền đọc file `NuGet.Config`, kiểm tra quyền của NuGet configuration
trên máy hiện tại hoặc chạy bằng DLL đã build như các ví dụ trên.

## Đồng bộ `payroll_basic_salary_records` từ kỳ trước

`public.payroll_basic_salary_records` từng không tồn tại ở database nguồn trong lần kiểm
chứng lịch sử, nên console khi đó không thể sync trực tiếp source -> target như attendance.

Với bảng này, console hỗ trợ command riêng để copy dữ liệu từ kỳ trước ngay trong database đích:

```powershell
dotnet src/Vnta.PostgresSync/Vnta.PostgresSync.Console/bin/Debug/net10.0/Vnta.PostgresSync.Console.dll sync-basic-salary --month 6 --year 2026
```

Rule hiện tại:

- nguồn là `payroll_basic_salary_records` của tháng trước trong target
- đích là `payroll_basic_salary_records` của tháng chỉ định
- nếu nhân viên đã có dòng ở tháng đích và lương khác dữ liệu nguồn, console sẽ update
- nếu chưa có dòng ở tháng đích, console sẽ insert mới
- nếu tháng nguồn không có dữ liệu, command kết thúc thành công với `sourceRows=0`

Kiểm chứng ngày `2026-07-13`:

- chạy thành công command `sync-basic-salary --month 6 --year 2026`
- kết quả runtime: `source=05/2026; target=06/2026; sourceRows=0; created=0; updated=0; unchanged=0`
- kết luận: code sync đã chạy được, nhưng hiện chưa có dữ liệu tháng `05/2026` trong target để làm nguồn copy sang tháng `06/2026`

## Quy tắc cấu hình bảng

Mỗi bảng được mô tả trong `PostgresSync:Tables` với các trường chính:

- `Phase`: xác định bảng thuộc `MasterData` hay `AttendanceDaily`
- `Order`: thứ tự chạy trong cùng pha
- `Name`: bảng nguồn
- `TargetTable`: bảng đích
- `ConflictKeys`: khóa dùng cho `ON CONFLICT`
- `ColumnMappings`: chỉ định cột khi schema hai bên lệch nhau
- `SourceQuery`: query tùy chỉnh thay cho `SELECT *`

## Ghi chú triển khai hiện tại

- Không phải toàn bộ bảng ở database nguồn đều có bảng tương ứng ở database đích.
- Chỉ nên cấu hình sync cho các bảng đã kiểm tra có schema tương thích hoặc đã có `ColumnMappings` rõ ràng.
- Với bảng có ràng buộc khóa ngoại, cần đảm bảo bảng cha đã sync xong trước khi chạy bảng con.
- Các bảng log vận hành như `device_cmd`, `oplog`, `errorlog`, `outbound_attendance_logs`, `outbound_system_logs` chưa được đưa vào pha mặc định.

## Kiểm chứng tối thiểu trước khi chạy thật

1. Chạy `inspect` để xác nhận schema nguồn và đích chưa thay đổi ngoài dự kiến.
2. Chạy `sync-master` trước.
3. Kiểm tra khóa ngoại và số lượng dòng ở database đích.
4. Chỉ chạy `sync-attendance` sau khi master data ổn định.

## Bo sung 2026-07-13: sync source -> target cho `payroll_basic_salary_records` thang 06/2026

Nguồn business được ghi nhận trong lần kiểm chứng lịch sử cho bảng đích
`public.payroll_basic_salary_records` là `public.payroll_monthly_salary_rates` ở database
nguồn khi đó. Cần xác nhận lại schema và quyền truy cập trước khi vận hành trên Jifeng.

Mapping da duoc cau hinh:

- `Luong_can_ban` -> `BasicSalary`
- `So_Ngay_Cong` -> `StandardWorkingDays`
- `Luong_theo_ngay` -> `DailySalary`
- `Luong_Tang_Ca_Theo_Gio` -> `HourlySalary`
- `Thang` -> `PayrollMonth`
- `Nam` -> `PayrollYear`

Pham vi sync hien tai duoc khoa theo yeu cau thang `06/2026` va chi lay du lieu hop le:

- `Nam = 2026`
- `Thang = 6`
- `Luong_can_ban > 0`
- `So_Ngay_Cong > 0`

Lenh chay thuc te:

```powershell
dotnet src/Vnta.PostgresSync/Vnta.PostgresSync.Console/bin/Debug/net10.0/Vnta.PostgresSync.Console.dll sync-master --table public.payroll_monthly_salary_rates
```

Ket qua kiem chung ngay `2026-07-13`:

```text
Completed table sync. Name=public.payroll_monthly_salary_rates; Rows=861.
PostgreSQL sync cycle completed. Tables=1; Rows=861.
```

Luu y:

- Lan chay dau tien that bai do source co dong `Luong_can_ban = 0` va `So_Ngay_Cong = 0`, bi target chan boi constraint `CK_payroll_basic_salary_records_BasicSalary`.
- Sau khi bo sung filter du lieu hop le cho ky `06/2026`, luong sync source -> target da chay thanh cong.

Trang thai ban giao:

- Scope sync `payroll_basic_salary_records` cho thang `06/2026` da hoan tat.
- Ket qua chay that da duoc ghi nhan trong tai lieu nay va trong `doc/implementation-log/20260713.md`.
- Neu can mo rong sang thang khac, uu tien doi filter ky luong trong `SourceQuery` cua mapping `public.payroll_monthly_salary_rates`.
