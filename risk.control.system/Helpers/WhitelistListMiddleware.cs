using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

using NetTools;

using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using System.Collections.Generic;
using System.Net;

namespace risk.control.system.Helpers
{
    public class WhitelistListMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WhitelistListMiddleware> _logger;
        private readonly IFeatureManager featureManager;
        private byte[][] _safelist;
        public WhitelistListMiddleware(RequestDelegate next, ILogger<WhitelistListMiddleware> logger, IFeatureManager featureManager)
        {
            _next = next;
            _logger = logger;
            this.featureManager = featureManager;
        }

        public async Task Invoke(HttpContext context)
        {
            if (await featureManager.IsEnabledAsync(FeatureFlags.IPRestrict))
            {
                if (!context.Request.Path.Value.StartsWith("/api/") &&
                    !context.Request.Path.Value.StartsWith("/Dashboard/Get") &&
                    !context.Request.Path.Value.StartsWith("/js") &&
                    !context.Request.Path.Value.Contains("api/Notification/GetClientIp") &&
                    (
                    !context.Request.Path.Value.StartsWith("/Account/Login") &&
                    !context.Request.Path.Value.StartsWith("/account/login") &&
                    !context.Request.Query.Any(q => q.Key == "ReturnUrl") &&
                    !context.Request.Query.Any(q => q.Value.Contains("js"))
                    ))
                {
                    var remoteIp = context.Connection.RemoteIpAddress;
                    _logger.LogDebug("Request from Remote IP address: {RemoteIp}", remoteIp);

                    var bytes = remoteIp.GetAddressBytes();
                    var badIp = true;

                    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                    var ipAddresses = dbContext.ClientCompany.Where(c => !string.IsNullOrWhiteSpace(c.WhitelistIpAddress)).Select(c => c.WhitelistIpAddress).ToList();

                    if (ipAddresses.Any())
                    {
                        var safelist = string.Join(";", ipAddresses);
                        var ips = safelist.Split(';');

                        _safelist = new byte[ips.Length][];
                        for (var i = 0; i < ips.Length; i++)
                        {
                            _safelist[i] = IPAddress.Parse(ips[i]).GetAddressBytes();
                        }
                        foreach (var address in _safelist)
                        {
                            if (address.SequenceEqual(bytes))
                            {
                                badIp = false;
                                break;
                            }
                        }
                        //if(badIp)
                        //{
                        //    foreach (var ip in ips)
                        //    {
                        //        var ipRange = IPAddressRange.Parse($"{ip}/255.255.255.0");
                        //        var isInRange = ipRange.Contains(remoteIp); // is True.
                        //        if (isInRange)
                        //        {
                        //            badIp = false;
                        //            break;
                        //        }
                        //    }
                        //}
                       if (badIp)
                        {
                            context.Response.Redirect("/page/oops.html");
                            return;
                        }
                    }
                }
            }

            await _next.Invoke(context);
        }
    }

    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private const string TicketIssuedTicks = nameof(TicketIssuedTicks);

        public override async Task SigningIn(CookieSigningInContext context)
        {
            context.Properties.SetString(TicketIssuedTicks, DateTimeOffset.UtcNow.Ticks.ToString());

            await base.SigningIn(context);
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var ticketIssuedTicksValue = context.Properties.GetString(TicketIssuedTicks);

            if (ticketIssuedTicksValue is null || !long.TryParse(ticketIssuedTicksValue, out var ticketIssuedTicks))
            {
                await RejectPrincipalAsync(context);
                return;
            }

            var ticketIssuedUtc = new DateTimeOffset(ticketIssuedTicks, TimeSpan.FromHours(0));

            if (DateTimeOffset.Now - ticketIssuedUtc > TimeSpan.FromMinutes(1))
            {
                await RejectPrincipalAsync(context);
                return;
            }

            await base.ValidatePrincipal(context);
        }

        private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
