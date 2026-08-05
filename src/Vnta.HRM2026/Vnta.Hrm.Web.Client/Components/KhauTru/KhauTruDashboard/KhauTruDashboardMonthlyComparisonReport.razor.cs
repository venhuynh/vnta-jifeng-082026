using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruDashboard;

public partial class KhauTruDashboardMonthlyComparisonReport
{
    private const string AmountFieldPrefix = "AmountMonth";
    private const string ChangeFieldPrefix = "ChangeMonth";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter, EditorRequired] public IReadOnlyList<PayrollDeductionDashboardDeductionComparisonDto> Comparison { get; set; } = [];
    [Parameter, EditorRequired] public int PayrollMonth { get; set; }
    [Parameter, EditorRequired] public int PayrollYear { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public EventCallback RefreshRequested { get; set; }

    private int ComparisonEndMonth => Comparison
        .SelectMany(row => row.Months)
        .Select(month => month.PayrollMonth)
        .DefaultIfEmpty(PayrollMonth)
        .Max();

    private IReadOnlyList<ComparisonMonth> ComparisonMonths => Enumerable.Range(1, ComparisonEndMonth)
        .Select(month => new ComparisonMonth(month, $"Tháng {month:00}", $"deduction-month-{month:00}", GetMonthTotal(month)))
        .ToArray();

    private string ComparisonGridHostStyle =>
        $"--dashboard-comparison-grid-min-width: {14 + ComparisonMonths.Count * 20}rem;";

    private decimal GetMonthTotal(int month) => Comparison.Sum(row => GetAmount(row, month));

    private static decimal GetAmount(PayrollDeductionDashboardDeductionComparisonDto row, int month) =>
        row.Months.FirstOrDefault(item => item.PayrollMonth == month)?.Amount ?? 0m;

    private static decimal GetChange(PayrollDeductionDashboardDeductionComparisonDto row, int month) =>
        GetAmount(row, month) - (month > 1 ? GetAmount(row, month - 1) : 0m);

    private static string GetAmountFieldName(int month) => $"{AmountFieldPrefix}{month:00}";

    private static string GetChangeFieldName(int month) => $"{ChangeFieldPrefix}{month:00}";

    private static void GetMonthlyComparisonUnboundColumnData(GridUnboundColumnDataEventArgs args)
    {
        if(args.DataItem is not PayrollDeductionDashboardDeductionComparisonDto row) return;

        args.Value = GetMonthlyColumnValue(row, args.FieldName);
    }

    private static decimal? GetMonthlyColumnValue(PayrollDeductionDashboardDeductionComparisonDto row, string? fieldName)
    {
        if(TryGetMonth(fieldName, AmountFieldPrefix, out var amountMonth))
            return GetAmount(row, amountMonth);

        if(TryGetMonth(fieldName, ChangeFieldPrefix, out var changeMonth))
            return GetChange(row, changeMonth);

        return null;
    }

    private static bool TryGetMonth(string? fieldName, string prefix, out int month)
    {
        month = 0;

        return !string.IsNullOrWhiteSpace(fieldName)
            && fieldName.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(fieldName[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out month);
    }

    private static string FormatMoney(decimal amount) => amount.ToString("N0", DisplayCulture) + " đ";

    private static string FormatChange(decimal amount) => amount switch
    {
        > 0m => "+" + FormatMoney(amount),
        < 0m => "-" + FormatMoney(Math.Abs(amount)),
        _ => "--"
    };

    private static string GetChangeCssClass(decimal amount) => amount switch
    {
        > 0m => "comparison-change comparison-change-positive",
        < 0m => "comparison-change comparison-change-negative",
        _ => "comparison-change"
    };

    private sealed record ComparisonMonth(int Value, string Caption, string BandName, decimal TotalAmount);
}
