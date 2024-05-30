using Microsoft.Playwright;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace risk.control.system.ui.test.PageObject
{
    public class LoginPage
    {
        private readonly IPage _page;
        private ILocator _header => _page.Locator("text=Login...");
		private ILocator _userNameInput => _page.Locator("select[id='email']");
		private ILocator _passwordInput => _page.Locator("input[id='password']");
		private ILocator _loginButton => _page.Locator("role=button[name='Login']");
		public LoginPage(IPage page) 
        {
            _page = page;             
            _header.WaitForAsync(new() { State = WaitForSelectorState.Visible }).Wait();
        }

        public LoginPage EnterUsername(string username)
        {            
            _userNameInput.SelectOptionAsync(new[] { username }).Wait();
            return this;
        }

        public LoginPage EnterPassword(string password)
        {
            _passwordInput.FillAsync(password).Wait();
            return this;
        }

        public SecureAreaPage ClickLogin()
        {
            _loginButton.ClickAsync().Wait();
            return new(_page);
        }
    }
}