using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.AppConstant
{
    public enum AppRoles
    {
        PORTAL_ADMIN,
        COMPANY_ADMIN,
        CREATOR,
        ASSESSOR,
        MANAGER,
        AGENCY_ADMIN,
        SUPERVISOR,
        AGENT,
        GUEST
    }

    public static class RoleGroups
    {
        public static string[] AllRoles =
            {
            PORTAL_ADMIN.DISPLAY_NAME,
            COMPANY_ADMIN.DISPLAY_NAME,
            CREATOR.DISPLAY_NAME,
            ASSESSOR.DISPLAY_NAME,
            MANAGER.DISPLAY_NAME,
            AGENCY_ADMIN.DISPLAY_NAME,
            SUPERVISOR.DISPLAY_NAME,
            AGENT.DISPLAY_NAME,
                GUEST.DISPLAY_NAME
        };

        public static readonly string[] CompanyRoles =
        {
            PORTAL_ADMIN.DISPLAY_NAME,
           COMPANY_ADMIN.DISPLAY_NAME,
           CREATOR.DISPLAY_NAME,
            MANAGER.DISPLAY_NAME,
            ASSESSOR.DISPLAY_NAME
        };

        public static readonly string[] AgencyRoles =
        {
            AGENCY_ADMIN.DISPLAY_NAME,
            SUPERVISOR.DISPLAY_NAME,
            AGENT.DISPLAY_NAME
        };

        public static AppRoles[] CompanyAppRoles = new[]
                {
                    AppRoles.COMPANY_ADMIN,
                    AppRoles.CREATOR,
                    AppRoles.MANAGER,
                    AppRoles.ASSESSOR
                };

        public static AppRoles[] AgencyAppRoles = new[]
                {
                    AppRoles.AGENCY_ADMIN,
                    AppRoles.SUPERVISOR,
                    AppRoles.AGENT
                };
    }

    public static class PORTAL_ADMIN
    {
        public const string DISPLAY_NAME = "PORTAL_ADMIN";
        public const string CODE = "admin";
        public static string USERNAME = "admin@" + WEBSITE_SITE_URL;
        public static string EMAIL = USERNAME;
        public const string FIRST_NAME = "Simmy";
        public const string LAST_NAME = "Collins";
        public const string PROFILE_IMAGE = "/img/portal-admin.jpeg";
    }

    public static class COMPANY_ADMIN
    {
        public const string DISPLAY_NAME = "COMPANY_ADMIN";
        public const string CODE = "admin";
        public const string FIRST_NAME = "Andy";
        public const string LAST_NAME = "Murrey";
        public const string PROFILE_IMAGE = "/img/company-admin.jpeg";
    }

    public static class CREATOR
    {
        public const string DISPLAY_NAME = "CREATOR";
        public const string CODE = "creator";
        public const string FIRST_NAME = "Reita";
        public const string LAST_NAME = "Cremorne";
        public const string PROFILE_IMAGE = "/img/creator.jpeg";
    }

    public static class ASSESSOR
    {
        public const string DISPLAY_NAME = "ASSESSOR";
        public const string CODE = "assessor";
        public const string FIRST_NAME = "Simone";
        public const string LAST_NAME = "Patrick";
        public const string PROFILE_IMAGE = "/img/assessor.jpeg";
    }

    public static class MANAGER
    {
        public const string DISPLAY_NAME = "MANAGER";
        public const string CODE = "manager";
        public const string FIRST_NAME = "Keira";
        public const string LAST_NAME = "Mathew";
        public const string PROFILE_IMAGE = "/img/manager.jpeg";
    }

    public static class AGENCY_ADMIN
    {
        public const string DISPLAY_NAME = "AGENCY_ADMIN";
        public const string CODE = "admin";
        public const string FIRST_NAME = "Patricia";
        public const string LAST_NAME = "George";
        public const string PROFILE_IMAGE = "/img/agency-admin.jpeg";
    }

    public static class SUPERVISOR
    {
        public const string DISPLAY_NAME = "SUPERVISOR";
        public const string CODE = "supervisor";
        public const string FIRST_NAME = "Alicia";
        public const string LAST_NAME = "Victor";
        public const string PROFILE_IMAGE = "/img/supervisor.jpeg";
    }

    public static class AGENT
    {
        public const string DISPLAY_NAME = "AGENT";
        public const string CODE = "agent";
        public const string FIRST_NAME = "Denny";
        public const string LAST_NAME = "Travolta";
        public const string PROFILE_IMAGE = "/img/agent.jpeg";
    }

    public static class GUEST
    {
        public const string DISPLAY_NAME = "GUEST";
    }
}