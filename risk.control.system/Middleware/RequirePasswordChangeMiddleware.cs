using Microsoft.AspNetCore.Identity;
using Microsoft.FeatureManagement;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Middleware
{
    public class RequirePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFeatureManager featureManager;

        public RequirePasswordChangeMiddleware(RequestDelegate next, IFeatureManager featureManager)
        {
            _next = next;
            this.featureManager = featureManager;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (!context.Request.Path.StartsWithSegments("/api") && !context.Request.Headers.ContainsKey("Authorization") && await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION))
            {
                var userEmail = context.User?.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    await _next(context);
                    return;
                }

                var user = await userManager.FindByEmailAsync(userEmail);
                if (user != null && user.IsPasswordChangeRequired && !context.Request.Path.StartsWithSegments("/Account/ChangePassword"))
                {
                    context.Response.Redirect("/Account/ChangePassword");
                    return;
                }
            }

            await _next(context);
        }
    }
}
