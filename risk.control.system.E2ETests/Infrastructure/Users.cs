namespace risk.control.system.E2ETests.Infrastructure;

public enum Role
{
    PortalAdmin,
    CompanyAdmin,
    Manager,
    Creator,
    Assessor,
    AgencyAdmin,
    Agent,
}

/// <summary>
/// Seeded users from the Australian seed (COUNTRY=AU). All share <see cref="TestSettings.Password"/>.
/// Company domain "insurer.com"; "checker.com" is the empanelled agency, "crucible.com" is not.
/// No supervisor is seeded (SupervisorSeed is commented out in Seeds/AgencyUserSeed.cs).
/// </summary>
public static class Users
{
    public static string EmailFor(Role role) => role switch
    {
        // "admin@" + WEBSITE_SITE_URL; AppHost passes WEBSITE_SITE_URL=localhost.com.
        Role.PortalAdmin => Env("E2E_PORTAL_ADMIN_EMAIL", "admin@localhost.com"),
        Role.CompanyAdmin => "admin@insurer.com",
        Role.Manager => TestSettings.ManagerEmail,
        Role.Creator => "creator@insurer.com",
        Role.Assessor => "assessor@insurer.com",
        Role.AgencyAdmin => "admin@checker.com",
        Role.Agent => "agent@checker.com",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static string Env(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
