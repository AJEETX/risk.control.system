namespace risk.control.system.e2e.tests.Configuration;

/// <summary>
/// Configuration settings for E2E tests
/// </summary>
public class TestConfiguration
{
    /// <summary>
    /// Base URL of the application under test
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5001"; // Matches the app's local launch profile

    /// <summary>
    /// Timeout for page navigation in milliseconds
    /// </summary>
    public int NavigationTimeout { get; set; } = 30000;

    /// <summary>
    /// Timeout for element operations in milliseconds
    /// </summary>
    public int ActionTimeout { get; set; } = 10000;

    /// <summary>
    /// Whether to run tests headless
    /// </summary>
    public bool Headless { get; set; } = false;

    /// <summary>
    /// Browser type to use (chromium, firefox, webkit)
    /// </summary>
    public string BrowserType { get; set; } = "chromium";

    /// <summary>
    /// Whether to capture screenshots on failure
    /// </summary>
    public bool CaptureScreenshots { get; set; } = true;

    /// <summary>
    /// Whether to record videos of test runs
    /// </summary>
    public bool RecordVideos { get; set; } = false;

    /// <summary>
    /// Path for screenshots and videos
    /// </summary>
    public string ArtifactsPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "artifacts");

    /// <summary>
    /// Test user credentials - PortalAdmin role
    /// </summary>
    public TestUser PortalAdminUser { get; set; } = new TestUser
    {
        // The app seeds a portal admin with this default email/password on first startup.
        Email = "admin@icheckify.biz",
        Password = "R1$kcontrol!"
    };

    /// <summary>
    /// Test user credentials - Company role
    /// </summary>
    public TestUser CompanyUser { get; set; } = new TestUser
    {
        Email = "company@test.com",
        Password = "TestPassword123!"
    };

    /// <summary>
    /// Test user credentials - Agency role
    /// </summary>
    public TestUser AgencyUser { get; set; } = new TestUser
    {
        Email = "agency@test.com",
        Password = "TestPassword123!"
    };
}

/// <summary>
/// Test user model
/// </summary>
public class TestUser
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
