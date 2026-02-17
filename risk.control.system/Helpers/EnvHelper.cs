namespace risk.control.system.Helpers
{
    public static class EnvHelper
    {
        // Pass in IConfiguration to access appsettings.json
        public static string? Get(string key, IConfiguration? config = null)
        {
            // 1️⃣ Try appsettings.json first (via IConfiguration)
            if (config != null)
            {
                var configValue = config[key];
                if (!string.IsNullOrWhiteSpace(configValue))
                    return configValue;
            }

            // 2️⃣ Try environment variable (Portal settings, launchSettings)
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue;

            // 3️⃣ Try Docker secret file
            var secretPath = $"/run/secrets/{key}";
            if (File.Exists(secretPath))
                return File.ReadAllText(secretPath).Trim();

            return null;
        }
    }
}