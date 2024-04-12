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

                    var remoteIp = context.Request.Headers["x-ipaddress"];
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
                        if (isCompanyUser)
                        {
                            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                            var companyUser = dbContext.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user);
                            var companyWithCurrentRequestIpAddress = dbContext.ClientCompany.FirstOrDefault(c =>
                                !string.IsNullOrWhiteSpace(c.WhitelistIpAddress) &&
                                c.WhitelistIpAddress.Contains(remoteIp) && companyUser.ClientCompanyId == c.ClientCompanyId);

                            if (companyWithCurrentRequestIpAddress == null)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.Redirect("/page/error.html");
                                return;
                            }
                            if (companyWithCurrentRequestIpAddress.LicenseType == Standard.Licensing.LicenseType.Trial && companyWithCurrentRequestIpAddress.ExpiryDate < DateTime.Now)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.Redirect("/page/error.html");
                                return;
                            }
                        }

                    }

                    //var isAgencyUser = AgencyRole.AgencyAdmin.Equals(userRole) || AgencyRole.Supervisor.Equals(userRole) || AgencyRole.Agent.Equals(userRole);

                }


                //else
                //{
                //    _logger.LogWarning("Forbidden Request from Remote IP address: {RemoteIp}", remoteIp);
                //    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                //    return;
                //}
            }
            await _next.Invoke(context);
        }
    }
}
