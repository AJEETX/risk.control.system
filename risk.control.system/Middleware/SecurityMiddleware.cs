using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Middleware
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;
        private readonly IJwtService tokenService;

        public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger,
            IJwtService tokenService)
        {
            _next = next;
            _logger = logger;
            this.tokenService = tokenService;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/signin-oidc") || context.Request.Path.StartsWithSegments("/swagger") || context.Request.Path.StartsWithSegments("/Account/AzureLogin") || context.Request.Path.StartsWithSegments("/debug-test"))
            {
                await _next(context);
                return;
            }
            if (context.Request.Path.StartsWithSegments("/css") || context.Request.Path.StartsWithSegments("/js") || context.Request.Path.StartsWithSegments("/img"))
            {
                context.Response.Headers.CacheControl = "public,max-age=2592000";
            }
            else
            {
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";
            }
            context.Response.Headers.Remove("X-Powered-By");
            context.Response.Headers.Remove("X-AspNet-Version");
            context.Response.Headers.Remove("X-AspNetMvc-Version");
            context.Response.Headers.Remove("X-AspNetCore-Version");
            context.Response.Headers.Remove("X-Generator");
            context.Response.Headers.Remove("Server");
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
            context.Response.Headers.XXSSProtection = "1; mode=block";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(self)";
            context.Response.Headers.ContentSecurityPolicy =
                "default-src 'self';" +
                "connect-src 'self' https://maps.googleapis.com https://ifsc.razorpay.com https://login.microsoftonline.com;" +
                "script-src 'self' https://maps.googleapis.com https://cdnjs.cloudflare.com;" +
                "style-src 'self' https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com;" +
                "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://fonts.googleapis.com https://stackpath.bootstrapcdn.com;" +
                "img-src 'self' data: blob: https://maps.gstatic.com https://maps.googleapis.com;" +
                "frame-src 'self' https://login.microsoftonline.com;" +
                "media-src 'self' data:;" +
                "object-src 'none';" +
                "form-action 'self' https://login.microsoftonline.com;" +
                "frame-ancestors 'none';";
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

        private string ExtractJwtToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            return null!;
        }
    }
}