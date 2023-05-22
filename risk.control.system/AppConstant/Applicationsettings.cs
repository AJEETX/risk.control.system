namespace risk.control.system.AppConstant
{
    public static class Applicationsettings
    {
        public const string Password = "R1$kcontrol!";
        public const string TestPassword = "R1$kcontrol";
        public const string PERMISSION = "Permission";

        //WEBSITE SETTINGS

        public const string WEBSITE_SITE_MENU_BRAND = "aby";
        public const string WEBSITE_TITLE = "risk.control.unit";
        public const string WEBSITE_SITE_MENU_LOGO = "/img/logo.png";
        public const string WEBSITE_SITE_DESIGNER = "its aby";
        public const string WEBSITE_SITE_DESIGNER_URL = "http://itsaby.com.au";

        //LOGIN
        public const string WEBSITE_LOGIN = "Log in: risk.control.unit";
        public const string WEBSITE_SIGNIN = "Sign in";



        // BUTTONS / ACTIONS

        public const string BACK = "back";
        public const string CANCEL = "cancel";
        public const string RESET = "reset";
        public const string APPROVE = "approve";
        public const string SEARCH = "search";
        public const string CREATE = "create";
        public const string EDIT = "edit";
        public const string DELETE = "delete";
        public const string VIEW = "view";
        public const string DETAILS = "details";
        public const string VERIFY_LOCATIONS = "Locations to verify";

        // LABELS / ACTIONS
        public const string CLIENT_COMPANIES = "Company";
        public const string CLIENT_COMPANY = "Company";
        public const string COUNTRY = "Country";
        public const string DISTRICT = "District";
        public const string CASE_STATUS = "Case status";
        public const string SERVICE = "Service";
        public const string VERIFY_SERVICE = "Verify Service";
        public const string INVESTIGATION_SERVICE = "Type of service";
        public const string BENEFICIARY = "Claims beneficiary";
        public const string CASE_ENABLER = "Case enabler";
        public const string COST_CENTRE = "Cost centre";
        public const string CASE_OUTCOME = "Case outcome";
        public const string CASE_SUBSTATUS = "Case sub status";
        public const string LINE_OF_BUSINESS = "Line of business";
        public const string PINCODE = "Pincode";
        public const string RECORD = "Record";
        public const string ROLE = "Role";
        public const string STATE = "State";
        public const string USER = "User";
        public const string VENDOR = "Vendor";
        public const string VENDORS = "Vendor";
        public const string MANAGE_VENDOR = "Manage vendor";
        public const string MANAGE_CLIENT_COMPANY = "Manage client company";
        public const string MANAGE_VERIFICTION_LOCATIONS = "Manage locations to verify";
        public const string EMPANEL = "Empanel";
        public const string BROADCAST = "Broadcast";
        public const string CREATE_SELECTED_CASES = "Create selected cases";

        public const string MANAGE_COMPANY_VENDOR = "Manage company vendor";
        public const string AVAILABLE_VENDORS = "Available vendors";
        public const string DEPANEL_VENDORS = "Depanel vendors";
        public const string MANAGE_VENDOR_USER = "Manage vendor user";
        public const string EDIT_VENDOR = "Edit vendor";
        public const string MANAGE_USERS = "Manage user";
        public const string AUDIT_LOG = "Audit log";

        public const string MANAGE_SERVICE = "Manage service";
        public const string ADD_SERVICE = "Add service";
        public const string PROFILE = "Profile";
        public const string MULTIPLE_UPLOAD = "Multi-Upload";
        public const string PINCODE_UPLOAD = "Pincode upload";
        public const string PINCODE_SAMPLE = "Upload sample file to upload in csv format";
        public const string UPLOADED_SAMPLE = "Uploaded Records";
        public const string NO_RECORDS_FOUND = "No Records Found";

        public const string UPLOAD = "Upload Sample";
        public const string UPLOAD_CASE = "Upload Cases";
        public const string UPLOADED_CASES = "Uploaded Cases";
        public const string SAMPLE_PINCODE_FILE_TYPE = "csv";
        public const string DOWNLOAD = "Download";
        public static string SAMPLE_PINCODE_FILE = $"Sample csv file for pincode upload";


        // MENUS / SUBMENUS

        public const string DASHBOARD = "Dashboard";
        public const string INVESTIGATION_MAIN = "INVESTIGATIONS";
        public const string INVESTIGATION_CASE_SETTINGS = "CASE SETTINGS";
        public const string COMPANY_SETTINGS = "COMPANY SETTINGS";
        public const string INVESTIGATION_CASE_STATUS = "Case status";
        public const string INVESTIGATION_CLAIMS = "Claim";
        public const string INVESTIGATION_CLAIMS_CASE= "Claim case";
        public const string INVESTIGATION_CLAIMS_OPEN = "Claim case open";
        public const string INVESTIGATION_CLAIMS_CASE_DETAILS = "Claim case details";
        public const string CUSTOMER_DETAILS = "Customer details";
        public const string BENEFICIARY_DETAILS = "Beneficiary details";
        public const string MAILBOX = "Mailbox";
        public const string INBOX = "Inbox";
        public const string COMPOSE = "Compose";
        public const string SENT_MAIL = "Sent";
        public const string OUTBOX = "Outbox";
        public const string TRASH_MAIL = "Trash";
        public const string DRAFT_MAIL = "Draft";
        public const string INVESTIGATION_UNDERWRITINGS = "Underwriting";
        public const string USER_ROLES = "USERS/ROLES";
        public const string ADMIN_SETTINGS = "ADMIN SETTINGS";
        public const string GENERAL_SETUP = "GENERAL SETUP";
        public const string UPLOAD_FILE = "UPLOAD FILE";
        public const string UPLOAD_DATABASE = "UPLOAD DB";
        public const string AUDIT_LOGS = "AUDIT LOGS";


        public const string CURRENT_PINCODE = "515631";
        public const string CURRENT_DISTRICT = "ANANTAPUR";
        public const string CURRENT_STATE = "AD";



        public const string NO_IMAGE = "/img/no-image.png";
        public static class PORTAL_ADMIN
        {
            public const string DISPLAY_NAME = "PORTAL ADMIN";
            public const string CODE = "pa";
            public const string USERNAME = "portal-admin@admin.com";
            public const string EMAIL = "portal-admin@admin.com";
            public const string FIRST_NAME = "Ajeet";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/superadmin.jpg";
        }
        public static class CLIENT_ADMIN
        {
            public const string DISPLAY_NAME = "CLIENT ADMIN";
            public const string CODE = "ca";
            public const string USERNAME = "client-admin@admin.com";
            public const string EMAIL = "client-admin@admin.com";
            public const string FIRST_NAME = "Amit";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/admin.png";
        }
        public static class CLIENT_CREATOR
        {
            public const string DISPLAY_NAME = "CLIENT CREATOR";
            public const string CODE = "cc";
            public const string USERNAME = "client-creator@admin.com";
            public const string EMAIL = "client-creator@admin.com";
            public const string FIRST_NAME = "Rashmi";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/creator.jpg";
        }
        public static class CLIENT_ASSIGNER
        {
            public const string DISPLAY_NAME = "CLIENT ASSIGNER";
            public const string CODE = "cs";
            public const string USERNAME = "client-assigner@admin.com";
            public const string EMAIL = "client-assigner@admin.com";
            public const string FIRST_NAME = "Christian";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/assigner.png";
        }
        public static class CLIENT_ASSESSOR
        {
            public const string DISPLAY_NAME = "CLIENT ASSESSOR";
            public const string CODE = "co";
            public const string USERNAME = "client-assessor@admin.com";
            public const string EMAIL = "client-assessor@admin.com";
            public const string FIRST_NAME = "Damien";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/assessor.png";
        }
        public static class VENDOR_ADMIN
        {
            public const string DISPLAY_NAME = "VENDOR ADMIN";
            public const string CODE = "va";
            public const string USERNAME = "vendor-admin@admin.com";
            public const string EMAIL = "vendor-admin@admin.com";
            public const string FIRST_NAME = "Gopal";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/vendor-admin.png";
        }
        public static class VENDOR_SUPERVISOR
        {
            public const string DISPLAY_NAME = "VENDOR SUPERVISOR";
            public const string CODE = "vs";
            public const string USERNAME = "vendor-supervisor@admin.com";
            public const string EMAIL = "vendor-supervisor@admin.com";
            public const string FIRST_NAME = "Lala";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/supervisor.png";
        }
        public static class VENDOR_AGENT
        {
            public const string DISPLAY_NAME = "VENDOR AGENT";
            public const string CODE = "vf";
            public const string USERNAME = "vendor-agent@admin.com";
            public const string EMAIL = "vendor-agent@admin.com";
            public const string FIRST_NAME = "Gogo";
            public const string LAST_NAME = "Kumar";
            public const string PROFILE_IMAGE = "/img/agent.jpg";
        }
    }
}
