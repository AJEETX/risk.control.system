using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace risk.control.system.e2e.tests.Fixtures;

/// <summary>
/// Base test fixture providing browser and page setup for all E2E tests
/// </summary>
[TestFixture]
public abstract class BaseTest
{
    private static Process? _webServerProcess;
    protected IPlaywright? Playwright { get; set; }
    protected IBrowser? Browser { get; set; }
    protected IBrowserContext? Context { get; set; }
    protected IPage? Page { get; set; }
    protected TestConfiguration Config { get; set; } = new TestConfiguration();

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var webAppPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../risk.control.system"));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{webAppPath}\" --urls \"{Config.BaseUrl}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = webAppPath
        };

        // Force Development environment so launch profiles load correctly
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

        // Capture stdout/stderr so we can detect when Kestrel reports it's listening
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        // Ensure artifacts directory exists for logs
        try
        {
            Directory.CreateDirectory(Config.ArtifactsPath);
        }
        catch
        {
            // ignore - best effort to create artifacts directory
        }

        _webServerProcess = Process.Start(startInfo);

        if (_webServerProcess == null)
            throw new InvalidOperationException("Failed to start web server process.");

        var serverReady = false;
        var timeout = TimeSpan.FromSeconds(120); // give the app more time to start

        var outputLogPath = Path.Combine(Config.ArtifactsPath, "webserver_output.log");

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _webServerProcess.OutputDataReceived += (s, e) =>
        {
            if (e.Data == null) return;
            try { File.AppendAllText(outputLogPath, e.Data + Environment.NewLine); } catch { }
            if (e.Data.Contains("Now listening on", StringComparison.OrdinalIgnoreCase) || e.Data.Contains("Application started", StringComparison.OrdinalIgnoreCase))
            {
                tcs.TrySetResult(true);
            }
        };

        _webServerProcess.ErrorDataReceived += (s, e) =>
        {
            if (e.Data == null) return;
            try { File.AppendAllText(outputLogPath, "ERR: " + e.Data + Environment.NewLine); } catch { }
        };

        _webServerProcess.BeginOutputReadLine();
        _webServerProcess.BeginErrorReadLine();

        // Bypass SSL certificate validation for local health check
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        };

        using var httpClient = new HttpClient(handler);

        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            // If we detected ready message on stdout, stop waiting
            if (tcs.Task.IsCompleted)
            {
                serverReady = true;
                break;
            }

            try
            {
                var response = await httpClient.GetAsync(Config.BaseUrl);
                // Any response (even 404 or 302) means Kestrel server is actively listening
                serverReady = true;
                break;
            }
            catch
            {
                // Server is still initializing
            }

            // If the process has exited early, break and fail fast
            if (_webServerProcess.HasExited)
            {
                try { File.AppendAllText(outputLogPath, $"Process exited with code {_webServerProcess.ExitCode}{Environment.NewLine}"); } catch { }
                break;
            }

            await Task.Delay(1000);
        }

        if (!serverReady)
        {
            var message = $"Web server failed to start at {Config.BaseUrl} within {timeout.TotalSeconds} seconds. See {outputLogPath} for details.";
            throw new InvalidOperationException(message);
        }
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        // Stop the background web application when all tests finish
        if (_webServerProcess != null && !_webServerProcess.HasExited)
        {
            _webServerProcess.Kill(entireProcessTree: true);
            _webServerProcess.Dispose();
        }
    }

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
            "firefox" => await Playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = Config.Headless, Args = Config.Headless ? null : new[] { "-start-fullscreen" } }),
            "webkit" => await Playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = Config.Headless, Args = Config.Headless ? null : new[] { "--start-maximized" } }),
            _ => await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = Config.Headless, Args = Config.Headless ? null : new[] { "--start-maximized" } })
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
        // Ensure the browser window is full-size for consistent E2E screenshots and interactions.
        // Headed browsers: attempt to maximize window and set a large viewport. In headless mode Playwright ignores window size.
        try
        {
            if (!Config.Headless)
            {
                // Set a common desktop resolution; also attempt to set viewport to a large size to simulate fullscreen.
                await Page.SetViewportSizeAsync(1920, 1080);
            }
        }
        catch
        {
            // Ignore failures to set viewport on some browser drivers
        }

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
        try
        {
            await Page!.GotoAsync(url, new Microsoft.Playwright.PageGotoOptions
            {
                WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded,
                Timeout = Config.NavigationTimeout
            });
        }
        catch
        {
            // Navigation may fail for invalid pages; tests will assert on behavior.
        }
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
