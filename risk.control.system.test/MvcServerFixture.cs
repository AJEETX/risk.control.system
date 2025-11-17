using System.Diagnostics;

namespace risk.control.system.test
{
    [SetUpFixture]
    public class MvcServerFixture
    {
        private static Process? _process;

        public static string BaseUrl { get; private set; } = Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://localhost:5001";
        private static string DllPath => Path.Combine(PublishFolder, "risk.control.system.dll");
        private static string ProjectPath => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../risk.control.system"));
        private static string PublishFolder => Path.Combine(ProjectPath, "risk.control.system", "bin", "Release", "net8.0", "publish");

        [OneTimeSetUp]
        public async Task StartServer()
        {
            // STEP 1: Publish the MVC project
            var publish = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{ProjectPath}\" -c Release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            publish.WaitForExit();
            if (publish.ExitCode != 0)
            {
                throw new Exception("dotnet publish failed:\n" +
                    publish.StandardError.ReadToEnd());
            }

            if (!Directory.Exists(PublishFolder))
            {
                throw new DirectoryNotFoundException($"Publish folder NOT FOUND: {PublishFolder}");
            }

            if (!File.Exists(DllPath))
            {
                throw new FileNotFoundException($"DLL NOT FOUND: {DllPath}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{DllPath}\" --urls={BaseUrl}",
                WorkingDirectory = PublishFolder,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Force environment (CI/CD runs as Production by default)
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

            _process = new Process();
            _process.StartInfo = startInfo;

            // Read logs
            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    Console.WriteLine("[MVC OUT] " + e.Data);
            };

            _process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    Console.WriteLine("[MVC ERR] " + e.Data);
            };

            Console.WriteLine("Starting MVC process…");
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Console.WriteLine("MVC process started. PID = " + _process.Id);

            await WaitUntilReady();
        }

        private async Task WaitUntilReady()
        {
            Console.WriteLine("Waiting for MVC to become ready: " + BaseUrl);
            await Task.Delay(10000);

            using var client = new HttpClient();

            for (var i = 0; i < 50; i++) // 40 seconds max
            {
                try
                {
                    var response = await client.GetAsync(BaseUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("MVC is ready!");
                        return;
                    }
                }
                catch { }

                Console.Write(".");
                await Task.Delay(1000);
            }

            throw new Exception("MVC application did NOT start in time.");
        }

        [OneTimeTearDown]
        public void StopServer()
        {
            if (_process is { HasExited: false })
            {
                Console.WriteLine("Stopping MVC process…");
                _process.Kill(true);
            }
            _process?.Dispose();
        }
    }
}
