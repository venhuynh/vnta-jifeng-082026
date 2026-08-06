# Prompt — Comment Infrastructure và Database

```text
BẠN LÀ SENIOR EF CORE/INFRASTRUCTURE ENGINEER. Đây là tác vụ IMPLEMENT COMMENT, tuyệt đối không thay đổi schema, migration hoặc runtime behavior.

## Đầu vào
- Feature/name: [điền]
- Infrastructure root/DbContext: [điền]
- Comment map: [dán hoặc tự khảo sát]

## Bắt buộc
1. Comment entity, configuration, table/column mapping, index, constraint, relationship và migration compatibility.
2. Comment query projection, filter/paging, `AsNoTracking`, transaction, command update, audit, lock và optimistic concurrency.
3. Giải thích vì sao adapter/service phụ thuộc application contract và nguồn dữ liệu nào được đọc/ghi.
4. Không sửa migration lịch sử, SQL, schema, seed hoặc dữ liệu production.
5. File generated/vendor không sửa trực tiếp; ghi nhận trong báo cáo.
6. Dùng path:line chính xác và đánh dấu `Chưa xác minh` thay vì đoán.

## Kiểm tra và báo cáo
- Build Infrastructure và test persistence/integration liên quan.
- Xác nhận schema/migration/runtime không thay đổi.
- Bảng `file:line | database object/operation | mục đích | transaction/concurrency impact`.
```
