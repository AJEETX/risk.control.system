namespace risk.control.system.AppConstant
{
    public static class Applicationsettings
    {
        public const double ACTIVE_USER_TIMESPAN = 10;
        public const string TestingData = "R1$kcontrol!";
        public const string PERMISSION = "Permission";
        public static readonly string ADMIN_MOBILE = "404723089";
        //public static readonly string PORTAL_ADMIN_MOBILE = "432854196";
        public static readonly string SAMPLE_MOBILE_INDIA = Environment.GetEnvironmentVariable("SAMPLE_MOBILE_INDIA");
        public static readonly string SAMPLE_MOBILE_AUSTRALIA = Environment.GetEnvironmentVariable("SAMPLE_MOBILE_AUSTRALIA");

        public static readonly string USER_MOBILE = "400000000";
        public static readonly string HEXdATA = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static readonly string ValidIssuer = "icheckify";
        public static readonly string ValidAudience = "canarahdfclife";
        public static readonly int TokenTimeoutMinutes = 5;
        //public const string APP_URL = "https://ickeckify-apk.s3.ap-southeast-2.amazonaws.com/demo/app-release.apk";
        public static string APP_URL = Environment.GetEnvironmentVariable("APP_URL");
        public const string AZURE_APP_DEMO_URL = "icheckify-demo.azurewebsites.net";
        public const string AZURE_APP_URL = "icheckify.azurewebsites.net";

        //WEBSITE SETTINGS

        public const string WEBSITE_SITE_MENU_BRAND = "aby";
        public const string WEBSITE_COMPANY_MENU_BRAND = "iCheckify";
        public const string WEBSITE_TITLE = "iCheckify";
        public const string WEBSITE_SITE_MENU_LOGO = "/img/logo.png";
        public const string WEBSITE_SITE_DESIGNER = "its aby";
        public static readonly string FTP_SITE = "ftp://ftp.drivehq.com/holosync/";
        public static readonly string FTP_SITE_LOG = "its.aby@email.com";
        public static readonly string FTP_SITE_DATA = "C0##ect10n";
        public static readonly string REVERRSE_GEOCODING = "f2a54c0ec9ba4dfdbd450116509c6313";

        public static readonly string DEFAULT_POLICY_IMAGE = "policy-design.png";
        public static readonly string NO_POLICY_IMAGE = "/img/no-policy.jpg";
        public static readonly string POLICY_BLANK_IMAGE = "/img/blank-document.png";
        public static readonly string NO_PHOTO_IMAGE = "/img/no-photo.png";
        public static readonly string NO_AUDIO = "/img/no-audio.png";
        public static readonly string NO_VIDEO = "/img/no-video.png";
        public static readonly string MARKER_FLAG = "/images/beachflag.png";

        public const string ALL_DISTRICT = "All Districts";
        public const string ALL_DISTRICT_CODE = "-1";

        public const string ALL_PINCODE = "All PinCodes";
        public const string ALL_PINCODE_CODE = "-1";

        //LOGIN
        public const string WEBSITE_LOGIN = "Log in: iCheckify";

        public const string WEBSITE_SIGNIN = "Sign in";

        // BUTTONS / ACTIONS

        public const string BACK = "Back";
        public const string CANCEL = "Cancel";
        public const string RESET = "Reset";
        public const string APPROVE = "Approve";
        public const string SEARCH = "Search";
        public const string CREATE = "Add";
        public const string EDIT = "Edit";
        public const string DELETE = "Delete";
        public const string ALLOCATE = "Allocate";
        public const string VIEW = "View";
        public const string DETAILS = "Details";
        public const string VERIFY_LOCATIONS = "Locations to verify";

        // LABELS / ACTIONS
        public const string CLIENT_COMPANIY_PROFILE = "Company Profile";

        public const string CLIENT_COMPANIES = "Company";
        public const string CLIENT_COMPANY = "Company";
        public const string COUNTRY = "Country";
        public const string DISTRICT = "District";
        public const string CASE_STATUS = "Case status";
        public const string SERVICE = "Service";
        public const string SERVICES = "Services";
        public const string VERIFY_LOCATION = "Verify location";
        public const string CUSTOMER_VERIFY_LOCATION = "Customer Verification location detail";
        public const string INVESTIGATION_SERVICE = "Type of service";
        public const string BENEFICIARY = "Beneficiary Relation";
        public const string CASE_ENABLER = "Reason To Verify";
        public const string COST_CENTRE = "Cost centre";
        public const string CASE_OUTCOME = "Case outcome";
        public const string CASE_SUBSTATUS = "Case sub status";
        public const string LINE_OF_BUSINESS = "Line of business";
        public const string PINCODE = "Pincode";
        public const string RECORD = "Record";
        public const string ROLE = "Role";
        public const string STATE = "State";
        public const string USER = "User";
        public const string COMPANY_USERS = "Manage Users";
        public const string VENDOR_USERS = "Agency Users";
        public const string VENDOR = "Agency";
        public const string VENDORS = "Agency";
        public const string MANAGE_VENDORS = "Manage Agencys";
        public const string EMPANELLED_VENDORS = "Empanelled Agency";
        public const string DEPANELLED_VENDORS = "Depanelled Agency";
        public const string MANAGE_VENDOR = "Manage Agency";
        public const string MANAGE_CLIENT_COMPANY = "Manage client company";
        public const string CASE_PROFILE = "Case Profile";
        public const string VERIFICTION_LOCATIONS = "Locations to verify";
        public const string EMPANEL = "Go to Empanel Vendors";
        public const string SELECT_TO_EMPANEL = "Select to Empanel Vendors";
        public const string BROADCAST = "Broadcast";
        public const string ASSIGN = "Assign";
        public const string ALLOCATE_TO_VENDOR = "Allocate To Agency";
        public const string SUBMIT_REPORT = "Submit Report";
        public const string CREATE_SELECTED_CASES = "Create selected cases";

        public const string MANAGE_COMPANY_VENDOR = "Manage company Agency";
        public const string MANAGE_VENDOR_PROFILE = "Manage Agency Profile";
        public const string SELECT_VERIFICATION_LOCATION_TO_ALLOCATE_TO_VENDOR = "Allocate to Agency";
        public const string SELECT_CASE_TO_ALLOCATE_TO_VENDOR = "ALLOCATE";
        public const string SELECT_CASE_INVESTIGATE = "Investigate";
        public const string SELECT_CASE_REPORT = "Verify report";
        public const string SUBMIT_CASE_INVESTIGATE = "Submit";
        public const string SUBMIT_CASE_TO_COMPANY = "Submit to company";
        public const string SELECT_CASE_TO_ASSIGN_TO_AGENT = "Assign to agent";
        public const string AGENTS = "Agents";
        public const string SELECT_CASE_TO_START = "Select Claim case to Verify";
        public const string AVAILABLE_VENDORS = "Available Agency";
        public const string DEPANEL_VENDORS = "Go to Depanel Agency";
        public const string SELECT_TO_DEPANEL_VENDORS = "Select to Depanel Agency";
        public const string MANAGE_VENDOR_USER = "Manage Agency user";
        public const string EDIT_VENDOR = "Edit Agency";
        public const string PROFILE = "Profile";
        public const string MANAGE_USERS = "Manage users";
        public const string AUDIT_LOG = "Audit log";

        public const string MANAGE_SERVICE = "Manage service";
        public const string ADD_SERVICE = "Add service";
        public const string MULTIPLE_UPLOAD = "Multi-Upload";
        public const string PINCODE_UPLOAD = "Pincode upload";
        public const string PINCODE_SAMPLE = "Upload sample file to upload in csv format";
        public const string UPLOADED_SAMPLE = "Uploaded Records";
        public const string NO_RECORDS_FOUND = "No Records Found";

        public const string UPLOAD = "Upload Sample";
        public const string UPLOAD_CASE = "Upload";
        public const string UPLOADED_CASES = "Uploaded Cases";
        public const string SAMPLE_PINCODE_FILE_TYPE = "csv";
        public const string DOWNLOAD = "Download";
        public static string SAMPLE_PINCODE_FILE = $"Sample csv file for pincode upload";

        // MENUS / SUBMENUS

        public const string DASHBOARD = "DASHBOARD";
        public const string INVESTIGATION_CLAIM = "CLAIMS";
        public const string INVESTIGATION_CASE_SETTINGS = "CASE SETTINGS";
        public const string COMPANY_SETTINGS = "COMPANY SETTINGS";
        public const string INVESTIGATION_CASE_STATUS = "Case status";
        public const string INVESTIGATION_CLAIMS_CASE = "Claim";
        public const string INVESTIGATION_CLAIMS_CASE_READY_TO_ASSIGN = "Claim (ready to assign)";
        public const string INVESTIGATION_CLAIMS_CASE_DRAFT = "Claim (draft)";
        public const string INVESTIGATION_CLAIMS_OPEN = "Claim (open)";
        public const string INVESTIGATION_CLAIMS_SUBMITTED_BY_AGENT = "Claim case (report)";
        public const string INVESTIGATION_CLAIMS_CASE_DETAILS = "Claim case details";
        public const string CUSTOMER_DETAILS = "Customer details";
        public const string BENEFICIARY_DETAILS = "Beneficiary details";
        public const string MAILBOX = "MAILBOX";
        public const string INBOX = "Inbox";
        public const string COMPOSE = "Compose";
        public const string SENT_MAIL = "Sent";
        public const string OUTBOX = "Outbox";
        public const string TRASH_MAIL = "Trash";
        public const string DRAFT_MAIL = "Draft";
        public const string INVESTIGATION_UNDERWRITINGS = "Underwriting";
        public const string USER_ROLES = "USERS/ROLES";
        public const string ADMIN_SETTINGS = "ADMIN SETTINGS";
        public const string GLOBAL_SETTINGS = "Global-settings";
        public const string GENERAL_SETUP = "GENERAL SETUP";
        public const string UPLOAD_FILE = "UPLOAD FILE";
        public const string UPLOAD_DATABASE = "UPLOAD DB";
        public const string AUDIT_LOGS = "Audit Log";

        //457990
        //public const string CURRENT_PINCODE = "457990";

        public const string CURRENT_PINCODE = "3131";

        public const string CURRENT_PINCODE2 = "3130";
        public const string CURRENT_PINCODE3 = "3133";
        public const string CURRENT_PINCODE4 = "3150";
        public const string CURRENT_PINCODE5 = "3125";
        public const string CURRENT_PINCODE6 = "3124";
        //public const string CURRENT_DISTRICT = "Forest Hill";
        //public const string CURRENT_STATE = "VIC";

        public const string INSURER = "Insurer life";
        public const string INSURERDOMAIN = "insurer.com";
        public const string INSURERLOGO = "/img/insurer.jpg";

        public const string CANARA = "Canara Hsbc";
        public const string CANARADOMAIN = "canarahsbc.com";
        public const string CANARALOGO = "/img/chl.jpg";

        public const string ALLIANZ = "Allianz";
        public const string ALLIANZ_DOMAIN = "allianz.com";
        public const string ALLIANZ_LOGO = "/img/allianz.png";

        public const string TATA = "Tata Aia";
        public const string TATA_DOMAIN = "tataaia.com";
        public const string TATA_LOGO = "/img/tata_logo.png";

        public const string HDFC = "Hdfc Life";
        public const string HDFCDOMAIN = "hdfclife.com";
        public const string HDFCLOGO = "/img/hdfc.jpg";

        public const string AGENCY1NAME = "Checker";
        public const string AGENCY1DOMAIN = "checker.com";
        public const string AGENCY1PHOTO = "/img/checker.png";

        public const string AGENCY2NAME = "Verify";
        public const string AGENCY2DOMAIN = "verify.com";
        public const string AGENCY2PHOTO = "/img/verify.png";

        public const string AGENCY3NAME = "Investigate";
        public const string AGENCY3DOMAIN = "investigate.com";
        public const string AGENCY3PHOTO = "/img/investigate.png";

        public const string AGENCY4NAME = "Proper";
        public const string AGENCY4DOMAIN = "proper.com";
        public const string AGENCY4PHOTO = "/img/proper.png";

        public const string AGENCY5NAME = "Honest";
        public const string AGENCY5DOMAIN = "honest.com";
        public const string AGENCY5PHOTO = "/img/honest.png";

        public const string AGENCY6NAME = "Nicer";
        public const string AGENCY6DOMAIN = "nicer.com";
        public const string AGENCY6PHOTO = "/img/nicer.png";

        public const string NO_IMAGE = "/img/no-image.png";
        public const string AUDIO_UPLOAD_IMAGE = "/img/upload-audio.png";
        public const string VIDEO_UPLOAD_IMAGE = "/img/upload-video.png";
        public const string USER_PHOTO = "/img/user.png";
        public const string NO_USER = "/img/no-user.png";
        public const string NO_MAP = "/img/no-map.jpeg";
        public const string MAP_MARKER = "/img/map-marker-icon.png";

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
}