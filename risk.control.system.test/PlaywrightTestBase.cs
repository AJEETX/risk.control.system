using Microsoft.Playwright;

namespace risk.control.system.test
{
    public class PlaywrightTestBase
    {
        protected IPlaywright? _playwright;
        protected IBrowser? _browser;
        protected IPage? _page;

        [SetUp]
        public async Task Setup()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            });

            _page = await _browser.NewPageAsync();
        }

        [TearDown]
        public async Task Cleanup()
        {
            await _browser!.CloseAsync();
            _playwright!.Dispose();
        }

        protected string BaseUrl => MvcServerFixture.BaseUrl;
    }
}
