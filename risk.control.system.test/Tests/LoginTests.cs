namespace risk.control.system.test.Tests
{
    public class LoginTests : PlaywrightTestBase
    {
        [TestCase("admin@insurer.com", "R1$kcontrol!", "Andy", "admin")]
        [TestCase("manager@insurer.com", "R1$kcontrol!", "Manny", "manager")]
        [TestCase("creator@insurer.com", "R1$kcontrol!", "Creaty", "creator")]
        [TestCase("assessor@insurer.com", "R1$kcontrol!", "Assessy", "assessor")]
        [TestCase("admin@honest.com", "R1$kcontrol!", "Mathew", "admin")]
        [TestCase("supervisor@honest.com", "R1$kcontrol!", "Adam", "supervisor")]
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

            // Optional cookie banner
            if (await _page.Locator("#acceptCookies").IsVisibleAsync())
                await _page.ClickAsync("#acceptCookies");
            await _page.WaitForSelectorAsync("#email");

            await _page.FillAsync("#email", email);
            await _page.FillAsync("#Password", password);

            await _page.ClickAsync("button[type=submit]");
        }
    }
}

