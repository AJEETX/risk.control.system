using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using System.Collections.Generic;
using System.Net;
using System.Security.Claims;

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
            if (await featureManager.IsEnabledAsync(FeatureFlags.LICENSE))
            {

                if (context.User.Identity.IsAuthenticated)
                {
                    var user = context.User.Identity.Name;
                    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var appUser = dbContext.ApplicationUser.FirstOrDefault(u => u.Email == user);
                    var userRole = context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                    if (userRole == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return;
                    }
                    var adminUser = userRole.Value.Contains(AppRoles.PORTAL_ADMIN.ToString());
                    if (!adminUser)
                    {
                        var isCompanyUser = userRole.Value.Contains(AppRoles.COMPANY_ADMIN.ToString()) ||
                                                userRole.Value.Contains(AppRoles.CREATOR.ToString()) ||
                                                userRole.Value.Contains(AppRoles.MANAGER.ToString()) ||
                                                userRole.Value.Contains(AppRoles.ASSESSOR.ToString());

                        var isAgencyUser = userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString())
                                                || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()) ||
                                                userRole.Value.Contains(AppRoles.AGENT.ToString());
                        if (isCompanyUser)
                        {
                            var companyUser = dbContext.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user);
                            var company = dbContext.ClientCompany.FirstOrDefault(c => companyUser.ClientCompanyId == c.ClientCompanyId);

                            if (company == null)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.Redirect("/page/oops.html");
                                return;
                            }
                            if (company.LicenseType == Standard.Licensing.LicenseType.Trial && company.ExpiryDate.HasValue && DateTime.Now.Subtract(company.ExpiryDate.Value).Ticks > 0)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.Redirect("/page/trial.html");
                                return;
                            }
                        }
                    }
                }
            }
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return;
            }
        }
    }
}
