﻿using Microsoft.AspNetCore.Authentication;
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
            var timeout = double.Parse(config["SESSION_TIMEOUT_SEC"]) - 30;
            context.Items.Add("timeout", timeout);
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
