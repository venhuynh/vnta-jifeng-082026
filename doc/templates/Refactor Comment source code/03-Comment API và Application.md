# Prompt — Comment API và Application

```text
BẠN LÀ SENIOR ASP.NET CORE APPLICATION ARCHITECT. Đây là tác vụ IMPLEMENT COMMENT, không đổi API contract hay business logic.

## Đầu vào
- Feature/name: [điền]
- API root: [điền]
- Comment map: [dán hoặc tự khảo sát]

## Bắt buộc
1. Comment endpoint mapping, route, HTTP verb, authorization, request/response, validation và status-code mapping.
2. Comment application contract, command/query, handler/use case, policy và exception: mục đích, invariant, input/output, side effect và cancellation.
3. Dùng XML docs cho public interface/DTO/record/method; dùng `<see cref>`/`<paramref>` khi phù hợp.
4. Giải thích actor/correlation lấy từ server context, audit, idempotency và concurrency nếu có.
5. Ghi rõ mapping giữa UI DTO, API DTO và application model.
6. Không ghi secret, dữ liệu thật hoặc giả định nghiệp vụ chưa được xác minh.

## Kiểm tra và báo cáo
- Build Web/Application và test endpoint/contract liên quan.
- Bảng endpoint: `file:line | method/URL | request | contract | response/error`.
- Bảng use case và business rule đã được giải thích.
```
