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
            // Try multiple heuristics to detect a loaded dashboard
            var selectors = new[] { 
                DashboardContentSelector, // existing
                "section.content", 
                ".card-body",
                "h3.card-title",
                "#content",
                ".content-wrapper",
            };

            foreach (var sel in selectors)
            {
                try
                {
                    var locator = _page.Locator(sel);
                    if (await locator.CountAsync() > 0)
                    {
                        // Wait briefly for element to be visible
                        try { await locator.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 2000 }); } catch { }
                        if (await locator.IsVisibleAsync())
                            return true;
                    }
                }
                catch
                {
                    // ignore and try next selector
                }
            }

            // Fallback: check page title or header text contains 'Dashboard'
            try
            {
                var title = await _page.TitleAsync();
                if (!string.IsNullOrWhiteSpace(title) && title.Contains("Dashboard", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            try
            {
                var header = await _page.TextContentAsync("h3.card-title");
                if (!string.IsNullOrWhiteSpace(header) && header.Contains("Dashboard", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            return false;
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
            // Click the logout trigger (opens modal) then confirm logout in modal
            var trigger = _page.Locator("a[data-toggle='modal'][data-target='#logoutModal'], button[data-toggle='modal'][data-target='#logoutModal']");
            if (await trigger.CountAsync() > 0)
            {
                try
                {
                    await trigger.First.ClickAsync(new LocatorClickOptions { Force = true });
                }
                catch
                {
                    // Fallback to JS click if Playwright click fails (e.g., element not in viewport)
                    try
                    {
                        await trigger.First.EvaluateAsync("el => el.click()");
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            else
            {
                // Fallback: try clicking any nav link that contains logout text/icon
                try { await _page.ClickAsync("text=Logout"); } catch { }
            }

            // Wait for the logout modal and click the logout button
            try
            {
                await _page.Locator("#logoutModal").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                var logoutBtn = _page.Locator("#logoutModal button#logout, button#logout");
                if (await logoutBtn.CountAsync() > 0)
                {
                    await logoutBtn.First.ClickAsync(new LocatorClickOptions { Force = true });
                    // Wait briefly for navigation/redirect
                    try { await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 5000 }); } catch { }
                }
            }
            catch
            {
                // If modal didn't appear, try direct logout link
                try { await _page.ClickAsync("a[href*='/Account/Logout'], a[href*='/Tools/Logout']"); } catch { }
            }

            // Wait for redirect or network idle
            try { await _page.WaitForLoadStateAsync(LoadState.NetworkIdle); } catch { }
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
