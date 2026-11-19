namespace risk.control.system.E2ETests.Infrastructure;

/// <summary>
/// Settings for the E2E run. Every value can be overridden with an environment variable,
/// so the same tests run against local, docker or a deployed environment.
/// Defaults match the seeded Australian data (see Seeds/AustraliaSeed.cs) and launchSettings.json.
/// </summary>
public static class TestSettings
{
    public static string BaseUrl => Get("E2E_BASE_URL", "https://localhost:56529").TrimEnd('/');

    public static string Password => Get("E2E_PASSWORD", "R1$kcontrol!");

    public static string ManagerEmail => Get("E2E_MANAGER_EMAIL", "manager@insurer.com");

    /// <summary>Domain of an agency that is seeded as empanelled for the insurer.</summary>
    public static string ExpectedAgencyDomain => Get("E2E_EXPECTED_AGENCY", "checker.com");

    /// <summary>Start the app automatically if it is not already running.</summary>
    public static bool StartApp => !string.Equals(Get("E2E_START_APP", "true"), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// When this run starts the app, give it a brand-new seeded database (tests that create cases need one).
    /// Ignored when an already-running app is reused.
    /// </summary>
    public static bool FreshDatabase => !string.Equals(Get("E2E_FRESH_DB", "true"), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>Start the app with fake AI/cloud services (USE_FAKE_EXTERNAL_SERVICES). Default true.</summary>
    public static bool FakeExternalServices => !string.Equals(Get("E2E_FAKE_EXTERNAL", "true"), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>Ignore the cached seeded database and seed from scratch (also refreshes the cache).</summary>
    public static bool RefreshTemplate => string.Equals(Get("E2E_REFRESH_TEMPLATE", "false"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>First start on an empty database seeds a lot of data, so allow plenty of time.</summary>
    public static TimeSpan AppStartupTimeout => TimeSpan.FromSeconds(int.Parse(Get("E2E_APP_STARTUP_TIMEOUT_SEC", "600")));

    private static string Get(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
