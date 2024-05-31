using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace risk.control.system.ui.test.Setup
{
    internal class CustomWebApplicationFactory<TEntryPoint> :
       WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        private readonly Action<IServiceCollection> _configureServices;
        private readonly string _environment;
        public readonly string ServerAddress = "https://localhost:5001";

        public CustomWebApplicationFactory(
            Action<IServiceCollection> configureServices,
            string environment = "Development")
        {
            _configureServices = configureServices;
            _environment = environment;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
            base.ConfigureWebHost(builder);

            // Add mock/test services to the builder here
            if (_configureServices is not null)
            {
                builder.ConfigureServices(_configureServices);
            }
        }

        // ...

    }
}
