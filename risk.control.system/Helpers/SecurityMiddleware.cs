using Microsoft.FeatureManagement;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WhitelistListMiddleware> _logger;
        private readonly IFeatureManager featureManager;
        private string[] errStatusCodes;
        public SecurityMiddleware(RequestDelegate next, ILogger<WhitelistListMiddleware> logger, string httpStatusErrorCodes, IFeatureManager featureManager)
        {
            _next = next;
            _logger = logger;
            this.featureManager = featureManager;
            errStatusCodes = httpStatusErrorCodes.Split(',');
        }

        public async Task Invoke(HttpContext context)
        {
            if (await featureManager.IsEnabledAsync(FeatureFlags.SECURITY))
            {
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
                context.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("Referrer-Policy", "no-referrer");
                context.Response.Headers.Add("Permissions-Policy", "geolocation=(self)");

                context.Response.Headers.Add("Content-Security-Policy",
                    "default-src 'self';" +
                    "connect-src 'self' wss: https://maps.googleapis.com; " +
                    "script-src 'unsafe-inline' 'self' https://maps.googleapis.com https://polyfill.io https://highcharts.com https://export.highcharts.com https://cdnjs.cloudflare.com; " +
                    "style-src 'unsafe-inline' 'self' https://cdnjs.cloudflare.com/ https://fonts.googleapis.com https://stackpath.bootstrapcdn.com; " +
                    "font-src  'self'  https://fonts.gstatic.com https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com; " +
                    "img-src 'self'  data: blob: https://maps.gstatic.com https://maps.googleapis.com https://hostedscan.com https://highcharts.com https://export.highcharts.com; " +
                    "frame-src 'none';" +
                    "media-src 'self' blob: https:;" +
                    "object-src 'none';" +
                    "form-action 'self';" +
                    "frame-ancestors 'self' https://maps.googleapis.com;" +
                    "upgrade-insecure-requests;");
            }
            //if (errStatusCodes.Any(e=>int.Parse(e) == context.Response.StatusCode))
            //{
            //    context.Response.Redirect("/Account/Login", true);
            //}
            await _next(context).ConfigureAwait(false); 
        }
    }
}
