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
    }
    public static class PORTAL_ADMIN
    {
        public const string DISPLAY_NAME = "PORTAL_ADMIN";
        public const string CODE = "admin";
        public const string USERNAME = "admin@icheckify.co.in";
        public const string EMAIL = USERNAME;
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
        public const string USERNAME = "creator@" + CANARADOMAIN;
        public const string EMAIL = USERNAME;
        public const string FIRST_NAME = "Reita";
        public const string LAST_NAME = "Cremorne";
        public const string PROFILE_IMAGE = "/img/creator.jpeg";
    }

    public static class ASSIGNER
    {
        public const string DISPLAY_NAME = "ASSIGNER";
        public const string CODE = "assigner";
        public const string USERNAME = "assigner@" + CANARADOMAIN;
        public const string EMAIL = USERNAME;
        public const string FIRST_NAME = "Jesse";
        public const string LAST_NAME = "Trantor";
        public const string PROFILE_IMAGE = "/img/assigner.jpeg";
    }

    public static class ASSESSOR
    {
        public const string DISPLAY_NAME = "ASSESSOR";
        public const string CODE = "assessor";
        public const string USERNAME = "assessor@" + CANARADOMAIN;
        public const string EMAIL = USERNAME;
        public const string FIRST_NAME = "Samy";
        public const string LAST_NAME = "Patrick";
        public const string PROFILE_IMAGE = "/img/assessor.jpeg";
    }

    public static class MANAGER
    {
        public const string DISPLAY_NAME = "MANAGER";
        public const string CODE = "manager";
        public const string USERNAME = "manager@" + CANARADOMAIN;
        public const string EMAIL = USERNAME;
        public const string FIRST_NAME = "Peter";
        public const string LAST_NAME = "Mathew";
        public const string PROFILE_IMAGE = "/img/assigner.jpeg";
    }

    public static class AGENCY_ADMIN
    {
        public const string DISPLAY_NAME = "AGENCY_ADMIN";
        public const string CODE = "admin";
        public const string USERNAME = "admin";
        public const string FIRST_NAME = "Mathew";
        public const string LAST_NAME = "George";
        public const string PROFILE_IMAGE = "/img/agency-admin.jpeg";
    }

    public static class SUPERVISOR
    {
        public const string DISPLAY_NAME = "SUPERVISOR";
        public const string CODE = "supervisor";
        public const string USERNAME = "supervisor";
        public const string FIRST_NAME = "Adam";
        public const string LAST_NAME = "Victor";
        public const string PROFILE_IMAGE = "/img/supervisor.jpeg";
    }

    public static class AGENT
    {
        public const string DISPLAY_NAME = "AGENT";
        public const string CODE = "agent";
        public const string USERNAME = "agent";
        public const string FIRST_NAME = "Denny";
        public const string LAST_NAME = "Travolta";
        public const string PROFILE_IMAGE = "/img/agent.jpeg";
    }

    public static class GUEST
    {
        public const string DISPLAY_NAME = "GUEST";
        public const string CODE = "guest";
        public const string USERNAME = "guest";
        public const string FIRST_NAME = "Zenny";
        public const string LAST_NAME = "Tobbs";
        public const string PROFILE_IMAGE = "/img/assigner.jpeg";
    }
}