namespace risk.control.system.Middleware
{
    public class CookieConsentMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CookieConsentMiddleware> _logger;
        private readonly IConfiguration config;

        public CookieConsentMiddleware(RequestDelegate next, ILogger<CookieConsentMiddleware> logger, IConfiguration config)
        {
            _next = next;
            _logger = logger;
            this.config = config;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Check if the "CookieConsent" cookie exists
                if (!context.Request.Path.StartsWithSegments("/api") &&
                    !context.Request.Path.StartsWithSegments("/js") &&
                    !context.Request.Path.StartsWithSegments("/Session") &&
                    !context.Request.Path.StartsWithSegments("/Document") &&
                    !context.Request.Path.StartsWithSegments("/DashboardGraph") &&
                    !context.Request.Path.StartsWithSegments("/Dashboard"))
                {
                    var cookieConsent = context.Request.Cookies["CookieConsent"];
                    context.Items["HasCookieConsent"] = cookieConsent == "Accepted";

                    var timeout = double.Parse(config["SESSION_TIMEOUT_SEC"]);
                    context.Items.Add("timeout", timeout);
                    Console.WriteLine("timeout (sec): " + timeout);
                }

                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ocurred. {UserId}", context?.User?.Identity?.Name ?? "Anonymous");
                return;
            }
        }
    }
}