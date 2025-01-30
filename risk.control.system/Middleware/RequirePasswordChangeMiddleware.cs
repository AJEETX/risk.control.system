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
            var user = await userManager.GetUserAsync(context.User);
            if (user != null && await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION))
            {
                if (user.IsPasswordChangeRequired && !context.Request.Path.StartsWithSegments("/Account/ChangePassword"))
                {
                    context.Response.Redirect("/Account/ChangePassword");
                    return;
                }
            }
            

            await _next(context);
        }
    }
}
