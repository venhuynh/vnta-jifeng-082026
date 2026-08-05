# Ghi chú merge baseline menu ngày 2026-07-01

Tài liệu này là phần mô tả/nghi chú merge ngắn gọn để dùng khi review PR và khi hướng dẫn các branch khác đồng bộ theo menu chuẩn trên `main`.

## Baseline cần đồng bộ

- Branch chuẩn: `main`
- Baseline merge commit: `25b00b8`
- Ngày merge: `2026-07-01`
- Merge PR: `#10`
- Branch lịch sử đã đưa baseline vào `main`: `codex/refactor-main-navmenu`

## Phạm vi thay đổi mà branch khác phải tuân thủ

- Chốt cây menu chuẩn mới với root `UI DEMO`, `Đang triển khai`, `Tổng quan`, `Nhân sự`, `Ca kíp`, `Chấm công`, `Phụ cấp`, `Tính lương`, `Quản trị`.
- Di dời source UI để folder bám theo menu thật:
  - `Components/UiDemo/...`
  - `Components/ChamCong/...`
  - `Components/QuanTri/...`
- Đổi tên file, component và namespace theo folder mới.
- Giữ nguyên route hiện hành trong baseline này để tránh gãy link đang dùng.
- Bắt buộc lấy đầy đủ DevExpress icon theo `VntaNavMenuCatalog.cs` và `VntaDevExpressIcons`.
- Cập nhật tài liệu sống để phản ánh đúng source of truth mới.

## Quy tắc merge cho các branch khác

1. Khi tạo nhánh mới, bắt buộc đồng bộ đủ `main` trước:
   - `git fetch origin`
   - `git switch main`
   - `git pull --ff-only origin main`
   - sau đó mới `git switch -c <ten-nhanh-moi>`
2. Không tạo nhánh mới từ `main` local đã cũ, từ working tree chưa sync đủ với `origin/main`, hoặc từ feature branch khác nếu chưa có chỉ định rõ.
3. Ưu tiên `merge origin/main`.
4. Nếu chưa thể merge full `main`, branch vẫn phải sync thủ công các file menu và tài liệu chuẩn từ `main`.
5. Khi conflict do rename hoặc move file, giữ cấu trúc folder mới rồi chuyển logic đang làm dở của branch vào đúng folder mới.
6. Không tạo source mới vào các root cũ `Components/Contacts`, `Components/Planning`, `Components/Analytics`, `Components/Attendance`, `Components/Implementation`.
7. Menu mới chưa chốt IA phải đặt mặc định dưới `Đang triển khai`.
8. Không chấp nhận branch chỉ lấy một phần DevExpress icon. Nếu menu của branch thiếu icon, còn Bootstrap icon hoặc icon không đúng ngữ cảnh, phải lấy đúng icon từ source hiện hành.

## Checklist review cho branch đồng bộ

- [ ] Nếu branch được tạo mới trong lượt làm việc này, nhánh đã được mở từ local `main` vừa `pull --ff-only` theo `origin/main`.
- [ ] Branch đã nhập baseline từ `main`.
- [ ] Source UI không còn thêm mới vào root cũ.
- [ ] Namespace, component name và file name đã khớp folder mới.
- [ ] Route đang chạy không bị đổi ngoài chủ đích.
- [ ] Tài liệu của branch không còn trỏ nhầm sang path đang hoạt động cũ.
- [ ] DevExpress icon của menu đã khớp với `VntaNavMenuCatalog.cs` và `VntaDevExpressIcons`.
- [ ] Build sạch: `dotnet build src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj`

## Mẫu ghi chú để dán vào review PR

```text
PR này cần được review theo baseline menu/UI structure đã chốt trên `main` sau PR #10 ngày 2026-07-01.

Phạm vi baseline:
- Chuẩn hóa cây menu mới.
- Di dời source UI theo folder bám đúng menu.
- Đổi namespace, component/file name theo folder mới.
- Giữ nguyên route hiện hành để tránh gãy link.
- Bắt buộc đồng bộ đầy đủ DevExpress icon theo `VntaNavMenuCatalog.cs` và `VntaDevExpressIcons`.
- Cập nhật tài liệu sống làm source of truth cho các branch khác.

Nguồn chuẩn cần đối chiếu:
- doc/rules/menu-structure-rules.md
- doc/project/source-map.md
- doc/project/menu-sync-20260701.md
- doc/project/branch-merge-notes-20260701.md
- doc/project/codex-branch-sync-prompt-20260701.md
- src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Navigation/VntaNavMenuCatalog.cs

Baseline:
- branch: main
- commit: 25b00b8
- date: 2026-07-01
- PR: #10

Kỳ vọng với các branch khác:
1. Nếu cần mở nhánh mới, phải `fetch origin`, chuyển về `main`, `pull --ff-only origin main`, rồi mới `switch -c`.
2. Merge `main` hoặc sync thủ công theo tài liệu chuẩn.
3. Chuyển toàn bộ phần source đang sửa sang folder mới đúng theo menu.
4. Không tạo source mới vào các root cũ.
5. Menu mới chưa chốt IA phải đặt dưới Đang triển khai.
6. Lấy đủ DevExpress icon theo source hiện hành, không được bỏ sót icon.
7. Build sạch trước khi tiếp tục feature work.
```


