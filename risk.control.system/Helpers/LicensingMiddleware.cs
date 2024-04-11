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
            if(await featureManager.IsEnabledAsync(FeatureFlags.LICENSE))
            {
                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                var remoteIp = context.Request.Headers["X-IPAddress"];
                if(context.User.Identity.IsAuthenticated)
                {
                    var userEmail = context.User.Identity.Name;

                    var userCompany = dbContext.ClientCompany.FirstOrDefault(c=>c.Email == userEmail.Substring(userEmail.IndexOf("@")));

                    if(userCompany != null)
                    {
                        if(userCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                        {
                            if(userCompany.Status == Models.CompanyStatus.ACTIVE )
                            {
                                if(userCompany.ExpiryDate < DateTime.Now)
                                {

                                }
                            }
                        }
                    }
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
