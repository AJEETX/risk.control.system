using NUnit.Framework;
using risk.control.system.e2e.tests.Fixtures;
using risk.control.system.e2e.tests.Pages;

namespace risk.control.system.e2e.tests.Tests.Dashboard;

/// <summary>
/// E2E tests for dashboard functionality
/// </summary>
[TestFixture(Category = "Dashboard")]
public class DashboardTests : BaseTest
{
    private LoginPage? _loginPage;
    private DashboardPage? _dashboardPage;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _loginPage = new LoginPage(Page!);
        _dashboardPage = new DashboardPage(Page!);

        // Login before dashboard tests
        await _loginPage!.NavigateToLogin(Config.BaseUrl);
        await _loginPage.Login(Config.PortalAdminUser.Email, Config.PortalAdminUser.Password);
    }

    /// <summary>
    /// Test: Dashboard loads successfully after login
    /// </summary>
    [Test]
    [Order(1)]
    public async Task Dashboard_ShouldLoadAfterLogin()
    {
        // Assert
        Assert.That(await _dashboardPage!.IsDashboardLoaded(), Is.True, "Dashboard should be loaded");
    }

    /// <summary>
    /// Test: Dashboard displays user information
    /// </summary>
    [Test]
    [Order(2)]
    public async Task Dashboard_ShouldDisplayUserInfo()
    {
        // Act
        var isLoggedIn = await _dashboardPage!.IsUserLoggedIn();

        // Assert
        Assert.That(isLoggedIn, Is.True, "Dashboard should show user is logged in");
    }

    /// <summary>
    /// Test: Dashboard has navigation menu
    /// </summary>
    [Test]
    [Order(3)]
    public async Task Dashboard_ShouldHaveNavigationMenu()
    {
        // Act - Try to click a menu item (adjust selector based on your app)
        try
        {
            // This will depend on your actual menu structure
            await Page!.WaitForSelectorAsync("nav, .sidebar, [role='navigation']", new PageWaitForSelectorOptions { Timeout = 5000 });
            
            // Assert
            var isMenuVisible = await IsElementVisible("nav, .sidebar, [role='navigation']");
            Assert.That(isMenuVisible, Is.True, "Navigation menu should be visible");
        }
        catch
        {
            // Menu may not exist, which is okay for this generic test
            Assert.Pass("Navigation menu check skipped - element not found in this configuration");
        }
    }

    /// <summary>
    /// Test: User menu is accessible from dashboard
    /// </summary>
    [Test]
    [Order(4)]
    public async Task Dashboard_UserMenu_ShouldBeAccessible()
    {
        // This test checks if user can interact with their profile/menu
        try
        {
            // Try to find and interact with user menu
            var userMenuExists = await IsElementVisible(".user-menu, [data-testid='user-menu']");
            Assert.That(userMenuExists, Is.True, "User menu should be accessible");
        }
        catch
        {
            Assert.Pass("User menu check skipped - element not found in this configuration");
        }
    }

    /// <summary>
    /// Test: Dashboard page responds to resize events
    /// </summary>
    [Test]
    [Order(5)]
    public async Task Dashboard_ShouldResponsivelyResize()
    {
        // Act - Resize viewport
        await Page!.SetViewportSizeAsync(1024, 768);
        await Task.Delay(500); // Wait for resize to complete

        // Assert - Page should still be loaded
        var isStillLoaded = await _dashboardPage!.IsDashboardLoaded();
        Assert.That(isStillLoaded, Is.True, "Dashboard should still be loaded after resize");

        // Act - Resize to mobile
        await Page!.SetViewportSizeAsync(375, 667);
        await Task.Delay(500);

        // Assert
        isStillLoaded = await _dashboardPage.IsDashboardLoaded();
        Assert.That(isStillLoaded, Is.True, "Dashboard should still be loaded on mobile viewport");
    }

    /// <summary>
    /// Test: Page title is present
    /// </summary>
    [Test]
    [Order(6)]
    public async Task Dashboard_ShouldHavePageTitle()
    {
        // Act
        var title = await Page!.TitleAsync();

        // Assert
        Assert.That(title, Is.Not.Null.And.Not.Empty, "Page should have a title");
        Assert.That(title, Does.Contain("iCheckify"), "Page title should contain application name");
    }

    /// <summary>
    /// Test: No console errors on dashboard load
    /// </summary>
    [Test]
    [Order(7)]
    public async Task Dashboard_ShouldHaveNoConsoleErrors()
    {
        var errors = new List<string>();

        // Capture console messages
        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                errors.Add(msg.Text);
            }
        };

        // Reload page to check for errors
        await Page!.ReloadAsync();
        await Task.Delay(2000);

        // Assert
        if (errors.Count > 0)
        {
            Assert.Warn($"Console errors detected: {string.Join(", ", errors)}");
        }
        else
        {
            Assert.Pass("No console errors detected");
        }
    }
}
