using System.Net;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Middleware
{
    public class LicensingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WhitelistListMiddleware> _logger;
        private readonly IFeatureManager featureManager;

        public LicensingMiddleware(RequestDelegate next, ILogger<WhitelistListMiddleware> logger, IFeatureManager featureManager)
        {
            _next = next;
            _logger = logger;
            this.featureManager = featureManager;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (await featureManager.IsEnabledAsync(FeatureFlags.LICENSE))
                {
                    if (context.User.Identity.IsAuthenticated)
                    {
                        var authTime = context.User.Claims.FirstOrDefault(c => c.Type == "auth_time")?.Value;
                        if (authTime != null && DateTimeOffset.TryParse(authTime, out var authDateTime))
                        {
                            var now = DateTimeOffset.UtcNow;

                            // Example: Timeout after 30 minutes of inactivity
                            var timeoutDuration = TimeSpan.FromMinutes(double.Parse(context.Items["timeout"].ToString()));

                            if (now - authDateTime > timeoutDuration)
                            {
                                // Logout the user if timeout exceeded
                                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                                // Redirect to login page
                                context.Response.Redirect("/Account/Login"); // Replace with your logout or login page
                                return;
                            }
                        }
                        var user = context.User.Identity.Name;
                        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var appUser = await dbContext.ApplicationUser.FirstOrDefaultAsync(u => u.Email == user);
                        var userRole = context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                        if (userRole == null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            return;
                        }
                        var adminUser = userRole.Value.Contains(PORTAL_ADMIN.DISPLAY_NAME);
                        if (!adminUser)
                        {
                            var isCompanyUser = userRole.Value.Contains(COMPANY_ADMIN.DISPLAY_NAME) ||
                                                    userRole.Value.Contains(CREATOR.DISPLAY_NAME) ||
                                                    userRole.Value.Contains(MANAGER.DISPLAY_NAME) ||
                                                    userRole.Value.Contains(ASSESSOR.DISPLAY_NAME);

                            var isAgencyUser = userRole.Value.Contains(AGENCY_ADMIN.DISPLAY_NAME)
                                                    || userRole.Value.Contains(SUPERVISOR.DISPLAY_NAME) ||
                                                    userRole.Value.Contains(AGENT.DISPLAY_NAME);
                            if (isCompanyUser)
                            {
                                var companyUser = await dbContext.ApplicationUser.FirstOrDefaultAsync(u => u.Email == user && u.ClientCompanyId > 0);
                                var company = await dbContext.ClientCompany.FirstOrDefaultAsync(c => companyUser.ClientCompanyId == c.ClientCompanyId);

                                if (company == null)
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                    context.Response.Redirect("/page/error.html");
                                    return;
                                }
                                if (company.LicenseType == LicenseType.Trial && company.ExpiryDate.HasValue && DateTime.Now.Subtract(company.ExpiryDate.Value).Ticks > 0)
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                    context.Response.Redirect("/page/trial.html");
                                    return;
                                }
                            }
                        }
                    }
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