using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace risk.control.system.ui.test.Setup
{
    public abstract class SelfHostedPageTest<TEntryPoint> : PageTest where TEntryPoint : class
    {
        private readonly CustomWebApplicationFactory<TEntryPoint> _webApplicationFactory;

        public SelfHostedPageTest(Action<IServiceCollection> configureServices)
        {
            _webApplicationFactory = new CustomWebApplicationFactory<TEntryPoint>(configureServices);
        }

        protected string GetServerAddress() => _webApplicationFactory.ServerAddress;
    }
}
