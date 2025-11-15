using System.Diagnostics;

namespace risk.control.system.test
{
    [SetUpFixture]
    public class MvcServerFixture
    {
        private static Process? _process;
        public static string BaseUrl { get; private set; } = "https://localhost:5001";

        [OneTimeSetUp]
        public async Task StartServer()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project ../../risk.control.system",
                WorkingDirectory = "../../../../",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            _process = Process.Start(startInfo)!;

            await WaitUntilReady();
        }
        private async Task WaitUntilReady()
        {
            using var client = new HttpClient();
            for (var i = 0; i < 30; i++)
            {
                try
                {
                    var result = await client.GetAsync(BaseUrl);
                    if (result.IsSuccessStatusCode) return;
                }
                catch { }
                Thread.Sleep(1000);
            }

            throw new Exception("MVC application did not start.");
        }
        [OneTimeTearDown]
        public void StopServer()
        {
            if (_process is { HasExited: false })
                _process.Kill(true);
        }
    }
}
