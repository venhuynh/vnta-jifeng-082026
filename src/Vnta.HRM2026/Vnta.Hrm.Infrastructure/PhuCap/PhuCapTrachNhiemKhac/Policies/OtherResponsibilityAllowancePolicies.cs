namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;

internal static class OtherResponsibilityAllowancePeriodPolicy
{
    internal const int MinimumSupportedYear = 2026;
    internal const int MinimumSupportedMonth = 6;
    internal const int MaximumSupportedYear = 2100;
    internal const int MaxSearchResultLimit = 2000;

    internal static void Validate(int year, int month)
    {
        if(year < MinimumSupportedYear || year > MaximumSupportedYear)
        {
            throw new InvalidOperationException(
                $"Năm dữ liệu phải nằm trong khoảng {MinimumSupportedYear} đến {MaximumSupportedYear}.");
        }

        if(month is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
        }

        if(year == MinimumSupportedYear && month < MinimumSupportedMonth)
        {
            throw new InvalidOperationException(
                $"Dữ liệu chỉ hỗ trợ từ tháng {MinimumSupportedMonth:00}/{MinimumSupportedYear}.");
        }
    }
}

internal static class OtherResponsibilityAllowancePersistenceSupport
{
    internal const string SystemActor = "system";

    internal static DateTime GetDatabaseNow()
    {
        var now = DateTime.UtcNow.AddHours(7);
        return new DateTime(
            now.Ticks - now.Ticks % TimeSpan.TicksPerMicrosecond,
            DateTimeKind.Unspecified);
    }

    internal static string NormalizeActor(string? actor) =>
        string.IsNullOrWhiteSpace(actor) ? SystemActor : actor.Trim();
}
