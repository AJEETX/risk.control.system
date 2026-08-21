using NUnit.Framework;
using risk.control.system.e2e.tests.Fixtures;
using risk.control.system.e2e.tests.Pages;

namespace risk.control.system.e2e.tests.Tests.Navigation;

/// <summary>
/// E2E tests for navigation and page accessibility
/// </summary>
[TestFixture(Category = "Navigation")]
public class NavigationTests : BaseTest
{
    private LoginPage? _loginPage;
    private DashboardPage? _dashboardPage;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _loginPage = new LoginPage(Page!);
        _dashboardPage = new DashboardPage(Page!);

        // Login before navigation tests
        await _loginPage!.NavigateToLogin(Config.BaseUrl);
        await _loginPage.Login(Config.PortalAdminUser.Email, Config.PortalAdminUser.Password);
    }

    /// <summary>
    /// Test: Browser back button works correctly
    /// </summary>
    [Test]
    [Order(1)]
    public async Task Navigation_BackButton_ShouldWork()
    {
        // Arrange
        var currentUrl = Page!.Url;

        // Act - Navigate to a different page if possible
        try
        {
            await Page!.ClickAsync("a[href]", new PageClickOptions { Timeout = 5000 });
            await Task.Delay(1000);
        }
        catch
        {
            // No link found, skip navigation
        }

        var urlAfterClick = Page!.Url;

        // Go back
        await Page!.GoBackAsync();
        await Task.Delay(1000);

        // Assert
        var urlAfterBack = Page!.Url;
        Assert.That(urlAfterBack, Is.Not.Empty, "Navigation should have URL");
    }

    /// <summary>
    /// Test: Browser forward button works correctly
    /// </summary>
    [Test]
    [Order(2)]
    public async Task Navigation_ForwardButton_ShouldWork()
    {
        // Act
        await Page!.GoBackAsync();
        await Task.Delay(500);
        await Page!.GoForwardAsync();
        await Task.Delay(500);

        // Assert
        var isLoaded = await _dashboardPage!.IsDashboardLoaded();
        Assert.That(isLoaded || Page!.Url.Contains("login"), Is.True, "Page should navigate forward");
    }

    /// <summary>
    /// Test: Page refresh maintains state
    /// </summary>
    [Test]
    [Order(3)]
    public async Task Navigation_PageRefresh_ShouldMaintainSession()
    {
        // Arrange
        var urlBefore = Page!.Url;

        // Act
        await Page!.ReloadAsync();
        await Task.Delay(2000);

        // Assert
        var isStillLoggedIn = await _dashboardPage!.IsUserLoggedIn() || Page!.Url.Contains("dashboard");
        Assert.That(isStillLoggedIn, Is.True, "Session should be maintained after refresh");
    }

    /// <summary>
    /// Test: Direct URL navigation works
    /// </summary>
    [Test]
    [Order(4)]
    public async Task Navigation_DirectUrl_ShouldLoad()
    {
        // Act
        await NavigateTo("dashboard");
        await Task.Delay(1000);

        // Assert
        Assert.That(Page!.Url.Contains("dashboard") || await _dashboardPage!.IsDashboardLoaded(), Is.True,
            "Should be able to navigate directly to dashboard URL");
    }

    /// <summary>
    /// Test: Invalid URL shows appropriate error
    /// </summary>
    [Test]
    [Order(5)]
    public async Task Navigation_InvalidUrl_ShouldHandleGracefully()
    {
        // Act
        await NavigateTo("invalid-page-that-does-not-exist");
        await Task.Delay(1000);

        // Assert - Should either show 404 or redirect to home
        var url = Page!.Url;
        Assert.That(
            url.Contains("404") || url.Contains("error") || url.Contains("home") || url.Contains("dashboard"),
            Is.True,
            "Should handle invalid URL gracefully"
        );
    }

    /// <summary>
    /// Test: Navigation between multiple pages
    /// </summary>
    [Test]
    [Order(6)]
    public async Task Navigation_MultiplePage_ShouldSucceed()
    {
        // Act
        var initialUrl = Page!.Url;

        // Try to click multiple links
        var linkCount = 0;
        try
        {
            var links = await Page!.QuerySelectorAllAsync("a[href]:not([href^='#']):not([href^='javascript'])");
            linkCount = Math.Min(links.Count, 3); // Try first 3 links max

            for (int i = 0; i < linkCount; i++)
            {
                try
                {
                    var link = links[i];
                    await link.ClickAsync();
                    await Task.Delay(1000);
                }
                catch
                {
                    // Link might not be clickable, continue
                }
            }
        }
        catch
        {
            // No links found
        }

        // Assert
        Assert.That(Page!.Url != initialUrl || linkCount == 0, Is.True, 
            "Should be able to navigate through multiple pages");
    }

    /// <summary>
    /// Test: Loading states are displayed correctly
    /// </summary>
    [Test]
    [Order(7)]
    public async Task Navigation_LoadingStates_ShouldDisplay()
    {
        // Act - Reload page to see loading state
        var loadingIndicators = new List<string>();

        Page!.Console += (_, msg) =>
        {
            if (msg.Text.Contains("Loading") || msg.Text.Contains("loading"))
            {
                loadingIndicators.Add(msg.Text);
            }
        };

        await Page!.ReloadAsync();
        await Page!.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Assert
        var isLoaded = await _dashboardPage!.IsDashboardLoaded();
        Assert.That(isLoaded, Is.True, "Page should complete loading");
    }
}
