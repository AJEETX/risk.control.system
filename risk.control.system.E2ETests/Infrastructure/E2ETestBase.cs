using System.Text.RegularExpressions;

namespace risk.control.system.E2ETests.Infrastructure;

/// <summary>
/// Base class for browser tests. Expects the web app to already be running at <see cref="TestSettings.BaseUrl"/>.
/// </summary>
public abstract class E2ETestBase : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestSettings.BaseUrl,
        IgnoreHTTPSErrors = true, // local dev certificate
        ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        // The agent investigation page asks for the browser location.
        Permissions = new[] { "geolocation" },
        Geolocation = new Geolocation { Latitude = -37.8136f, Longitude = 144.9631f },
    };

    [TearDown]
    public async Task ScreenshotOnFailureAsync()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            return;
        }

        var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        var name = string.Concat(TestContext.CurrentContext.Test.Name.Split(Path.GetInvalidFileNameChars()));
        var file = Path.Combine(dir, $"{name}.png");
        try
        {
            await Page.ScreenshotAsync(new() { Path = file, FullPage = true });
            TestContext.AddTestAttachment(file);
            TestContext.Progress.WriteLine($"[E2E] Screenshot: {file} (url: {Page.Url})");
        }
        catch (PlaywrightException)
        {
            // Page already closed.
        }
    }

    protected async Task LogoutAsync()
    {
        await Page.GotoAsync("/Account/Logout");
        await Expect(Page).ToHaveURLAsync(LoginUrl);

        // Logout clears every cookie, including the consent ones, which brings the consent popup back.
        await AddConsentCookiesAsync();
    }

    /// <summary>Clicks a button in the jquery-confirm dialog.</summary>
    protected async Task ConfirmAsync(string buttonText)
    {
        var button = Page.Locator(".jconfirm-box button", new() { HasTextString = buttonText }).First;
        await Expect(button).ToBeVisibleAsync();
        await button.ClickAsync();
    }

    /// <summary>Asserts a Notyf toast containing <paramref name="text"/> is shown.</summary>
    protected async Task ExpectToastAsync(string text)
    {
        await Expect(Page.Locator(".notyf__message", new() { HasTextString = text }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    /// <summary>
    /// Finds the #dataTable row containing <paramref name="text"/>, using the table search box when present
    /// (server-side tables are paged).
    /// </summary>
    protected async Task<ILocator> FindRowAsync(string text)
    {
        var row = Page.Locator("#dataTable tbody tr", new() { HasTextString = text }).First;
        try
        {
            // New cases are normally on the first page.
            await row.WaitForAsync(new() { Timeout = 5_000 });
            return row;
        }
        catch (TimeoutException)
        {
            // Fall back to the table search (server-side tables are paged).
        }

        var search = Page.Locator("#dataTable_filter input");
        if (await search.CountAsync() > 0)
        {
            await search.FillAsync(text);
        }

        await Expect(row).ToBeVisibleAsync(new() { Timeout = 15_000 });
        return row;
    }

    /// <summary>
    /// Returns a visible element inside a table row. Tables use DataTables "responsive" mode, which moves
    /// columns that do not fit (often Actions) into a child row that opens when the row's control cell is clicked.
    /// </summary>
    protected async Task<ILocator> RowElementAsync(ILocator row, string selector)
    {
        var inRow = row.Locator(selector).First;
        if (await inRow.IsVisibleAsync())
        {
            return inRow;
        }

        await row.Locator("td.dtr-control").First.ClickAsync();
        var inChild = row.Locator("xpath=following-sibling::tr[1][contains(@class,'child')]").Locator(selector).First;
        await Expect(inChild).ToBeVisibleAsync();
        return inChild;
    }

    [SetUp]
    public Task DismissCookieConsentAsync() => AddConsentCookiesAsync();

    [SetUp]
    public void LogBrowserErrors()
    {
        // Surface JavaScript errors in the test output; they explain "nothing happened" failures.
        Page.PageError += (_, error) => TestContext.Progress.WriteLine($"[browser error] {Page.Url}: {error}");
        Page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                TestContext.Progress.WriteLine($"[browser console] {message.Text}");
            }
        };
        Page.RequestFailed += (_, request) =>
        {
            // ERR_ABORTED is normal when a page navigates away; third-party (maps) noise is ignored.
            if (request.Failure?.Contains("ERR_ABORTED") == true
                || !request.Url.StartsWith(TestSettings.BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TestContext.Progress.WriteLine($"[request failed] {request.Method} {request.Url}: {request.Failure}");
        };
    }

    private async Task AddConsentCookiesAsync()
    {
        // The cookie consent popup is only rendered when neither cookie is present.
        var host = new Uri(TestSettings.BaseUrl).Host;
        await Context.AddCookiesAsync(new[]
        {
            new Cookie { Name = "analyticsCookies", Value = "true", Domain = host, Path = "/" },
            new Cookie { Name = "perfomanceCookies", Value = "true", Domain = host, Path = "/" },
        });
    }

    protected static readonly Regex LoginUrl = new("/Account/Login", RegexOptions.IgnoreCase);

    /// <param name="agentPortal">
    /// Agents are blocked from the portal (feature flag AGENT_LOGIN_DISABLED_ON_PORTAL) unless the login form's
    /// agent_login field is "agent_login" (see LoginService.GetUserStatusAsync).
    /// </param>
    protected async Task LoginAsync(string email, string password, bool agentPortal = false)
    {
        await Page.GotoAsync("/Account/Login");
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#Password").FillAsync(password);
        if (agentPortal)
        {
            // Hidden field (display:none in login.css), so set it directly.
            await Page.EvalOnSelectorAsync("#agent_login", "e => e.value = 'agent_login'");
        }

        await Page.GetByTestId("logintest").ClickAsync();
    }

    protected async Task LoginAsAsync(Role role)
    {
        await LoginAsync(Users.EmailFor(role), TestSettings.Password, agentPortal: role == Role.Agent);

        // Login posts ReturnUrl="/" which maps to Dashboard/Index (default route).
        await Expect(Page).Not.ToHaveURLAsync(LoginUrl, new() { Timeout = 15_000 });
        await Expect(Page.Locator(".error-message .error")).ToHaveCountAsync(0);
    }

    protected Task LoginAsManagerAsync() => LoginAsAsync(Role.Manager);

    /// <summary>
    /// Asserts the current page is a normal app page: still on <paramref name="expectedPath"/>, layout rendered,
    /// no developer exception page / error page, and no DataTables error dialog.
    /// </summary>
    protected async Task AssertHealthyPageAsync(string expectedPath)
    {
        // Caught controller errors redirect to the dashboard; auth failures redirect to login.
        await Expect(Page).ToHaveURLAsync(new Regex(Regex.Escape(expectedPath), RegexOptions.IgnoreCase));
        await Expect(Page.GetByText("An unhandled exception occurred")).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Something went wrong")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".main-sidebar")).ToBeVisibleAsync();
        // DataTableErrorHandler shows a jquery-confirm dialog ("Server Error!", "Session Expired!", ...).
        await Expect(Page.Locator(".jconfirm")).ToHaveCountAsync(0);
    }
}
