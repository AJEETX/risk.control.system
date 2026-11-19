using System.Text.RegularExpressions;
using risk.control.system.E2ETests.Infrastructure;

namespace risk.control.system.E2ETests.Tests;

[TestFixture]
public class LoginTests : E2ETestBase
{
    [Test]
    public async Task LoginPage_Renders()
    {
        await Page.GotoAsync("/Account/Login");

        await Expect(Page.Locator("#login-form")).ToBeVisibleAsync();
        await Expect(Page.Locator("#email")).ToBeVisibleAsync();
        await Expect(Page.Locator("#Password")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("logintest")).ToBeEnabledAsync();
    }

    [Test]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        await LoginAsync(TestSettings.ManagerEmail, "wrong-password-123!");

        await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login", RegexOptions.IgnoreCase));
        await Expect(Page.Locator(".error-message .error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Login_AsManager_LandsOnDashboard()
    {
        await LoginAsManagerAsync();

        await Expect(Page).Not.ToHaveURLAsync(new Regex("/Account/Login", RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task ProtectedPage_WhenAnonymous_RedirectsToLogin()
    {
        await Page.GotoAsync("/ActiveAgency/Agencies");

        await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login", RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task Logout_ReturnsToLogin_AndBlocksProtectedPages()
    {
        await LoginAsManagerAsync();

        await Page.GotoAsync("/Account/Logout");
        await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login", RegexOptions.IgnoreCase));

        await Page.GotoAsync("/ActiveAgency/Agencies");
        await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login", RegexOptions.IgnoreCase));
    }
}
