using Microsoft.Playwright;

namespace risk.control.system.e2e.tests.Pages;

/// <summary>
/// Page Object Model for the Dashboard/Home page
/// </summary>
public class DashboardPage
{
    private readonly IPage _page;

    // Locators
    private const string DashboardTitleSelector = "h1, .page-title";
    private const string UserMenuSelector = ".user-menu, [data-testid='user-menu']";
    private const string LogoutButtonSelector = "a[href*='logout'], button:has-text('Logout')";
    private const string WelcomeMessageSelector = ".welcome-message, [data-testid='welcome-message']";
    private const string DashboardContentSelector = ".dashboard-content, [role='main']";

    public DashboardPage(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// Navigates to the dashboard
    /// </summary>
    public async Task NavigateToDashboard(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Gets the dashboard title
    /// </summary>
    public async Task<string?> GetDashboardTitle()
    {
        try
        {
            return await _page.TextContentAsync(DashboardTitleSelector);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if dashboard is loaded
    /// </summary>
    public async Task<bool> IsDashboardLoaded()
    {
        try
        {
            return await _page.IsVisibleAsync(DashboardContentSelector);
        }
        catch
        {
            return false;
        }
    }


    /// <summary>
    /// Clicks on user menu with improved error handling
    /// </summary>
    public async Task ClickUserMenu()
    {
        try
        {
            var userMenuLocator = _page.Locator(UserMenuSelector);
            await userMenuLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await userMenuLocator.ClickAsync();
            await _page.WaitForTimeoutAsync(500);
        }
        catch (TimeoutException)
        {
            throw new Exception($"User menu not found. Current URL: {_page.Url}");
        }
    }

    /// <summary>
    /// Performs logout
    /// </summary>
    public async Task Logout()
    {
        try
        {
            // Bootstrap dropdown menu structure:
            // The settings dropdown is a nav link with fa-cog icon
            // When clicked, shows a dropdown menu with logout button

            // Click the settings dropdown toggle (looks for nav link with fa-cog icon)
            await _page.ClickAsync(".nav-link.dropdown-toggle .fa-cog");
            await _page.WaitForTimeoutAsync(500); // Wait for dropdown to expand

            // Click the logout link by text
            await _page.ClickAsync("a:has-text('Logout')");

            // The logout modal may appear, or we may be redirected
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        catch (Exception ex)
        {
            throw new Exception($"Logout failed. Error: {ex.Message}. Current URL: {_page.Url}", ex);
        }
    }

    /// <summary>
    /// Gets welcome message text
    /// </summary>
    public async Task<string?> GetWelcomeMessage()
    {
        try
        {
            return await _page.TextContentAsync(WelcomeMessageSelector);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if user is logged in by verifying dashboard content and URL
    /// </summary>
    public async Task<bool> IsUserLoggedIn()
    {
        try
        {
            // Check if we're on a dashboard or home page (not login page)
            var url = _page.Url;
            if (url.Contains("/Account/Login") || url.EndsWith("/Account/ChangePassword"))
            {
                return false;
            }

            // Check if dashboard content or main role is visible
            var isDashboardVisible = await _page.IsVisibleAsync(DashboardContentSelector);
            if (isDashboardVisible)
                return true;

            // Fallback: check if we're on Dashboard or home page URL
            return url.Contains("/Dashboard") || url.EndsWith("/") || url.Contains("/dashboard");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clicks a menu item by text
    /// </summary>
    public async Task ClickMenuItemByText(string menuText)
    {
        await _page.ClickAsync($"text={menuText}");
    }

    /// <summary>
    /// Waits for page to be fully loaded
    /// </summary>
    public async Task WaitForPageLoad()
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
