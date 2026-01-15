using Microsoft.FeatureManagement;

using risk.control.system.Data;
using risk.control.system.Services;

namespace risk.control.system.Middleware
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WhitelistListMiddleware> _logger;
        private readonly IFeatureManager featureManager;
        private readonly IValidationService tokenService;
        private readonly IConfiguration config;
        private string[] errStatusCodes;
        public SecurityMiddleware(RequestDelegate next, ILogger<WhitelistListMiddleware> logger,
            string httpStatusErrorCodes, IFeatureManager featureManager,
            IValidationService tokenService,
            IConfiguration config)
        {
            _next = next;
            _logger = logger;
            this.featureManager = featureManager;
            this.tokenService = tokenService;
            this.config = config;
            errStatusCodes = httpStatusErrorCodes.Split(',');
        }

        public async Task Invoke(HttpContext context)
        {
            // 🔴 FIRST: Completely bypass Azure AD endpoints
            if (context.Request.Path.StartsWithSegments("/signin-oidc") || context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/Account/AzureLogin"))
            {
                await _next(context);
                return;
            }

            // Static files caching
            if (context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js") ||
                context.Request.Path.StartsWithSegments("/img"))
            {
                context.Response.Headers["Cache-Control"] = "public,max-age=2592000";
            }
            else
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }

            // Remove identifying headers
            context.Response.Headers.Remove("X-Powered-By");
            context.Response.Headers.Remove("X-AspNet-Version");
            context.Response.Headers.Remove("X-AspNetMvc-Version");
            context.Response.Headers.Remove("X-AspNetCore-Version");
            context.Response.Headers.Remove("X-Generator");
            context.Response.Headers.Remove("Server");

            // Security headers
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
            context.Response.Headers["X-Xss-Protection"] = "1; mode=block";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(self)";

            // CSP (SAFE)
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self';" +
                "connect-src 'self' https://maps.googleapis.com https://ifsc.razorpay.com https://login.microsoftonline.com/*;" +
                "script-src 'self' 'unsafe-inline' https://maps.googleapis.com https://highcharts.com https://export.highcharts.com https://cdnjs.cloudflare.com;" +
                "style-src 'self' https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com;" +
                "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com;" +
                "img-src 'self' data: blob: https://maps.gstatic.com https://maps.googleapis.com https://hostedscan.com https://highcharts.com https://export.highcharts.com;" +
                "frame-src 'self' https://login.microsoftonline.com/*;" +
                "media-src 'self' data:;" +
                "object-src 'none';" +
                "form-action 'self' https://login.microsoftonline.com/*;" +
                "frame-ancestors 'none';" +
                "upgrade-insecure-requests;";

            try
            {
                // JWT validation (API only)
                var token = ExtractJwtToken(context);
                if (!string.IsNullOrEmpty(token))
                {
                    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                    if (!await tokenService.ValidateJwtToken(dbContext, context, token))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Invalid or expired JWT token.");
                        return;
                    }
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecurityMiddleware error");
                throw;
            }
        }

        private string ExtractJwtToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            return null;
        }
    }
}
