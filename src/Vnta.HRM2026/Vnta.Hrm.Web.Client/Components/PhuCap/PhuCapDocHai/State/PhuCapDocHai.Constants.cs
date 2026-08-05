using System.Globalization;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Constants shared by the hazard allowance screen state.</summary>
public partial class PhuCapDocHai
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly IReadOnlyList<PhuCapDocHaiMonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new PhuCapDocHaiMonthOption(month, $"Tháng {month:00}"))
            .ToArray();

    private const int AllPageSize = 5000;
    private static readonly IReadOnlyList<PageSizeOption> PageSizeOptions =
    [
        new(20, "20"),
        new(50, "50"),
        new(100, "100"),
        new(AllPageSize, "Tất cả")
    ];

    private const string SummaryAllKey = "all";
    private const string SummaryEligibleKey = "eligible";
    private const string SummaryExceptionKey = "exception";
    private const string SummaryLockedKey = "locked";
    private const string SummaryOpenKey = "open";
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";
    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const string DefaultLoadingText = "Đang tải dữ liệu phụ cấp độc hại...";
    private const string HazardAllowanceAmountTotalSummaryName = "HazardAllowanceAmountTotal";
}
