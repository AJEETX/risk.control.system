using Microsoft.Playwright;

namespace risk.control.system.e2e.tests.Pages;

/// <summary>
/// Page Object Model for the Login page
/// </summary>
public class LoginPage
{
    private readonly IPage _page;

    // Locators that match the actual app markup.
    private const string EmailInputSelector = "#email, input[name='Email']";
    private const string PasswordInputSelector = "#Password, input[name='Password']";
    private const string LoginButtonSelector = "#login, button[data-testid='logintest'], button[type='submit']";
    private const string ErrorMessageSelector = ".error, .alert-danger, .text-danger, .error-message";
    private const string RememberMeCheckboxSelector = "input[name='RememberMe'], input[type='checkbox'][name*='Remember'], #rememberMe";
    private const string CookiePopupSelector = "#cookiePopup";
    private const string CookieAcceptButtonSelector = "#acceptCookies";

    public LoginPage(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// Navigates to the login page
    /// </summary>
    public async Task NavigateToLogin(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Account/Login");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await CloseCookiePopupIfVisible();
    }

    private async Task CloseCookiePopupIfVisible()
    {
        try
        {
            var popup = _page.Locator(CookiePopupSelector);
            if (await popup.IsVisibleAsync())
            {
                var acceptButton = _page.Locator(CookieAcceptButtonSelector);
                if (await acceptButton.IsVisibleAsync())
                {
                    await acceptButton.ClickAsync();
                    await _page.WaitForTimeoutAsync(500);
                }
            }
        }
        catch
        {
            // The cookie banner is optional and may not be present on every browser state.
        }
    }

    /// <summary>
    /// Performs login with email and password
    /// </summary>
    public async Task Login(string email, string password, bool rememberMe = false)
    {
        await CloseCookiePopupIfVisible();
        await _page.FillAsync(EmailInputSelector, email);
        await _page.FillAsync(PasswordInputSelector, password);

        var rememberMeCheckbox = _page.Locator(RememberMeCheckboxSelector);
        if (rememberMe && await rememberMeCheckbox.CountAsync() > 0)
        {
            await rememberMeCheckbox.CheckAsync();
        }

        await _page.ClickAsync(LoginButtonSelector);
        // Wait for navigation or an error message. Use a bounded timeout to avoid hanging.
        var navTask = _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 8000 });

        var errorLocator = _page.Locator(ErrorMessageSelector + ", .error-message a.error");
        var errorWaitTask = errorLocator.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

        // Wait for either navigation or error to appear, up to 8s
        await Task.WhenAny(navTask, errorWaitTask, Task.Delay(8000));

        // Give the page a small moment to settle
        await _page.WaitForTimeoutAsync(500);

        // If no visible error was detected, save page HTML to artifacts for debugging
        var errText = await GetErrorMessage();
        if (string.IsNullOrWhiteSpace(errText))
        {
            try
            {
                var html = await _page.ContentAsync();
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, "artifacts", "last_login_page.html");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? ".");
                await System.IO.File.WriteAllTextAsync(path, html);
            }
            catch
            {
                // ignore write failures
            }
        }
    }

    /// <summary>
    /// Gets the error message displayed on the login page
    /// </summary>
    public async Task<string?> GetErrorMessage()
    {
        try
        {
            var locator = _page.Locator(ErrorMessageSelector + ", .error-message a.error");
            if (await locator.CountAsync() == 0)
                return null;

            // Wait briefly for the message text to populate
            try
            {
                await locator.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 2000 });
            }
            catch
            {
                // ignore
            }

            var text = await locator.First.InnerTextAsync();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if error message is visible
    /// </summary>
    public async Task<bool> IsErrorMessageVisible()
    {
        try
        {
            // Wait up to a short timeout for error message to appear
            var locator = _page.Locator(".error-message a.error, .error-message, .alert-danger, .text-danger");
            try
            {
                await locator.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 3000 });
            }
            catch
            {
                // not visible within timeout
            }

            var txt = await GetErrorMessage();
            return !string.IsNullOrWhiteSpace(txt);
        }
        catch
        {
            // Error message not found
            return false;
        }
    }

    /// <summary>
    /// Checks if login form is visible
    /// </summary>
    public async Task<bool> IsLoginFormVisible()
    {
        try
        {
            return await _page.IsVisibleAsync(EmailInputSelector) &&
                   await _page.IsVisibleAsync(PasswordInputSelector);
        }
        catch
        {
            return false;
        }
    }
}
