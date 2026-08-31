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
            return rawVersion;
            //var rawVersion = attribute.InformationalVersion;
            //int dateIdx = rawVersion.IndexOf('+');
            //string dateStr = dateIdx != -1 ? rawVersion[(dateIdx + 1)..] : rawVersion;

            //if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var buildUtc))
            //{
            //    // Converts UTC to server local time and formats to 24-Oct-2026 14:30
            //    return buildUtc.ToLocalTime().ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture);
            //}
        }

        return "Local Build";
    }
}