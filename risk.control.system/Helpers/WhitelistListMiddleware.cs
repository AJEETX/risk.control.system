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
            var ipAddress = context.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? context.Connection.RemoteIpAddress?.ToString();
            context.Request.Headers["x-ipaddress"] = ipAddress;
            if (await featureManager.IsEnabledAsync(FeatureFlags.IPRestrict))
            {
                if (!context.Request.Path.Value.Contains("api/agent") && !context.Request.Path.Value.Contains("api/Notification/GetClientIp"))
                {
                    var remoteIp = IPAddress.Parse(ipAddress);
                    _logger.LogDebug("Request from Remote IP address: {RemoteIp}", remoteIp);

                    var bytes = remoteIp.GetAddressBytes();
                    var badIp = true;
                    var ipRange = IPAddressRange.Parse("202.7.251.21/255.255.255.0");
                    ipRange.Contains(remoteIp); // is True.

                    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var ipAddressRanges = dbContext.ClientCompany.Where(c => !string.IsNullOrWhiteSpace(c.WhitelistIpAddressRange)).Select(c => c.WhitelistIpAddressRange).ToList();
                    if (ipAddressRanges.Any())
                    {
                        foreach (var ipAddressRange in ipAddressRanges)
                        {
                            var inRange = IPAddressRange.Parse(ipAddressRange);
                            if (inRange.Contains(remoteIp))
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
            }

            await _next.Invoke(context);
            context.Request.Headers.Remove("x-ipaddress");
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
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await context.HttpContext.SignOutAsync();
        }
    }
}
