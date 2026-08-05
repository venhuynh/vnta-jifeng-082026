using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDashboard;

public partial class PhuCapDashboardDepartmentTreeList
{
    private const string AmountFieldPrefix = "AmountMonth";
    private const string ChangeFieldPrefix = "ChangeMonth";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter, EditorRequired] public IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto> Comparison { get; set; } = [];
    [Parameter, EditorRequired] public int PayrollMonth { get; set; }
    private int ComparisonEndMonth => Comparison
        .SelectMany(node => node.Months)
        .Select(month => month.PayrollMonth)
        .DefaultIfEmpty(PayrollMonth)
        .Max();

    private IReadOnlyList<ComparisonMonth> ComparisonMonths => Enumerable.Range(1, ComparisonEndMonth)
        .Select(month => new ComparisonMonth(
            month,
            $"Tháng {month:00}",
            $"department-month-{month:00}",
            GetMonthTotal(month)))
        .ToArray();

    private string ComparisonGridHostStyle =>
        $"--dashboard-comparison-grid-min-width: {20 + ComparisonMonths.Count * 20}rem;";

    private decimal GetMonthTotal(int month) => Comparison
        .Where(node => node.ParentId is null)
        .Sum(node => GetAmount(node, month));

    private static decimal GetAmount(PayrollAllowanceDashboardDepartmentTreeNodeDto row, int month) =>
        row.Months.FirstOrDefault(item => item.PayrollMonth == month)?.Amount ?? 0m;

    private static decimal GetChange(PayrollAllowanceDashboardDepartmentTreeNodeDto row, int month) =>
        GetAmount(row, month) - (month > 1 ? GetAmount(row, month - 1) : 0m);

    private static string GetAmountFieldName(int month) => $"{AmountFieldPrefix}{month:00}";

    private static string GetChangeFieldName(int month) => $"{ChangeFieldPrefix}{month:00}";

    private static void GetDepartmentUnboundColumnData(TreeListUnboundColumnDataEventArgs args)
    {
        if(args.DataItem is not PayrollAllowanceDashboardDepartmentTreeNodeDto row) return;

        args.Value = GetMonthlyColumnValue(row, args.FieldName);
    }

    private static decimal? GetMonthlyColumnValue(PayrollAllowanceDashboardDepartmentTreeNodeDto row, string? fieldName)
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

    private static string GetDepartmentNameCssClass(PayrollAllowanceDashboardDepartmentTreeNodeDto node) =>
        node.HierarchyLevel switch
        {
            0 => "dashboard-tree-node dashboard-tree-node-root",
            1 => "dashboard-tree-node dashboard-tree-node-department",
            _ => "dashboard-tree-node"
        };

    private sealed record ComparisonMonth(int Value, string Caption, string BandName, decimal TotalAmount);
}
