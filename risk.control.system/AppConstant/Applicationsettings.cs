using risk.control.system.Helpers;

namespace risk.control.system.AppConstant
{
    public static class Applicationsettings
    {
        public const double ACTIVE_USER_TIMESPAN = 10;
        public const string TestingData = "R1$kcontrol!";
        public const string PERMISSION = "Permission";
        public static readonly string ADMIN_MOBILE = "404723089";

        //public static readonly string PORTAL_ADMIN_MOBILE = "432854196";
        public static readonly string SAMPLE_MOBILE_INDIA = EnvHelper.Get("SAMPLE_MOBILE_INDIA")!;

        public static readonly string SAMPLE_MOBILE_AUSTRALIA = EnvHelper.Get("SAMPLE_MOBILE_AUSTRALIA")!;

        public static readonly string AGENT_MOBILE = EnvHelper.Get("AGENT_MOBILE")!;
        public static readonly string HEXdATA = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static readonly string ValidIssuer = EnvHelper.Get("WEBSITE_NAME")!;
        public static readonly string ValidAudience = "insurance";
        public static readonly int TokenTimeoutMinutes = 5;

        //public const string APP_URL = "https://ickeckify-apk.s3.ap-southeast-2.amazonaws.com/demo/app-release.apk";
        public static string APP_URL = EnvHelper.Get("APP_URL")!;

        //WEBSITE SETTINGS

        public static string WEBSITE_NAME = EnvHelper.Get("WEBSITE_NAME")!;
        public static string WEBSITE_SITE_URL = EnvHelper.Get("WEBSITE_SITE_URL")!;
        public static string WEBSITE_SITE_LOGO = EnvHelper.Get("WEBSITE_SITE_LOGO")!;
        public static readonly string FTP_SITE = "ftp://ftp.drivehq.com/holosync/";
        public static readonly string FTP_SITE_LOG = "its.aby@email.com";
        public static readonly string FTP_SITE_DATA = "C0##ect10n";
        public static readonly string REVERRSE_GEOCODING = "f2a54c0ec9ba4dfdbd450116509c6313";

        public static readonly string NO_POLICY_IMAGE = "/img/no-policy.jpg";
        public static readonly string POLICY_BLANK_IMAGE = "/img/blank-document.png";
        public static readonly string NO_AUDIO = "/img/no-audio.png";
        public static readonly string NO_VIDEO = "/img/no-video.png";
        public static readonly string MARKER_FLAG = "/img/beachflag.png";

        public const string ALL_DISTRICT = "All Districts";
        public const string ALL_DISTRICT_CODE = "-1";

        public const string CANCEL = "Cancel";
        public const string CREATE = "Add";
        public const string EDIT = "Edit";
        public const string DELETE = "Delete";
        public const string VIEW = "View";
        public const string DETAILS = "Details";

        // LABELS / ACTIONS
        public const string CASE_ENABLER = "Reason To Verify";

        public const string COST_CENTRE = "Cost centre";
        public const string PINCODE = "Pincode";
        public const string ROLE = "Role";
        public const string STATE = "State";
        public const string USER = "User";
        public const string COMPANY_USERS = "Manage Users";
        public const string VENDOR = "Agency";
        public const string MANAGE_USERS = "Manage users";
        public const string MANAGE_SERVICE = "Manage service";

        // MENUS / SUBMENUS

        public const string DASHBOARD = "DASHBOARD";
        public const string INVESTIGATION_CLAIM = "CLAIMS";
        public const string COMPANY_SETTING = "COMPANY SETTING";
        public const string INVESTIGATION_UNDERWRITINGS = "Underwriting";
        public const string USER_ROLES = "USERS/ROLES";
        public const string ADMIN_SETTING = "ADMIN SETTING";
        public const string GLOBAL_SETTINGS = "Global-settings";
        public const string GENERAL_SETUP = "GENERAL SETUP";
        public const string AUDIT_LOGS = "Audit Log";

        public const string CURRENT_PINCODE = "3131";

        public const string CURRENT_PINCODE2 = "3130";
        public const string CURRENT_PINCODE3 = "3133";
        public const string CURRENT_PINCODE4 = "3150";
        public const string CURRENT_PINCODE5 = "3125";
        public const string CURRENT_PINCODE6 = "3124";

        //public const string CANARADOMAIN = "canarahsbc.com";
        //public const string CANARALOGO = "/img/chl.jpg";

        //public const string ALLIANZ = "Allianz";
        //public const string ALLIANZ_DOMAIN = "allianz.com";
        //public const string ALLIANZ_LOGO = "/img/allianz.png";

        //public const string TATA = "Tata Aia";
        //public const string TATA_DOMAIN = "tataaia.com";
        //public const string TATA_LOGO = "/img/tata_logo.png";

        //public const string HDFC = "Hdfc Life";
        //public const string HDFCDOMAIN = "hdfclife.com";
        //public const string HDFCLOGO = "/img/hdfc.jpg";

        //public const string AGENCY1NAME = "Checker";
        //public const string AGENCY1DOMAIN = "checker.com";
        //public const string AGENCY1PHOTO = "/img/checker.png";

        //public const string AGENCY2NAME = "Verify";
        //public const string AGENCY2DOMAIN = "verify.com";
        //public const string AGENCY2PHOTO = "/img/verify.png";

        //public const string AGENCY3NAME = "Investigate";
        //public const string AGENCY3DOMAIN = "investigate.com";
        //public const string AGENCY3PHOTO = "/img/investigate.png";

        //public const string AGENCY4NAME = "Proper";
        //public const string AGENCY4DOMAIN = "proper.com";
        //public const string AGENCY4PHOTO = "/img/proper.png";

        //public const string AGENCY5NAME = "Honest";
        //public const string AGENCY5DOMAIN = "honest.com";
        //public const string AGENCY5PHOTO = "/img/honest.png";

        //public const string AGENCY6NAME = "Nicer";
        //public const string AGENCY6DOMAIN = "nicer.com";
        //public const string AGENCY6PHOTO = "/img/nicer.png";

        public const string NO_IMAGE = "/img/no-image.png";
        public const string AUDIO_UPLOAD_IMAGE = "/img/upload-audio.png";
        public const string VIDEO_UPLOAD_IMAGE = "/img/upload-video.png";
        public const string USER_PHOTO = "/img/user.png";
        public const string NO_USER = "/img/no-user.png";
        public const string NO_MAP = "/img/no-map.jpeg";
        public const string MAP_MARKER = "/img/map-marker-icon.png";
    }
}