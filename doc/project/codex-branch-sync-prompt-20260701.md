# Prompt đồng bộ branch bằng Codex ngày 2026-07-01

Đây là prompt sẵn để dán vào Codex AI trên các máy đang ở branch khác, nhằm lấy menu chuẩn, source chuẩn và tài liệu chuẩn từ `main`.

## Source of truth

- Branch chuẩn: `main`
- Commit chuẩn: `25b00b8`
- Ngày merge: `2026-07-01`
- Merge PR: `#10`

## Cập nhật sau sprint 011

- Phần icon Bootstrap trong prompt lịch sử này đã được thay bằng DevExpress Icon Library.
- Khi đồng bộ branch sau `2026-07-02`, ưu tiên `IconUrl` và `VntaDevExpressIcons` theo `doc/rules/devexpress-icon-rules.md`.
- Khi mở nhánh mới từ `2026-07-13`, bắt buộc `git fetch origin`, `git switch main`, `git pull --ff-only origin main`, rồi mới `git switch -c <ten-nhanh-moi>` để nhánh mới có đầy đủ dữ liệu từ `main`.

## Prompt sẵn để dán

```text
Bạn đang làm việc trong repo Vnta-Blazor-2026 trên một feature branch, không phải main.

Source of truth cho menu, source UI và tài liệu chuẩn hiện nằm trên `origin/main`, đã được merge ngày 2026-07-01 qua PR #10, commit `25b00b8`.

Yêu cầu:
1. Nếu cần mở nhánh mới cho lượt làm việc này, bắt buộc đồng bộ đủ `main` trước bằng:
   - `git fetch origin`
   - `git switch main`
   - `git pull --ff-only origin main`
   - sau đó mới `git switch -c <ten-nhanh-moi>`
2. Đồng bộ branch hiện tại theo menu chuẩn và tài liệu chuẩn từ `origin/main`.
3. Bảo toàn logic/feature đang làm ở branch hiện tại nếu không xung đột với baseline mới.
4. Nếu có xung đột do rename/move file, ưu tiên giữ cấu trúc mới từ `main`, sau đó chuyển logic đang làm dở của branch vào folder mới đúng theo menu.
5. Bắt buộc lấy đầy đủ DevExpress icon từ menu chuẩn hiện hành. Nếu branch hiện tại đang thiếu icon, sai icon hoặc còn Bootstrap icon, phải sửa để khớp 100% với `VntaNavMenuCatalog.cs` và `VntaDevExpressIcons`.

Bắt buộc đọc và đối chiếu các file sau trên `origin/main`:
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenu.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenu.razor.css`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/Shared/NavMenuTreeNode.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/MainLayout.razor`
- `doc/rules/menu-structure-rules.md`
- `doc/project/source-map.md`
- `doc/project/menu-sync-20260701.md`
- `doc/project/branch-merge-notes-20260701.md`
- `doc/screens/README.md`

Cây menu chuẩn phải khớp như sau:
- UI DEMO
  - Contacts
    - Contact List
    - Contact Details
  - Planning
    - Task List
    - Scheduler
  - Analytics
    - Dashboard
    - Sales Analysis
- Đang triển khai
- Tổng quan
- Nhân sự
  - Phòng ban
  - Chức vụ
  - Nhân viên
    - Danh sách
    - Chi tiết nhân viên
- Ca kíp
  - Lịch làm việc
  - Cài đặt xếp ca
  - Bảng xếp ca
  - Cài đặt ca
- Chấm công
  - Bảng công ngày
  - Code kết quả tính công
  - Dữ liệu thô
- Phụ cấp
  - Phụ cấp trách nhiệm
  - Phụ cấp thâm niên
  - Phụ cấp chuyên cần
  - Phụ cấp cơm
  - Phụ cấp độc hại
- Tính lương
  - Lương căn bản
- Quản trị
  - Sinh trắc học
  - Máy chấm công
  - Giám sát ADMS
  - Lệnh máy chấm công

DevExpress icon bắt buộc phải khớp với source hiện hành:
- UI DEMO -> `VntaDevExpressIcons.UiDemo`
- Contacts -> `VntaDevExpressIcons.Contacts`
- Contact List -> `VntaDevExpressIcons.Employee`
- Contact Details -> `VntaDevExpressIcons.ContactCard`
- Planning -> `VntaDevExpressIcons.Planning`
- Task List -> `VntaDevExpressIcons.TaskList`
- Scheduler -> `VntaDevExpressIcons.Scheduler`
- Analytics -> `VntaDevExpressIcons.Analytics`
- Dashboard -> `VntaDevExpressIcons.Gauge`
- Sales Analysis -> `VntaDevExpressIcons.Trend`
- Đang triển khai -> `VntaDevExpressIcons.Implementation`
- Tổng quan -> `VntaDevExpressIcons.Gauge`
- Nhân sự -> `VntaDevExpressIcons.Hr`
- Phòng ban -> `VntaDevExpressIcons.Organization`
- Chức vụ -> `VntaDevExpressIcons.Organization`
- Nhân viên -> `VntaDevExpressIcons.Employee`
- Chi tiết nhân viên -> `VntaDevExpressIcons.ContactCard`
- Ca kíp -> `VntaDevExpressIcons.ShiftManagement`
- Lịch làm việc -> `VntaDevExpressIcons.WorkCalendar`
- Cài đặt xếp ca -> `VntaDevExpressIcons.ShiftSettings`
- Bảng xếp ca -> `VntaDevExpressIcons.Scheduler`
- Cài đặt ca -> `VntaDevExpressIcons.ShiftSettings`
- Chấm công -> `VntaDevExpressIcons.Attendance`
- Bảng công ngày -> `VntaDevExpressIcons.Attendance`
- Code kết quả tính công -> `VntaDevExpressIcons.ResultCodes`
- Dữ liệu thô -> `VntaDevExpressIcons.Database`
- Phụ cấp -> `VntaDevExpressIcons.Money`
- Phụ cấp trách nhiệm -> `VntaDevExpressIcons.Money`
- Phụ cấp thâm niên -> `VntaDevExpressIcons.Money`
- Phụ cấp chuyên cần -> `VntaDevExpressIcons.Attendance`
- Phụ cấp cơm -> `VntaDevExpressIcons.Money`
- Phụ cấp độc hại -> `VntaDevExpressIcons.Money`
- Tính lương -> `VntaDevExpressIcons.Money`
- Lương căn bản -> `VntaDevExpressIcons.Money`
- Quản trị -> `VntaDevExpressIcons.Settings`
- Sinh trắc học -> `VntaDevExpressIcons.Database`
- Máy chấm công -> `VntaDevExpressIcons.Device`
- Giám sát ADMS -> `VntaDevExpressIcons.AdmsMonitor`
- Lệnh máy chấm công -> `VntaDevExpressIcons.Command`

Cấu trúc source folder chuẩn phải khớp:
- `Components/UiDemo/...`
- `Components/DangTrienKhai/...`
- `Components/TongQuan/...`
- `Components/NhanSu/...`
- `Components/ChamCong/DuLieuTho/...`
- `Components/TinhLuong/...`
- `Components/QuanTri/MayChamCong/...`
- `Components/QuanTri/GiamSatAdms/...`
- `Components/QuanTri/LenhMayChamCong/...`

Không được tạo hoặc giữ source song song trong các root legacy sau:
- `Components/Contacts/`
- `Components/Planning/`
- `Components/Analytics/`
- `Components/Attendance/`
- `Components/Implementation/`

Route hiện hành phải được giữ nguyên trong đợt đồng bộ này:
- `/attendance/logs`
- `/attendance/devices`
- `/Adms`
- `/adms/device-commands`
- `/ContactList`
- `/ContactDetails`
- `/TaskList`
- `/Scheduler`
- `/Dashboard`
- `/SalesAnalysis`

Cách làm:
1. Nếu đang mở nhánh mới, chạy:
   - `git fetch origin`
   - `git switch main`
   - `git pull --ff-only origin main`
   - `git switch -c <ten-nhanh-moi>`
2. `git fetch origin`
3. Kiểm tra diff giữa branch hiện tại và `origin/main`
4. Ưu tiên `git merge origin/main`
5. Nếu branch tạm thời chưa thể merge full `main`, vẫn phải sync thủ công các file menu và tài liệu nêu trên từ `origin/main`
6. Di dời source branch đang sửa vào folder mới đúng theo menu
7. Cập nhật namespace, component name và đọc link theo folder mới
8. Kiểm tra lại DevExpress icon để đảm bảo không bỏ sót node nào hoặc còn Bootstrap icon
9. Build `src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj`
10. Smoke test các route mà branch đang chạm vào

Acceptance criteria:
- Nhánh mới, nếu có tạo trong lượt này, đã được cắt ra từ local `main` vừa đồng bộ đầy đủ với `origin/main`
- Menu và tài liệu branch hiện tại khớp source of truth trên `main`
- DevExpress icon đã được lấy đầy đủ từ source hiện hành, không còn node nào dùng Bootstrap icon, icon placeholder hoặc thiếu icon
- Không còn source mới nằm trong root legacy
- Namespace và folder khớp với menu mới
- Build thành công, hoặc nếu build bị khóa bởi process đang chạy thì báo cáo rõ process nào đang lock file

Sau khi xong:
- Cho tôi biết những file đã đồng bộ
- Nếu có conflict, giải thích branch logic nào đã được reapply vào cấu trúc mới
- Xác nhận rõ ràng đã đồng bộ đầy đủ DevExpress icon từ source hiện hành
```



