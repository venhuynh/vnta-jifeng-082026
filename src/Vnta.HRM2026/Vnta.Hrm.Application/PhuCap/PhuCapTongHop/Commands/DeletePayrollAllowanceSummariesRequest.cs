namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Xóa snapshot theo phiên bản người dùng đang thấy, tránh xóa âm thầm một
/// dòng đã được phiên khác thay đổi sau khi grid được tải.
/// </summary>
public sealed record DeletePayrollAllowanceSummariesRequest(
    IReadOnlyList<PayrollAllowanceSummaryDeleteItem> Items);

/// <summary>Định danh một snapshot cần xóa cùng phiên bản mà client đã tải.</summary>
public sealed record PayrollAllowanceSummaryDeleteItem(
    Guid Id,
    DateTime? OriginalUpdatedAtUtc);
