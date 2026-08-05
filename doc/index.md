# Tài Liệu Dự Án Vnta HRM Blazor

Đây là điểm vào chính cho tài liệu dự án.

## Hiện trạng source chính

- Source HRM hiện hành của repo là `src/Vnta.HRM2026`.
- Solution HRM hiện hành là `src/Vnta.HRM2026/Vnta.Hrm.slnx`.
- Baseline công nghệ chính thức của source HRM hiện hành là `.NET 10` và `DevExpress 26.1.x` (đang pin package `26.1.3`).
- Thư mục `src/Vnta.HRM` đã bị loại khỏi repo.
- Các tài liệu sprint, implementation log và troubleshooting cũ có thể còn tham chiếu `src/Vnta.HRM`; xem chúng như tư liệu lịch sử, không phải đường dẫn source đang hoạt động.

## Nhóm tài liệu

- [`project/`](./project/overview.md): tổng quan, kiến trúc và bản đồ source hiện tại.
- [`project/target-solution-structure.md`](./project/target-solution-structure.md): cấu trúc solution đích chính thức theo ba giai đoạn phát triển.
- [`project/refactor-roadmap.md`](./project/refactor-roadmap.md): roadmap refactor `Vnta.HRM2026` thành các project thành phần và thứ tự triển khai khuyến nghị.
- [`project/refactor-standard-document-plan.md`](./project/refactor-standard-document-plan.md): kế hoạch tạo tài liệu chuẩn làm kim chỉ nam cho các nhánh refactor tiếp theo.
- [`project/hrm-refactor-standard.md`](./project/hrm-refactor-standard.md): tài liệu chuẩn cấp dự án cho boundary UI, endpoint, service, database và playbook refactor từng màn.
- [`project/feature-folder-standard.md`](./project/feature-folder-standard.md): chuẩn tổ chức folder và đặt tên file theo `context key` tiếng Việt không dấu xuyên suốt từ UI đến backend, bám theo SOLID.
- [`project/infrastructure-feature-folder-map.md`](./project/infrastructure-feature-folder-map.md): map folder implementation Infrastructure theo cùng nhóm nghiệp vụ và context key của UI.
- [`project/cross-project-feature-folder-refactor-plan.md`](./project/cross-project-feature-folder-refactor-plan.md): kế hoạch chuẩn hóa folder feature xuyên Web.Client, Web, Application và Domain.
- [`project/refactor-gap-register.md`](./project/refactor-gap-register.md): sổ đăng ký các gap hiện trạng so với chuẩn refactor, dùng để ưu tiên hóa sprint và backlog kiến trúc.
- [`project/hrm-list-screen-blueprint.md`](./project/hrm-list-screen-blueprint.md): blueprint chuẩn cho các màn danh sách HRM dùng toolbar, grid hoặc tree list và popup form.
- [`project/ui-solid-rollout-plan.md`](./project/ui-solid-rollout-plan.md): checklist SOLID và backlog rollout cho toàn bộ UI, lấy `KhauTruKhac` làm mẫu tham chiếu.
- [`project/attendance-device-management-screen.md`](./project/attendance-device-management-screen.md): phân tích mô hình máy chấm công ZKTeco và thiết kế màn quản lý Máy chấm công cho HRM.
- [`screens/README.md`](./screens/README.md): cây tài liệu kỹ thuật bám theo `NavMenu`, mỗi màn hình có một file mô tả riêng để điền dần trong quá trình phát triển.
- [`setup/`](./setup/local-development.md): hướng dẫn chuẩn bị môi trường local và PostgreSQL.
- [`setup/ubuntu-docker-deployment.md`](./setup/ubuntu-docker-deployment.md): hướng dẫn triển khai Ubuntu theo hướng image-only, không đưa source code lên server.
- [`setup/ubuntu-test-pfx-and-runtime-env.md`](./setup/ubuntu-test-pfx-and-runtime-env.md): tạo PFX self-signed cho môi trường test, upload certificate và hoàn tất runtime env Ubuntu.
- [`setup/device-activation-code.md`](./setup/device-activation-code.md): hướng dẫn lấy mã kích hoạt thiết bị theo đúng logic dùng chung giữa `HRM` và `adms-gateway`.
- [`domain/`](./domain/hrm-glossary.md): thuật ngữ nghiệp vụ HRM.
- [`knowledgeBase/`](./knowledgeBase/index.md): kiến thức Blazor, DevExpress và ví dụ thực tế rút ra từ source hiện tại.
- [`troubleshooting/`](./troubleshooting/index.md): playbook xử lý sự cố thực tế, gồm cả NavMenu/DxTreeView khi branch bị lệch menu hoặc render bất ổn.
- [`checklists/`](./checklists/done-checklist.md): checklist hoàn tất trước khi kết thúc lượt triển khai.
- [`checklists/screen-implementation-principles.md`](./checklists/screen-implementation-principles.md): checklist gốc về nguyên tắc triển khai màn hình mới, đặc biệt cho boundary UI, service và database.
- [`checklists/ui-screen-checklist.md`](./checklists/ui-screen-checklist.md): checklist ngắn để tạo mới màn hình UI theo chuẩn layout, CSS, grid và state của repo.
- [`mcp-servers.md`](./mcp-servers.md): cấu hình MCP chính thức cho DevExpress, Copilot instructions và cách dùng trong Visual Studio Agent mode.
- [`implementation-log/`](./implementation-log/index.md): nhật ký triển khai theo ngày và branch, mỗi file mới dùng format `yyyyMMdd-<ten-nhanh-da-chuan-hoa>.md`.
- [`sprints/`](./sprints/index.md): nơi lưu tài liệu sprint theo nhóm nghiệp vụ; sprint lịch sử đã được archive trong `sprints/_OLD/`.
- [`rules/`](./rules/index.md): quy tắc code, AI Vibe Coding, Blazor DevExpress, HRM và kiểm chứng.
- [`templates/`](./templates/feature-spec-template.md): mẫu đặc tả chức năng, mẫu triển khai màn hình mới và ADR.
- [`agent-skills/`](./agent-skills/index.md): tài liệu tham khảo về skill và quy tắc vận hành skill.

## Quy tắc ưu tiên

Khi có xung đột, ưu tiên theo thứ tự:

1. Yêu cầu trực tiếp mới nhất của người dùng.
2. Quy tắc trong [`rules/`](./rules/index.md).
3. Quy ước hiện có trong source code.
4. Tài liệu tham khảo và MCP.

## Ghi chú hiện trạng

Source hiện hành `Vnta.HRM2026` vẫn còn nhiều text mẫu tiếng Anh và naming CRM từ template DevExpress/Identity. Khi phát triển nghiệp vụ HRM, cần Việt hóa caption và đổi naming dần theo `rules/blazor-devexpress-rules.md`.

## Checklist bắt buộc cho AI

Trước khi kết thúc một lượt code hoặc sửa tài liệu, đọc [`checklists/done-checklist.md`](./checklists/done-checklist.md).

## Prompt tái sử dụng cho AI agent

- [`project/codex-branch-sync-prompt-20260701.md`](./project/codex-branch-sync-prompt-20260701.md): prompt đồng bộ baseline menu/source từ `main`.
- [`project/codex-navmenu-fix-prompt-20260701.md`](./project/codex-navmenu-fix-prompt-20260701.md): prompt chuyên đề để các branch khác sửa nhóm lỗi NavMenu/DxTreeView/icon/current module.
- [`templates/prompt-refactor-cau-truc-folder-xuyen-project.txt`](./templates/prompt-refactor-cau-truc-folder-xuyen-project.txt): prompt refactor một context theo folder chuẩn xuyên các project.
