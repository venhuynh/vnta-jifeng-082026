using System.Reflection;

namespace Vnta.Hrm.Web.Client.Components.Layout.Shared;

public partial class ApplicationMenuFooter
{
    private const string ProductName = "VNTA HRM";
    private const string ProductDescription = "Hệ thống quản trị nhân sự";
    private const string CopyrightYear = "2026";
    private const string OrganizationName = "VNTA";

    private static readonly IReadOnlyDictionary<string, string> BuildMetadata = typeof(ApplicationMenuFooter)
        .Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .ToDictionary(attribute => attribute.Key, attribute => attribute.Value ?? string.Empty);

    private static string ReleaseVersion => GetBuildMetadata("ApplicationVersion", "2026.07");

    private static string BuildNumber => GetBuildMetadata("BuildNumber", "0");

    private static string ReleaseDate => GetBuildMetadata("ReleaseDate", "Chưa phát hành");

    private static string GetBuildMetadata(string key, string fallback)
        => BuildMetadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
}
