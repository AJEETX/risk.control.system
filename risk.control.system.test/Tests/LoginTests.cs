using risk.control.system.AppConstant;

namespace risk.control.system.test.Tests
{
    public class LoginTests : PlaywrightTestBase
    {
        [TestCase("admin@insurer.com", Applicationsettings.TestingData, COMPANY_ADMIN.FIRST_NAME, COMPANY_ADMIN.CODE)]
        [TestCase("manager@insurer.com", Applicationsettings.TestingData, MANAGER.FIRST_NAME, MANAGER.CODE)]
        [TestCase("creator@insurer.com", Applicationsettings.TestingData, CREATOR.FIRST_NAME, CREATOR.CODE)]
        [TestCase("assessor@insurer.com", Applicationsettings.TestingData, ASSESSOR.FIRST_NAME, ASSESSOR.CODE)]
        [TestCase("admin@verify.com", Applicationsettings.TestingData, AGENCY_ADMIN.FIRST_NAME, AGENCY_ADMIN.CODE)]
        [TestCase("supervisor@verify.com", Applicationsettings.TestingData, SUPERVISOR.FIRST_NAME, SUPERVISOR.CODE)]
        public async Task Login_ShouldRedirectToHome_AndDisplayCorrectUserInfo(
            string email,
            string password,
            string expectedName,
            string expectedRole)
        {
            // Perform Login
            await LoginAsync(email, password);

            // Validate redirect
            await _page!.WaitForURLAsync($"{BaseUrl}/");
            Assert.That(_page.Url, Is.EqualTo($"{BaseUrl}/"));

            // Read user name
            var username = await _page.Locator("#user-firstname").InnerTextAsync();
            StringAssert.Contains(expectedName, username);

            // Read user role
            var role = await _page.Locator("#user-role").InnerTextAsync();
            StringAssert.Contains(expectedRole, role);
        }

        /// <summary>
        /// Performs login using the UI.
        /// Keeps the test clean and reusable.
        /// </summary>
        private async Task LoginAsync(string email, string password)
        {
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForTimeoutAsync(1000);

            if (await _page.Locator("#acceptCookies").IsVisibleAsync())
                await _page.ClickAsync("#acceptCookies");
            await _page.WaitForSelectorAsync("#email");

            await _page.FillAsync("#email", email);
            await _page.FillAsync("#Password", password);

            await _page.ClickAsync("button[type=submit]");
        }
    }
}