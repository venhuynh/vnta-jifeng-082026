namespace Vnta.Hrm.Infrastructure.NhanSu.NhanVien;

internal static class EmployeeCodeNormalizer
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length < 5 && normalized.All(char.IsDigit))
        {
            normalized = normalized.PadLeft(5, '0');
        }

        return normalized;
    }
}
