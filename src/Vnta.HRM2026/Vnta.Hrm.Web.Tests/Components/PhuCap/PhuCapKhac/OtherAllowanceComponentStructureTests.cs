using System.Text.RegularExpressions;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Components.PhuCap.PhuCapKhac;

public sealed class OtherAllowanceComponentStructureTests
{
    [Fact]
    public void Every_component_has_one_code_behind_and_one_scoped_css_file()
    {
        var root = FindRepositoryRoot();
        var componentRoot = Path.Combine(root, "src", "Vnta.HRM2026", "Vnta.Hrm.Web.Client", "Components", "PhuCap", "PhuCapKhac");
        var razorFiles = Directory.GetFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), "_Imports.razor", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(razorFiles);
        foreach(var razorFile in razorFiles)
        {
            var codeBehind = razorFile + ".cs";
            var css = razorFile + ".css";
            Assert.True(File.Exists(codeBehind), $"Missing code-behind: {codeBehind}");
            Assert.True(File.Exists(css), $"Missing scoped CSS: {css}");
            Assert.DoesNotContain("@code", File.ReadAllText(razorFile), StringComparison.Ordinal);
            AssertCssClassesMatchTheOwningComponent(razorFile, codeBehind, css);
        }

        var componentBases = razorFiles.Select(path => Path.GetFullPath(path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var codeBehind in Directory.GetFiles(componentRoot, "*.razor.cs", SearchOption.AllDirectories))
        {
            var razor = codeBehind[..^3];
            Assert.Contains(Path.GetFullPath(razor), componentBases);
        }

        foreach(var css in Directory.GetFiles(componentRoot, "*.razor.css", SearchOption.AllDirectories))
        {
            var razor = css[..^4];
            Assert.Contains(Path.GetFullPath(razor), componentBases);
        }
    }

    private static string FindRepositoryRoot()
    {
        for(var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if(Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static void AssertCssClassesMatchTheOwningComponent(string razorFile, string codeBehind, string cssFile)
    {
        var razorClasses = ExtractOtherAllowanceClasses(File.ReadAllText(razorFile));
        razorClasses.UnionWith(ExtractOtherAllowanceClasses(File.ReadAllText(codeBehind)));
        var cssClasses = ExtractOtherAllowanceClasses(File.ReadAllText(cssFile));

        Assert.True(
            !razorClasses.Except(cssClasses).Any(),
            $"Razor or code-behind CSS classes without a local selector in {Path.GetFileName(cssFile)}");
        Assert.True(
            !cssClasses.Except(razorClasses).Any(),
            $"CSS selectors without a matching class in {Path.GetFileName(razorFile)} or its code-behind");
    }

    private static HashSet<string> ExtractOtherAllowanceClasses(string content) =>
        Regex.Matches(content, @"\bother-allowance-[A-Za-z0-9-]+\b")
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
}
