namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>Số dòng tổng kết theo trạng thái khóa trong cùng phạm vi kỳ lương và tìm kiếm.</summary>
public sealed record PayrollDeductionSummaryLockStatusCountsDto(
    int All,
    int Open,
    int Locked)
{
    public static PayrollDeductionSummaryLockStatusCountsDto Empty { get; } = new(0, 0, 0);
}
