namespace risk.control.system.AppConstant
{
    public static class AppCookie
    {
        public const string AUTH_COOKIE_NAME = "Auth";
        public const string LOGIN_PATH = "/Account/Login";
        public const string LOGOUT_PATH = "/Account/Logout";

        public const string ANTI_FORGERY_COOKIE_NAME = "Antiforgery.cookie";

        public const string CONSENT_COOKIE_NAME = "Consent.cookie";
        public const string COOKIE_PAGELOAD = "Pageload.cookie";
        public const string COOKIE_PAGELOAD_VALUE = "true";
        public const string CONSENT_COOKIE_ACCEPTED = "Accepted";
        public const string ANALYTICS_COOKIE_NAME = "Analytics.cookie";
        public const string PERFORMANCE_COOKIE_NAME = "Performance.cookie";
    }
}