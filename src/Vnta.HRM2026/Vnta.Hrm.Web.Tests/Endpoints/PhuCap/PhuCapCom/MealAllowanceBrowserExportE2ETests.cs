using Microsoft.Playwright;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapCom;

/// <summary>
/// Browser-level smoke test for the rendered InteractiveServer screen and its real download workflow.
/// It is intentionally opt-in: the URL and authenticated storage state must belong to a dedicated test host.
/// No API is mocked and the test only reads the selected period before downloading the generated file.
/// </summary>
public sealed class MealAllowanceBrowserExportE2ETests
{
    [MealAllowanceBrowserE2EFact]
    public async Task Payroll_administrator_can_load_the_rendered_meal_allowance_screen_and_download_excel_for_the_test_period()
    {
        var baseUrl = RequireEnvironment("VNTA_MEAL_ALLOWANCE_E2E_BASE_URL");
        var storageStatePath = RequireEnvironment("VNTA_MEAL_ALLOWANCE_E2E_STORAGE_STATE_PATH");
        var expectedEmployeeCode = RequireEnvironment("VNTA_MEAL_ALLOWANCE_E2E_EXPECTED_EMPLOYEE_CODE");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            StorageStatePath = storageStatePath,
            AcceptDownloads = true
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(new Uri(new Uri(baseUrl), "/payroll/meal-allowance").ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Assertions.Expect(page.GetByText("Phụ cấp cơm", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();

        await page.GetByText("Xem", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByText(expectedEmployeeCode, new PageGetByTextOptions { Exact = false })).ToBeVisibleAsync();

        await page.GetByText("Xuất file", new PageGetByTextOptions { Exact = true }).ClickAsync();
        var downloadTask = page.WaitForDownloadAsync();
        await page.GetByText("Xuất Excel", new PageGetByTextOptions { Exact = true }).ClickAsync();
        var download = await downloadTask;

        Assert.EndsWith(".xlsx", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("meal-allowance-", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
    }

    private static string RequireEnvironment(string variable) =>
        Environment.GetEnvironmentVariable(variable)
        ?? throw new InvalidOperationException($"Set {variable} for meal allowance browser E2E tests.");
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class MealAllowanceBrowserE2EFactAttribute : FactAttribute
{
    private static readonly string[] RequiredVariables =
    [
        "VNTA_MEAL_ALLOWANCE_E2E_BASE_URL",
        "VNTA_MEAL_ALLOWANCE_E2E_STORAGE_STATE_PATH",
        "VNTA_MEAL_ALLOWANCE_E2E_EXPECTED_EMPLOYEE_CODE"
    ];

    public MealAllowanceBrowserE2EFactAttribute()
    {
        var missing = RequiredVariables.FirstOrDefault(variable =>
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)));
        if(missing is not null)
        {
            Skip = $"Set {string.Join(", ", RequiredVariables)} and install Playwright Chromium to run this browser E2E test against a dedicated test host.";
        }
    }
}
