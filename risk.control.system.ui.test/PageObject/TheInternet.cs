using Microsoft.Playwright;
using static NUnit.Framework.TestContext;

namespace risk.control.system.ui.test.PageObject
{
    public class TheInternet
    {
        private readonly IPage _page;
        private ILocator CheckboxLink => _page.Locator("a[href='/checkboxes']");
		private ILocator FormAuthentication => _page.Locator("a[href='/login']");

		public static TheInternet Initialize(IPage page)
        {
            return new TheInternet(page);
        }
        public TheInternet(IPage page)
        {
            _page = page;
        }

        public TheInternet Open(string? url = "")
        {
            var serverUrl = url ?? Parameters.Get("webAppUrl");
            _page.GotoAsync(url: serverUrl).Wait();
            //CheckboxLink.WaitForAsync(new() { State = WaitForSelectorState.Visible }).Wait();
            return this;
        }

        //public CheckboxesPage ClickCheckboxes()
        //{
        //    CheckboxLink.ClickAsync().Wait();
        //    return new(_page);
        //}

        public LoginPage ClickFormAuthentication()
        {
            //FormAuthentication.ClickAsync().Wait();
            return new(_page);
        }
    }
}
