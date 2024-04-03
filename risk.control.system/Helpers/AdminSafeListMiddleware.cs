using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using System.Collections.Generic;
using System.Net;

namespace risk.control.system.Helpers
{
    public class AdminSafeListMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminSafeListMiddleware> _logger;
        private readonly IFeatureManager featureManager;
        private readonly byte[][] _safelist;
        public AdminSafeListMiddleware(RequestDelegate next, ILogger<AdminSafeListMiddleware> logger, string safelist, IFeatureManager featureManager)
        {
            var ips = safelist.Split(';');
            _safelist = new byte[ips.Length][];
            for (var i = 0; i < ips.Length; i++)
            {
                _safelist[i] = IPAddress.Parse(ips[i]).GetAddressBytes();
            }

            _next = next;
            _logger = logger;
            this.featureManager = featureManager;
        }

        public async Task Invoke(HttpContext context)
        {
            if(await featureManager.IsEnabledAsync(FeatureFlags.BaseVersion))
            {
                if (!context.Request.Path.Value.Contains("api/agent") && !context.Request.Path.Value.Contains("api/Notification/GetClientIp"))
                {
                    var remoteIp = context.Connection.RemoteIpAddress;
                    _logger.LogDebug("Request from Remote IP address: {RemoteIp}", remoteIp);

                    var bytes = remoteIp.GetAddressBytes();
                    var badIp = true;
                    foreach (var address in _safelist)
                    {
                        if (address.SequenceEqual(bytes))
                        {
                            badIp = false;
                            break;
                        }
                    }
                    var userAuthenticated = context.Request.HttpContext.User?.Identity?.IsAuthenticated ?? false;
                    if (badIp && userAuthenticated)
                    {
                        _logger.LogWarning("Forbidden Request from Remote IP address: {RemoteIp}", remoteIp);
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        context.Response.Redirect("/page/ip.html");
                        return;
                    }
                    else if (badIp && !userAuthenticated)
                    {

                        context.Response.Redirect("/page/oops.html");
                        return;
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
            context.Properties.SetString(
                TicketIssuedTicks,
                DateTimeOffset.UtcNow.Ticks.ToString());

            await base.SigningIn(context);
        }

        public override async Task ValidatePrincipal(
            CookieValidatePrincipalContext context)
        {
            var ticketIssuedTicksValue = context
                .Properties.GetString(TicketIssuedTicks);

            if (ticketIssuedTicksValue is null ||
                !long.TryParse(ticketIssuedTicksValue, out var ticketIssuedTicks))
            {
                await RejectPrincipalAsync(context);
                return;
            }

            var ticketIssuedUtc =
                new DateTimeOffset(ticketIssuedTicks, TimeSpan.FromHours(0));

            if (DateTimeOffset.UtcNow - ticketIssuedUtc > TimeSpan.FromMinutes(1))
            {
                await RejectPrincipalAsync(context);
                return;
            }

            await base.ValidatePrincipal(context);
        }

        private static async Task RejectPrincipalAsync(
            CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }
}
