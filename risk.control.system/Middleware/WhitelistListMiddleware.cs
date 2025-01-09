using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using System.Collections.Generic;
using System.Net;

namespace risk.control.system.Middleware
{
    public class WhitelistListMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WhitelistListMiddleware> _logger;
        private readonly IFeatureManager featureManager;
        private readonly IConfiguration config;
        private byte[][] _safelist;
        public WhitelistListMiddleware(RequestDelegate next, ILogger<WhitelistListMiddleware> logger, IFeatureManager featureManager, IConfiguration config)
        {
            _next = next;
            _logger = logger;
            this.featureManager = featureManager;
            this.config = config;
        }

        public async Task Invoke(HttpContext context)
        {
            if (await featureManager.IsEnabledAsync(FeatureFlags.IPRestrict))
            {
                if (!context.Request.Path.Value.StartsWith("/api/") &&
                    !context.Request.Path.Value.StartsWith("/Dashboard/Get") &&
                    !context.Request.Path.Value.StartsWith("/js") &&
                    !context.Request.Path.Value.Contains("api/Notification/GetClientIp") &&
                    
                    !context.Request.Path.Value.StartsWith("/Account/Login") &&
                    !context.Request.Path.Value.StartsWith("/account/login") &&
                    !context.Request.Query.Any(q => q.Key == "ReturnUrl") &&
                    !context.Request.Query.Any(q => q.Value.Contains("js"))
                    )
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

                        badIp = !IsIpAllowed(remoteIp, ips?.ToList());
                        
                        if (badIp)
                        {
                            context.Response.Redirect("/page/oops.html");
                            return;
                        }
                    }
                }
            }
            var timeout = double.Parse(config["SESSION_TIMEOUT_SEC"]);
            context.Items.Add("timeout", timeout);
            Console.WriteLine("timeout: " + timeout);
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
        private bool IsIpAllowed(IPAddress remoteIp, List<string> allowedIpRanges)
        {
            foreach (var range in allowedIpRanges)
            {
                if (IPAddressRangeContains(range, remoteIp))
                    return true;
            }
            return false;
        }

        private bool IPAddressRangeContains(string range, IPAddress address)
        {
            // Handle CIDR ranges
            if (range.Contains("/"))
            {
                var parts = range.Split('/');
                var baseIp = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);

                var baseIpBytes = baseIp.GetAddressBytes();
                var addressBytes = address.GetAddressBytes();

                var mask = new byte[baseIpBytes.Length];
                for (int i = 0; i < mask.Length; i++)
                {
                    int bits = Math.Max(0, Math.Min(8, prefixLength - (i * 8)));
                    mask[i] = (byte)(0xFF << (8 - bits));
                }

                for (int i = 0; i < baseIpBytes.Length; i++)
                {
                    if ((baseIpBytes[i] & mask[i]) != (addressBytes[i] & mask[i]))
                        return false;
                }

                return true;
            }

            // Handle individual IP addresses
            if (range == address.ToString())
            {
                return true;
            }

            return false;
        }
    }
}
