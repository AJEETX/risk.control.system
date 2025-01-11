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
        private readonly string classAIpStartAddress = "1.0.0.0";    // Subnet mask for the network
        private readonly string classAIpEndAddress = "127.255.255.255";    // Subnet mask for the network
        private readonly string classAsubnetMask = "255.0.0.0";    // Subnet mask for the network

        private readonly string classBIpStartAddress = "128.0.0.0";    // Subnet mask for the network
        private readonly string classBIpEndAddress = "191.255.255.255";    // Subnet mask for the network
        private readonly string classBsubnetMask = "255.255.0.0";    // Subnet mask for the network

        private readonly string classCIpStartAddress = "192.0.0.0";    // Subnet mask for the network
        private readonly string classCIpEndAddress = "223.255.255.255";    // Subnet mask for the network
        private readonly string classCsubnetMask = "255.255.255.0";    // Subnet mask for the network

        private readonly string privateIp1StartAddress = "10.0.0.0";
        private readonly string privateIp1EndAddress = "10.255.255.255";

        private readonly string privateIp2StartAddress = "172.16.0.0";
        private readonly string privateIp2EndAddress = "10.255.255.255";

        private readonly string privateIp3StartAddress = "192.168.0.0";
        private readonly string privateIp3EndAddress = "192.168.255.255";


        private readonly RequestDelegate _next;
        private readonly ILogger<WhitelistListMiddleware> _logger;
        private readonly IFeatureManager featureManager;
        private readonly IConfiguration config;
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
                    //var ipAddress = context.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? context.Connection.RemoteIpAddress?.ToString();
                    //var ipAddressWithoutPort = ipAddress?.Split(':')[0];
                    //var remoteIp = IPAddress.Parse(ipAddressWithoutPort);
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
        private bool IsPrivateIp(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            // Check for private IP ranges
            return
                (bytes[0] == 10) ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168);
        }
        private bool IsInNetwork(string ipAddress, string networkAddress, string subnetMask)
        {
            var ip = IPAddress.Parse(ipAddress);
            var network = IPAddress.Parse(networkAddress);
            var mask = IPAddress.Parse(subnetMask);

            byte[] ipBytes = ip.GetAddressBytes();
            byte[] networkBytes = network.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();

            // Ensure that both the IP address and network address are in the same address family (IPv4)
            if (ip.AddressFamily != network.AddressFamily)
            {
                return false;
            }

            // Compare the network part of the IP address using the subnet mask
            for (int i = 0; i < ipBytes.Length; i++)
            {
                // Apply the subnet mask (AND operation)
                if ((ipBytes[i] & maskBytes[i]) != (networkBytes[i] & maskBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private string GetSubnetMask(IPAddress ip)
        {
            // Check if the IP is private and assign the appropriate subnet mask
            byte[] bytes = ip.GetAddressBytes();

            // Subnet mask for Class A (10.x.x.x)
            if (bytes[0] == 10)
            {
                return "255.0.0.0";
            }

            // Subnet mask for Class B (172.x.x.x) - consider 172.16.0.0 to 172.31.255.255 as private
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return "255.240.0.0";
            }

            // Subnet mask for Class C (192.168.x.x)
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return "255.255.0.0";
            }

            // If the IP is public (or any other IP that doesn't match the private ranges), assume /24 subnet mask
            return "255.255.255.0";
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
