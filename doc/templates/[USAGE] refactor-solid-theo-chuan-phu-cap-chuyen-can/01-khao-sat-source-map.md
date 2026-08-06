# 01 - Khảo sát source map xuyên tầng

## Đầu vào

Dán Feature Refactor Manifest đã điền ở bước 00. Nếu đã có artifact trước đó, cung cấp đường dẫn hoặc nội dung của artifact đó.

## Prompt

Hãy khảo sát read-only feature {{feature.display_name}} theo manifest. Đọc AGENTS.md và kiểm tra git status --short --branch trước. Không sửa source, config, migration, tài liệu hoặc tạo commit.

Lấy Phụ cấp chuyên cần tại {{reference_standard.client_root}} làm chuẩn về boundary, không sao chép business semantics. Dùng rg trước để tìm consumer, DI, route, DTO và test; chỉ đọc tài liệu/source trực tiếp liên quan feature thay vì quét toàn repository không có mục đích.

Lập bản đồ có bằng chứng path:line cho từng use case. Phải lần theo đủ chiều sau nếu tồn tại:

    UI page/section/dialog/action
      -> UI state/coordinator
      -> DataProvider/client adapter
      -> HTTP service/route/verb
      -> endpoint authorization/audit/error mapping
      -> Application contract/request/query/policy
      -> Infrastructure implementation/query projection/command
      -> entity/configuration/external source
      -> audit policy and tests

Với từng use case, ghi bảng gồm:

| Use case | UI entry/action | Provider/transport | Endpoint | Application contract | Infrastructure owner | Data read/write | Authorization/audit | Test hiện có | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

Ngoài bảng use case, phải khảo sát và báo cáo:

- page owner, route, menu, render mode và policy authorize;
- toàn bộ UI action, popup, state async, loading/error/empty/selection/export lifecycle;
- baseline Comment Map: public/cross-layer boundary và logic non-obvious, comment/XML docs hiện có, phần thiếu/sai/generic hoặc intentionally N/A; không đánh giá theo số lượng comment;
- consumer của DTO/route/interface đang có, đặc biệt contract public hoặc legacy;
- persistence entities, aggregate relationship, projection cross-feature, các raw/bulk write;
- nơi đang sở hữu rule, period/config, validation, lock, concurrency token, actor/tenant và audit policy;
- DI registrations ở client, web và infrastructure;
- baseline build/test có thể chạy và baseline failure đã tồn tại ngoài phạm vi.

Không coi một tên giống nhau là bằng chứng ownership. Hãy tìm writer thực tế và consumer thực tế. Nếu không chứng minh được, ghi UNKNOWN cùng lệnh/phạm vi discovery tiếp theo.

Kết thúc bằng:

1. Source map hoàn chỉnh.
2. Danh sách path/file thuộc scope dự kiến, chia theo UI, Client, Web, Application, Infrastructure, test và cross-cutting.
3. Danh sách contract/consumer cần bảo toàn.
4. Mâu thuẫn hoặc missing information cần đưa sang bước 02/03.
5. Xác nhận không có thay đổi file/git.
6. Comment Map baseline để bước 15 dùng lại.
