namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop.Models;

/// <summary>Small display contracts shared by the page coordinator and its sections.</summary>
public sealed record MonthOption(int Value, string Text);

public sealed record PageSizeOption(int Value, string Text);

public sealed record AllowanceAmountSummary(string Label, decimal Amount, string CssClass);

public sealed record AllowanceSummaryBadge(string Key, string Label, int Count);
