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
        public WhitelistListMiddleware(RequestDelegate next, ILogger<WhitelistListMiddleware> logger, IFeatureManager featureManager, IConfiguration config)
        {
            _next = next;
            _logger = logger;
            this.featureManager = featureManager;
            this.config = config;
        }

        public async Task Invoke(HttpContext context)
        {
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
    }
}
