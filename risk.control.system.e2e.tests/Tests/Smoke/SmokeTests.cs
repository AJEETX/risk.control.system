using NUnit.Framework;
using risk.control.system.e2e.tests.Fixtures;
using risk.control.system.e2e.tests.Pages;

namespace risk.control.system.e2e.tests.Tests.Smoke;

/// <summary>
/// Smoke tests - Quick tests to verify basic application functionality
/// </summary>
[TestFixture(Category = "Smoke")]
public class SmokeTests : BaseTest
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
    /// Test: Application is accessible
    /// </summary>
    [Test]
    [Order(1)]
    public async Task App_ShouldBeAccessible()
    {
        // Act
        await NavigateToHome();

        // Assert
        Assert.That(Page!.Url, Is.Not.Empty, "Application should be accessible");
    }

    /// <summary>
    /// Test: Login page is accessible
    /// </summary>
    [Test]
    [Order(2)]
    public async Task LoginPage_ShouldBeAccessible()
    {
        // Act
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Assert
        Assert.That(await _loginPage.IsLoginFormVisible(), Is.True, "Login page should be accessible");
    }

    /// <summary>
    /// Test: User can complete login workflow
    /// </summary>
    [Test]
    [Order(3)]
    public async Task LoginWorkflow_ShouldComplete()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Act
        await _loginPage.Login(Config.PortalAdminUser.Email, Config.PortalAdminUser.Password);

        // Assert
        Assert.That(await _dashboardPage!.IsUserLoggedIn(), Is.True, "User should be logged in");
    }

    /// <summary>
    /// Test: Application responds to user interactions
    /// </summary>
    [Test]
    [Order(4)]
    public async Task App_ShouldRespondToInteractions()
    {
        // Arrange
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Act
        await FillField("input[name='Email']", Config.PortalAdminUser.Email);
        var emailValue = await Page!.InputValueAsync("input[name='Email']");

        // Assert
        Assert.That(emailValue, Does.Contain(Config.PortalAdminUser.Email), 
            "Application should respond to input interactions");
    }

    /// <summary>
    /// Test: Page loads without critical errors
    /// </summary>
    [Test]
    [Order(5)]
    public async Task App_ShouldLoadWithoutErrors()
    {
        var criticalErrors = new List<string>();

        // Capture page errors
        Page!.PageError += (_, error) =>
        {
            criticalErrors.Add(error?.ToString() ?? "Unknown error");
        };

        // Act
        await NavigateToHome();
        await Task.Delay(2000);

        // Assert
        Assert.That(criticalErrors, Is.Empty, "Page should load without critical errors");
    }

    /// <summary>
    /// Test: Application has proper HTTP response codes
    /// </summary>
    [Test]
    [Order(6)]
    public async Task App_ShouldHaveValidHttpStatus()
    {
        var httpErrors = new List<int>();

        // Capture failed responses
        Page!.Response += (_, response) =>
        {
            if (response.Status >= 500)
            {
                httpErrors.Add(response.Status);
            }
        };

        // Act
        await NavigateToHome();
        await _loginPage!.NavigateToLogin(Config.BaseUrl);

        // Assert
        Assert.That(httpErrors, Is.Empty, "Application should return valid HTTP status codes");
    }

    /// <summary>
    /// Test: Static assets load correctly
    /// </summary>
    [Test]
    [Order(7)]
    public async Task App_StaticAssets_ShouldLoad()
    {
        var assetErrors = new List<string>();

        Page!.Response += (_, response) =>
        {
            if (response.Url.EndsWith(".css") || response.Url.EndsWith(".js"))
            {
                if (response.Status >= 400)
                {
                    assetErrors.Add($"{response.Url} ({response.Status})");
                }
            }
        };

        // Act
        await NavigateToHome();
        await Task.Delay(2000);

        // Assert
        Assert.That(assetErrors, Is.Empty, "All static assets should load successfully");
    }

    /// <summary>
    /// Test: Page performance - initial load time
    /// </summary>
    [Test]
    [Order(8)]
    public async Task App_ShouldLoadInReasonableTime()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        await NavigateToHome();
        var loadTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.That(loadTime.TotalSeconds, Is.LessThan(30), "Page should load within 30 seconds");
    }
}
