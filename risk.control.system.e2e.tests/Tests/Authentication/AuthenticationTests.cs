using NUnit.Framework;
using risk.control.system.e2e.tests.Fixtures;
using risk.control.system.e2e.tests.Pages;

namespace risk.control.system.e2e.tests.Tests.Authentication;

/// <summary>
/// E2E tests for authentication workflows (login, logout, password reset, etc.)
/// </summary>
[TestFixture(Category = "Authentication")]
public class AuthenticationTests : BaseTest
{
    private LoginPage? _loginPage;
    private DashboardPage? _dashboardPage;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _loginPage = new LoginPage(Page!);
        _dashboardPage = new DashboardPage(Page!);
    }

    /// <summary>
    /// Test: User can navigate to login page
    /// </summary>
    [Test]
    [Order(1)]
    public async Task LoginPage_ShouldLoad_Successfully()
    {
        // Act
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Assert
        Assert.That(await _loginPage!.IsLoginFormVisible(), Is.True, "Login form should be visible");
    }

    /// <summary>
    /// Test: User can login with valid credentials (Portal Admin)
    /// </summary>
    [Test]
    [Order(2)]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Act
        await _loginPage.Login(Config.PortalAdminUser.Email, Config.PortalAdminUser.Password);

        // Assert - Check URL first to see if we redirected from login
        var currentUrl = Page!.Url;
        Assert.That(currentUrl, Does.Not.Contain("/Account/Login"), $"Should have redirected from login page. Current URL: {currentUrl}");
        Assert.That(await _dashboardPage!.IsUserLoggedIn(), Is.True, "User should be logged in");
    }

    /// <summary>
    /// Test: User cannot login with invalid credentials
    /// </summary>
    [Test]
    [Order(3)]
    public async Task Login_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Act
        await _loginPage.Login("invalid@test.com", "invalidpassword");

        // Wait for error to appear
        await Task.Delay(2000);

        // Assert
        Assert.That(await _loginPage.IsErrorMessageVisible(), Is.True, "Error message should be displayed");
    }

    /// <summary>
    /// Test: User can logout successfully
    /// </summary>
    [Test]
    [Order(4)]
    public async Task Logout_WhenLoggedIn_ShouldSucceed()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);
        await _loginPage.Login(Config.PortalAdminUser.Email, Config.PortalAdminUser.Password);
        Assert.That(await _dashboardPage!.IsUserLoggedIn(), Is.True, "User should be logged in");

        // Act
        await _dashboardPage.Logout();

        // Wait for redirect
        await Task.Delay(2000);

        // Assert - Should be redirected to login
        Assert.That(await _loginPage.IsLoginFormVisible(), Is.True, "Should be redirected to login page");
    }

    /// <summary>
    /// Test: User can login with "Remember Me" option
    /// </summary>
    [Test]
    [Order(5)]
    public async Task Login_WithRememberMe_ShouldBePersistent()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Act
        await _loginPage.Login(Config.PortalAdminUser.Email, Config.PortalAdminUser.Password, rememberMe: true);

        // Assert
        Assert.That(await _dashboardPage!.IsUserLoggedIn(), Is.True, "User should be logged in with Remember Me");
    }

    /// <summary>
    /// Test: Empty email field shows validation error
    /// </summary>
    [Test]
    [Order(6)]
    public async Task Login_WithEmptyEmail_ShouldShowValidation()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Act - Try to submit with empty fields
        await _loginPage.Login("", Config.PortalAdminUser.Password);

        // Wait for validation
        await Task.Delay(1000);

        // Assert
        var isFormStillVisible = await _loginPage.IsLoginFormVisible();
        Assert.That(isFormStillVisible, Is.True, "Form should still be visible with validation error");
    }

    /// <summary>
    /// Test: Empty password field shows validation error
    /// </summary>
    [Test]
    [Order(7)]
    public async Task Login_WithEmptyPassword_ShouldShowValidation()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Act
        await _loginPage.Login(Config.PortalAdminUser.Email, "");

        // Wait for validation
        await Task.Delay(1000);

        // Assert
        var isFormStillVisible = await _loginPage.IsLoginFormVisible();
        Assert.That(isFormStillVisible, Is.True, "Form should still be visible with validation error");
    }
}
