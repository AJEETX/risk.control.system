using Microsoft.Playwright;

namespace risk.control.system.ui.test.PageObject
{
    public class SecureAreaPage
    {
        private readonly IPage _page;
        private ILocator _header => _page.Locator("//h3[contains(text(),'Dashboard')]");
        //private ILocator _header => _page.Locator("//h2[contains(text(),'Secure Area')]");
		private ILocator _status => _page.Locator("a[href='/companyuserprofile']");

		public SecureAreaPage(IPage page) 
        {
            _page = page;             
            _header.WaitForAsync(new() { State = WaitForSelectorState.Visible }).Wait();
        }

        public string GetLogin()
        {
            return _header.InnerTextAsync().Result;
        }
        public string GetLoginStatus()
        {
            return _status.InnerTextAsync().Result;
        }
    }
}