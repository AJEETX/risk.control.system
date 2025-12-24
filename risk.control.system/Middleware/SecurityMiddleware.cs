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
            if (context.Request.Path.StartsWithSegments("/css") || context.Request.Path.StartsWithSegments("/js") || context.Request.Path.StartsWithSegments("/images"))
            {
                context.Response.Headers["Cache-Control"] = "public,max-age=2592000"; // 30 days
            }
            else
            {
                // Sensitive pages / API
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

            // Security Headers
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
            context.Response.Headers["X-Xss-Protection"] = "1; mode=block";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";

            // Browser isolation protection (fixes Spectre + Fetch warnings)
            //context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
            //context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";

            // Permissions Policy
            context.Response.Headers["Permissions-Policy"] = "geolocation=(self)";
            var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            context.Items["CSP-Nonce"] = nonce;
            // ---- FIXED CSP (no wildcards, no trailing slashes) ----
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self';" +
                "connect-src 'self' https://maps.googleapis.com https://ifsc.razorpay.com;" +
                $"script-src 'self' 'unsafe-inline' https://maps.googleapis.com https://highcharts.com https://export.highcharts.com https://cdnjs.cloudflare.com;" +
                //$"script-src 'self' 'nonce-{nonce}' https://maps.googleapis.com https://highcharts.com https://export.highcharts.com https://cdnjs.cloudflare.com;" +
                "style-src 'self' https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com;" +
                "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com;" +
                "img-src 'self' data: blob: https://maps.gstatic.com https://maps.googleapis.com https://hostedscan.com https://highcharts.com https://export.highcharts.com;" +
                "frame-src 'none';" +
                "media-src 'self' data:;" +
                "object-src 'none';" +
                "form-action 'self';" +
                "frame-ancestors 'none';" +
                "upgrade-insecure-requests;";
            try
            {
                if (context.Request.Path.StartsWithSegments("/swagger"))
                {
                    await _next(context);
                    return;
                }
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                var token = ExtractJwtToken(context);
                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                if (!string.IsNullOrEmpty(token) && !(await tokenService.ValidateJwtToken(dbContext, context, token)))
                {
                    _logger.LogWarning("Invalid JWT token.");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid or expired JWT token.");
                    return;
                }
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred.");
                return;
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
