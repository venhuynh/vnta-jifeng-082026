using System.Globalization;
using System.Text;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.CodeKetQuaTinhCong;

internal static class AttendanceStatusCodePresentation
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    public const string YesNoCheckedDisplayText = "Có";
    public const string YesNoUncheckedDisplayText = "Không";

    public static string GetActiveText(bool isActive) => isActive ? "Đang áp dụng" : "Tạm ngưng";

    public static string GetActiveTextCssClass(bool value) =>
        string.Join(' ', "yes-no-status", value ? "yes-no-status-yes" : "yes-no-status-neutral");

    public static string GetDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value;

    public static string FormatDateTime(DateTime? value) => value.HasValue
        ? FormatDateTime(value.Value)
        : "--";

    public static string FormatDateTime(DateTime value) =>
        NormalizeDisplayDateTime(value).ToString("dd/MM/yyyy HH:mm:ss", DisplayCulture);

    public static string GetKindCssClass(string? kind) =>
        string.Join(' ', "kind-status", ResolveKindCssClass(kind));

    public static string GetKindText(string? kind) => NormalizeKindKey(kind) switch
    {
        "WORK" => "Ngày công",
        "LEAVE" => "Nghỉ",
        _ => "Khác"
    };

    public static string GetYesNoStatusCssClass(bool value) =>
        string.Join(' ', "yes-no-status", value ? "yes-no-status-yes" : "yes-no-status-no");

    public static string GetYesNoStatusText(bool value) =>
        value ? YesNoCheckedDisplayText : YesNoUncheckedDisplayText;

    private static DateTime NormalizeDisplayDateTime(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

    private static string ResolveKindCssClass(string? kind) => NormalizeKindKey(kind) switch
    {
        "WORK" => "kind-status-workday",
        "LEAVE" => "kind-status-leave",
        _ => "kind-status-neutral"
    };

    private static string NormalizeKindKey(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return string.Empty;
        }

        var normalized = kind.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark
                && char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }
}
