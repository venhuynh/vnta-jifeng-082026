namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Một trang dữ liệu khấu trừ BHXH-YT cùng tổng số dòng khớp điều kiện lọc.
/// </summary>
public sealed record PayrollInsuranceDeductionPageDto(
    IReadOnlyList<PayrollInsuranceDeductionListItemDto> Rows,
    int TotalCount);
