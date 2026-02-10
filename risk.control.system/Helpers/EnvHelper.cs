namespace risk.control.system.Helpers
{
    public static class EnvHelper
    {
        public static string? Get(string key)
        {
            // 1️⃣ Try environment variable first (launchSettings, docker env)
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            // 2️⃣ Try Docker secret file
            var secretPath = $"/run/secrets/{key}";
            if (File.Exists(secretPath))
                return File.ReadAllText(secretPath).Trim();

            return null;
        }
    }
}