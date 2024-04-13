using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

using NetTools;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using System.Collections.Generic;
using System.Net;
using System.Security.Claims;

namespace risk.control.system.Helpers
{
    public class LicensingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WhitelistListMiddleware> _logger;
        private readonly IFeatureManager featureManager;
        private byte[][] _safelist;
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
                    var userRole = context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                    if (userRole == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return;
                    }
                    var adminUser = userRole.Value.Contains(AppRoles.PortalAdmin.ToString());
                    if (!adminUser)
                    {
                        var isCompanyUser = userRole.Value.Contains(AppRoles.CompanyAdmin.ToString())
                                                || userRole.Value.Contains(AppRoles.Creator.ToString()) ||
                                                userRole.Value.Contains(AppRoles.Assessor.ToString());

                        var isAgencyUser = userRole.Value.Contains(AppRoles.AgencyAdmin.ToString())
                                                || userRole.Value.Contains(AppRoles.Supervisor.ToString()) ||
                                                userRole.Value.Contains(AppRoles.Agent.ToString());
                        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
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
            await _next.Invoke(context);
        }
    }
}
