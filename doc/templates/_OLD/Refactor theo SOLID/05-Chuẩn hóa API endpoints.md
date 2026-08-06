# Prompt — Chuẩn hóa API endpoints

```text
Bạn là ASP.NET Core API architect. Hãy chuẩn hóa HTTP boundary cho feature dưới đây theo SOLID, giữ tương thích API hiện có. Đây là tác vụ IMPLEMENT.

## Đầu vào
- Feature group / name: `PhuCap` / `PhuCapTrachNhiemCapBac` — Cấp bậc phụ cấp trách nhiệm
- API root: `/api/payroll/responsibility-allowance/grade-config`
- Authorization policy: `InternalAccountPolicies.PayrollAdministration` (áp dụng ở nhóm `/api/payroll` và page).
- API endpoints/contracts client đang gọi và cần đối chiếu/giữ tương thích: `GET /grade-config?year={year}&month={month}` và `POST /grade-config/grades`; giữ request/result/DTO, status 400/409, actor-audit và correlation hiện có.
- API breaking changes được phép: Không.

## Bắt buộc
1. Di chuyển endpoint source về `Endpoints/[Feature group]/[Feature name]`.
2. Tách mapping, query endpoints và command endpoints; một handler phụ thuộc narrow application capability tương ứng.
3. Giữ URL, HTTP verb, route parameters, JSON field names/status code/response shape. Không tạo endpoint mới hoặc đổi version nếu chưa được cho phép.
4. Chuẩn hóa null body, validation, `CancellationToken`, 400/401/403/404/409/500 theo convention hiện hữu.
5. Với command audit, lấy actor/correlation từ authenticated principal/server context; không tin actor client gửi lên.
6. Authorization phải được áp dụng ở API boundary, không chỉ ở Razor page.
7. Bổ sung endpoint contract tests: auth, actor spoofing, validation và conflict.

## Definition of Done
- Endpoint folder rõ query/command/mapping; route registry gọi mapping feature.
- Không endpoint nào inject concrete infrastructure service.
- API contract tests và build pass.
- Báo cáo endpoint cũ→mới, contract giữ nguyên và test kết quả.
```
