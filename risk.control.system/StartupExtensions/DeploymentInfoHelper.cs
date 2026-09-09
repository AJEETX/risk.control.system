using System.Globalization;
using System.Reflection;

namespace risk.control.system.StartupExtensions;

public static class DeploymentInfoHelper
{
    public static string GetServerLocalDeploymentTime()
    {
        var attribute = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (attribute?.InformationalVersion != null)
        {
            var rawVersion = attribute.InformationalVersion;

            // Extract the date/time after the '+' symbol
            var parts = rawVersion.Split('+');
            if (parts.Length > 1 && DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime deploymentTime))
            {
                TimeSpan duration = DateTime.UtcNow - deploymentTime.ToUniversalTime();

                return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m";
            }
        }

        return "Local Build";
    }
}