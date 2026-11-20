using System.Diagnostics;

namespace risk.control.system.E2ETests.Infrastructure;

/// <summary>
/// Starts the web app as a child process for the test run (unless it is already running),
/// waits until the login page responds, and stops it when the run ends.
/// </summary>
public sealed class AppHost : IAsyncDisposable
{
    private const string ProjectFolder = "risk.control.system";
    private const string LaunchProfile = "risk.control.system";

    // Placeholders the app needs to serve pages. Only applied when not already set,
    // so real values from the machine/user environment always win.
    private static readonly Dictionary<string, string> DefaultAppEnvironment = new()
    {
        // Must contain a dot: the portal admin is seeded as "admin@" + WEBSITE_SITE_URL and the login
        // form rejects e-mail domains without one.
        ["WEBSITE_SITE_URL"] = "localhost.com",
        ["WEBSITE_NAME"] = "RiskControl-E2E",
        ["WEBSITE_SITE_LOGO"] = "/img/logo.jpeg",
        ["SAMPLE_MOBILE_AUSTRALIA"] = "432854196",
        ["SAMPLE_MOBILE_INDIA"] = "9956312800",
    };

    // The app opens "Data Source={COUNTRY}_Database.db" in the project folder
    // (StartupExtensions/DatabaseServiceExtension.cs). The connection string cannot be overridden from the
    // environment because Program.cs re-adds the appsettings files last, but COUNTRY can, and it is only used
    // for the file name and seed selection. Any value other than AU/IN seeds both countries, so the AU users
    // exist. Always forcing it guarantees tests never open (or delete) a developer database.
    private const string E2ECountry = "E2E";
    private const string E2EDatabaseFile = E2ECountry + "_Database.db";

    private readonly Process? _process;
    private readonly StreamWriter? _log;

    public string? LogPath { get; }

    /// <summary>False when an already-running app was reused.</summary>
    public bool StartedApp => _process != null;

    private AppHost(Process? process, StreamWriter? log, string? logPath)
    {
        _process = process;
        _log = log;
        LogPath = logPath;
    }

    public static async Task<AppHost> StartAsync(string baseUrl, TimeSpan startupTimeout)
    {
        var loginUrl = $"{baseUrl}/Account/Login";

        if (await IsUpAsync(loginUrl))
        {
            TestContext.Progress.WriteLine($"[E2E] Reusing app already running at {baseUrl}");
            return new AppHost(null, null, null);
        }

        var projectDir = FindProjectDirectory();
        var logPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "app-under-test.log");
        var log = new StreamWriter(logPath, append: false) { AutoFlush = true };

        var psi = new ProcessStartInfo
        {
            FileName = FindDotnet(),
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        // The app is built as a dependency of this project, so skip a second build.
        foreach (var arg in new[] { "run", "--no-build", "--project", projectDir, "--launch-profile", LaunchProfile })
        {
            psi.ArgumentList.Add(arg);
        }

        foreach (var (key, value) in DefaultAppEnvironment)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                psi.Environment[key] = value;
            }
        }

        psi.Environment["COUNTRY"] = E2ECountry;

        // Fake AWS Rekognition/Textract/Transcribe + Google OCR inside the app (Development only),
        // so agent evidence uploads work without cloud credentials.
        psi.Environment["USE_FAKE_EXTERNAL_SERVICES"] = TestSettings.FakeExternalServices ? "true" : "false";
        var dbFile = Path.Combine(projectDir, E2EDatabaseFile);
        DatabaseTemplate? templateToSave = null;
        if (TestSettings.FreshDatabase)
        {
            foreach (var file in new[] { dbFile, dbFile + "-shm", dbFile + "-wal" })
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            var seedEnvironment = new[] { "COUNTRY", "WEBSITE_SITE_URL", "SAMPLE_MOBILE_AUSTRALIA", "SAMPLE_MOBILE_INDIA" }
                .ToDictionary(k => k, k => psi.Environment.TryGetValue(k, out var v) ? v : null);
            var template = new DatabaseTemplate(projectDir, seedEnvironment);
            if (template.IsValid)
            {
                template.CopyTo(dbFile);
                TestContext.Progress.WriteLine($"[E2E] Using fresh database {dbFile} (copied from seeded template)");
            }
            else
            {
                templateToSave = template;
                TestContext.Progress.WriteLine($"[E2E] Using fresh database {dbFile} (seeding on startup: takes a few minutes, cached for later runs)");
            }
        }
        else
        {
            TestContext.Progress.WriteLine($"[E2E] Reusing database {dbFile}");
        }

        TestContext.Progress.WriteLine($"[E2E] Starting app from {projectDir} (log: {logPath})");
        var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start 'dotnet run'.");
        process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (log) log.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (log) log.WriteLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var host = new AppHost(process, log, logPath);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < startupTimeout)
        {
            if (process.HasExited)
            {
                await host.DisposeAsync();
                throw new InvalidOperationException(
                    $"App exited with code {process.ExitCode} before it was ready. See {logPath}");
            }

            if (await IsUpAsync(loginUrl))
            {
                TestContext.Progress.WriteLine($"[E2E] App ready at {baseUrl} after {stopwatch.Elapsed.TotalSeconds:F0}s");
                if (templateToSave != null)
                {
                    try
                    {
                        templateToSave.SaveFrom(dbFile);
                        TestContext.Progress.WriteLine("[E2E] Saved seeded database template for faster later runs");
                    }
                    catch (Exception ex)
                    {
                        TestContext.Progress.WriteLine($"[E2E] Could not save database template: {ex.Message}");
                    }
                }

                return host;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        await host.DisposeAsync();
        throw new TimeoutException(
            $"App did not respond at {loginUrl} within {startupTimeout.TotalSeconds:F0}s. See {logPath}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            TestContext.Progress.WriteLine("[E2E] Stopping app");
            // 'dotnet run' spawns the app as a child process, so kill the whole tree.
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }

        _process?.Dispose();
        if (_log != null)
        {
            await _log.DisposeAsync();
        }
    }

    private static async Task<bool> IsUpAsync(string url)
    {
        using var handler = new HttpClientHandler
        {
            // Local dev certificate.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            using var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// dotnet may not be on PATH (e.g. inside Visual Studio). Prefer the host that is running the tests.
    /// </summary>
    private static string FindDotnet()
    {
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(hostPath) && File.Exists(hostPath))
        {
            return hostPath;
        }

        var programFiles = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        return File.Exists(programFiles) ? programFiles : "dotnet";
    }

    public static string FindProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, ProjectFolder, $"{ProjectFolder}.csproj");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find {ProjectFolder}/{ProjectFolder}.csproj above {AppContext.BaseDirectory}");
    }
}
