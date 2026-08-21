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

        // Wait for navigation to complete or error to appear
        try
        {
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        catch
        {
            // Ignore timeout, we'll check for errors or redirect manually
        }

        // Give the page time to settle after the submission
        await _page.WaitForTimeoutAsync(1000);
    }

    /// <summary>
    /// Gets the error message displayed on the login page
    /// </summary>
    public async Task<string?> GetErrorMessage()
    {
        try
        {
            return await _page.TextContentAsync(ErrorMessageSelector);
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
            // Check for error link inside error-message div (a.error)
            var errorLink = _page.Locator(".error-message a.error");
            return await errorLink.IsVisibleAsync();
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
