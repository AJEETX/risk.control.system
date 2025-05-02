using System.IdentityModel.Tokens.Jwt;
using System.Text;

using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;

using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
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
            var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            context.Items["CSP-Nonce"] = nonce;

            if (await featureManager.IsEnabledAsync(FeatureFlags.SECURITY))
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
                context.Response.Headers.Append("X-Xss-Protection", "1; mode=block");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("Referrer-Policy", "no-referrer");
                context.Response.Headers.Append("Permissions-Policy", "geolocation=(self)");

                context.Response.Headers.Append("Content-Security-Policy",
                    "default-src 'self';" +
                    "connect-src 'self' wss: https://maps.googleapis.com; " +
                    "script-src 'unsafe-inline' 'self' https://maps.googleapis.com https://highcharts.com https://export.highcharts.com https://cdnjs.cloudflare.com;" +
                    //$"script-src 'self' 'nonce-{nonce}' https://maps.googleapis.com https://highcharts.com https://export.highcharts.com https://cdnjs.cloudflare.com;" +
                    "style-src 'self' https://cdnjs.cloudflare.com/ https://fonts.googleapis.com https://stackpath.bootstrapcdn.com; " +
                    "font-src  'self'  https://fonts.gstatic.com https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com; " +
                    "img-src 'self'  data: blob: https://maps.gstatic.com https://maps.googleapis.com https://hostedscan.com https://highcharts.com https://export.highcharts.com; " +
                    "frame-src 'none';" +
                    "media-src 'self' blob: https:;" +
                    "object-src 'none';" +
                    "form-action 'self';" +
                    "frame-ancestors 'self' https://maps.googleapis.com;" +
                    "upgrade-insecure-requests;");
            }
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

                if (!string.IsNullOrEmpty(token) && !(await tokenService.ValidateJwtToken(dbContext, context,token)))
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
                Console.WriteLine(ex.StackTrace);
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
