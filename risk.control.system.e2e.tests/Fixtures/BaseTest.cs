using Microsoft.Playwright;
using NUnit.Framework;
using risk.control.system.e2e.tests.Configuration;

namespace risk.control.system.e2e.tests.Fixtures;

/// <summary>
/// Base test fixture providing browser and page setup for all E2E tests
/// </summary>
[TestFixture]
public abstract class BaseTest
{
    protected IPlaywright? Playwright { get; set; }
    protected IBrowser? Browser { get; set; }
    protected IBrowserContext? Context { get; set; }
    protected IPage? Page { get; set; }
    protected TestConfiguration Config { get; set; } = new TestConfiguration();

    [SetUp]
    public virtual async Task SetUp()
    {
        // Create artifacts directory if it doesn't exist
        Directory.CreateDirectory(Config.ArtifactsPath);

        // Initialize Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // Launch browser based on configuration
        Browser = Config.BrowserType.ToLowerInvariant() switch
        {
            "firefox" => await Playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = Config.Headless }),
            "webkit" => await Playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = Config.Headless }),
            _ => await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = Config.Headless })
        };

        // Create context with optional video recording
        var contextOptions = new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        };

        if (Config.RecordVideos)
        {
            contextOptions.RecordVideoDir = Config.ArtifactsPath;
        }

        Context = await Browser.NewContextAsync(contextOptions);

        // Create page
        Page = await Context.NewPageAsync();

        // Set default timeouts
        Page.SetDefaultTimeout(Config.ActionTimeout);
        Page.SetDefaultNavigationTimeout(Config.NavigationTimeout);
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        // Capture screenshot on failure
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed && Config.CaptureScreenshots)
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var screenshotPath = Path.Combine(Config.ArtifactsPath, $"{testName}_failure.png");
            await Page?.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath })!;
            TestContext.AddTestAttachment(screenshotPath, "Screenshot");
        }

        // Cleanup resources
        if (Context != null)
            await Context.CloseAsync();

        if (Browser != null)
            await Browser.CloseAsync();

        Playwright?.Dispose();
    }

    /// <summary>
    /// Navigates to the base URL of the application
    /// </summary>
    protected async Task NavigateToHome()
    {
        await Page!.GotoAsync(Config.BaseUrl);
    }

    /// <summary>
    /// Navigates to a specific path
    /// </summary>
    protected async Task NavigateTo(string path)
    {
        var url = Config.BaseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
        await Page!.GotoAsync(url);
    }

    /// <summary>
    /// Waits for a specific URL to match
    /// </summary>
    protected async Task WaitForUrl(string urlPart)
    {
        await Page!.WaitForURLAsync($"**/{urlPart}/**");
    }

    /// <summary>
    /// Fills an input field and triggers change events
    /// </summary>
    protected async Task FillField(string selector, string value)
    {
        await Page!.FillAsync(selector, value);
        await Page.DispatchEventAsync(selector, "change");
    }

    /// <summary>
    /// Checks if an element is visible on the page
    /// </summary>
    protected async Task<bool> IsElementVisible(string selector)
    {
        try
        {
            return await Page!.IsVisibleAsync(selector);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets text content of an element
    /// </summary>
    protected async Task<string?> GetElementText(string selector)
    {
        return await Page!.TextContentAsync(selector);
    }

    /// <summary>
    /// Clicks an element
    /// </summary>
    protected async Task ClickElement(string selector)
    {
        await Page!.ClickAsync(selector);
    }

    /// <summary>
    /// Waits for element to be visible
    /// </summary>
    protected async Task WaitForElement(string selector)
    {
        await Page!.WaitForSelectorAsync(selector);
    }

    /// <summary>
    /// Takes a screenshot
    /// </summary>
    protected async Task TakeScreenshot(string name)
    {
        var screenshotPath = Path.Combine(Config.ArtifactsPath, $"{name}.png");
        await Page!.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
    }
}
