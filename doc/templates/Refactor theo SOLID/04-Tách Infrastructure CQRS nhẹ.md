# Prompt — Tách Infrastructure CQRS nhẹ

```text
Bạn là senior EF Core/infrastructure engineer. Hãy tách infrastructure của feature dưới đây thành read side và command use cases theo SOLID. Đây là tác vụ IMPLEMENT.

## Đầu vào
- Feature group / name: `KhauTru` / `KhauTruPhiCongDoan` — Khấu trừ phí công đoàn
- Infrastructure root hiện tại: `src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/KhauTru/KhauTruPhiCongDoan`
- Application contracts liên quan: `src/Vnta.HRM2026/Vnta.Hrm.Application/KhauTru/KhauTruPhiCongDoan` (`IPayrollUnionFeeDeductionReadService`, `IPayrollUnionFeeDeductionCommandService` và request/result/DTO).
- Schema/migration được phép thay đổi: Không; bảo toàn bảng/cột/index/constraint hiện có, transaction, optimistic concurrency, lock state và audit.

## Bắt buộc
1. Khảo sát DbContext, entity configuration, indexes, migrations, transaction/concurrency và toàn bộ application contracts.
2. Đưa entity/configuration chỉ thuộc feature vào `Persistence`; giữ entity shared ở vị trí dùng chung nếu có consumer khác.
3. Tách concrete services theo use case: `Queries`, `Commands` (refresh/recalculate, manual adjustment, lock...) và `Policies` khi cần adapter dữ liệu.
4. Read service phải dùng projection/AsNoTracking phù hợp; command service chịu trách nhiệm validation, aggregate update, transaction, audit và concurrency của chính use case đó.
5. Không thay schema hoặc migration lịch sử nếu chưa được phép. Nếu di chuyển entity/configuration, bảo toàn table/column/index/constraint/schema names.
6. Bảo toàn cancellation token, optimistic concurrency, lock semantics, audit actor, exception mapping contract.
7. Đăng ký DI theo feature extension; composition root chỉ gọi extension, không biết concrete service chi tiết.

## Definition of Done
- Không còn service infrastructure ôm đồng thời query, export, policy, refresh, manual edit, lock và persistence.
- No duplicate query/update implementation.
- Build và integration tests feature pass; bổ sung test concurrency/transaction nếu thiếu.
- Báo cáo source cũ→mới, DI map, schema impact (hoặc xác nhận không có).
```
