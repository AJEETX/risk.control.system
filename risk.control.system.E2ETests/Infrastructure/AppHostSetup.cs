namespace risk.control.system.E2ETests;

using risk.control.system.E2ETests.Infrastructure;

/// <summary>
/// Runs once for every test in this assembly (namespace root): makes sure the app is up
/// before the first test and stops it after the last one if this run started it.
/// Set E2E_START_APP=false to never start the app (e.g. when testing a deployed environment).
/// </summary>
[SetUpFixture]
public class AppHostSetup
{
    private AppHost? _host;

    /// <summary>True when this run started the app on a brand-new database, so data-changing tests are safe.</summary>
    public static bool StartedWithFreshDatabase { get; private set; }

    [OneTimeSetUp]
    public async Task StartAppAsync()
    {
        if (!TestSettings.StartApp)
        {
            TestContext.Progress.WriteLine("[E2E] E2E_START_APP=false, expecting the app to be running already");
            return;
        }

        _host = await AppHost.StartAsync(TestSettings.BaseUrl, TestSettings.AppStartupTimeout);
        StartedWithFreshDatabase = _host.StartedApp && TestSettings.FreshDatabase;
    }

    [OneTimeTearDown]
    public async Task StopAppAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }
}
