# Tài liệu kỹ thuật JIFENG HRM

Thư mục `doc/` là điểm vào chính cho tài liệu kỹ thuật, quy tắc làm việc, màn hình, troubleshooting và nhật ký triển khai của repo.

## Source of truth hiện tại

- Source HRM chính: `src/Vnta.HRM2026`
- Solution hiện hành: `src/Vnta.HRM2026/Jifeng.Hrm.slnx`
- Cây menu chuẩn: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs`
- Bản đồ source hiện hành: `doc/project/source-map.md`
- Quy tắc menu và cấu trúc source: `doc/rules/menu-structure-rules.md`
- Tài liệu đồng bộ branch: `doc/project/menu-sync-20260701.md`
- Ghi chú merge cho branch khác: `doc/project/branch-merge-notes-20260701.md`
- Prompt Codex để đồng bộ branch khác: `doc/project/codex-branch-sync-prompt-20260701.md`

## Ghi chú về tài liệu lịch sử

- Các tài liệu trong `doc/implementation-log/`, `doc/sprints/` và một phần `doc/troubleshooting/` có thể còn giữ tên file, namespace hoặc source path ở thời điểm issue/feature được ghi nhận.
- Sprint lịch sử hiện đã được gom vào `doc/sprints/_OLD/`; sprint mới sẽ đi theo cấu trúc nhóm nghiệp vụ `doc/sprints/<nhom>/sprint-###-slug/`.
- Từ baseline ngày `2026-07-01`, source of truth cho menu, folder và namespace là:
  - `VntaNavMenuCatalog.cs`
  - `doc/project/source-map.md`
  - `doc/project/menu-sync-20260701.md`
- Nếu thấy tài liệu lịch sử nhắc tới `AttendanceDevices`, `AttendanceLogs`, `AdmsMonitor`, `SalesReport` hoặc các root cũ như `Components/Attendance`, `Components/Implementation`, hãy hiểu đó là tên/path lịch sử trước baseline refactor menu.

## Điểm vào nhanh

- Quy tắc: `doc/rules/index.md`
- Source map: `doc/project/source-map.md`
- Đánh giá và refactor bảo mật backend: `doc/project/security/README.md`
- Hướng dẫn Ubuntu image-only: `doc/setup/ubuntu-docker-deployment.md`
- Deploy Ubuntu tự động từ PowerShell: `doc/setup/automated-ubuntu-release.md`
- Màn hình theo menu: `doc/screens/README.md`
- Troubleshooting: `doc/troubleshooting/index.md`
- Implementation log: `doc/implementation-log/index.md`
- Sprints theo nhóm: `doc/sprints/README.md`

## Kỳ vọng khi cập nhật tài liệu

- Tài liệu “đang sống” phải phản ánh đúng menu và source path hiện tại.
- Tài liệu lịch sử không cần viết lại toàn bộ, nhưng phải để người đọc phân biệt rõ đâu là ngữ cảnh cũ và đâu là baseline hiện hành.
- Khi đổi menu, folder component, namespace hoặc route, phải rà thêm:
  - `doc/project/source-map.md`
  - `doc/screens/`
  - `doc/rules/menu-structure-rules.md`
  - `doc/project/menu-sync-20260701.md`


