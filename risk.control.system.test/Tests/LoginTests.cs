namespace risk.control.system.test.Tests
{
    public class LoginTests : PlaywrightTestBase
    {
        [Test]
        public async Task CanLogin_WithCompanyAdmin()
        {
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.ClickAsync("#acceptCookies");

            await _page.FillAsync("#email", "admin@insurer.com");
            await _page.FillAsync("#Password", "R1$kcontrol!");
            await _page.ClickAsync("button[type=submit]");

            await _page!.WaitForURLAsync($"{BaseUrl}/");
            Assert.That(_page.Url, Is.EqualTo($"{BaseUrl}/"));

            var username = await _page.Locator("#user-firstname").InnerTextAsync();
            StringAssert.Contains("Samy", username);
            var rolename = await _page.Locator("#user-role").InnerTextAsync();
            StringAssert.Contains("admin", rolename);
        }

        [Test]
        public async Task CanLogin_WithCompanyCreator()
        {
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.ClickAsync("#acceptCookies");

            await _page.FillAsync("#email", "creator@insurer.com");
            await _page.FillAsync("#Password", "R1$kcontrol!");
            await _page.ClickAsync("button[type=submit]");

            await _page!.WaitForURLAsync($"{BaseUrl}/");
            Assert.That(_page.Url, Is.EqualTo($"{BaseUrl}/"));

            var username = await _page.Locator("#user-firstname").InnerTextAsync();
            StringAssert.Contains("Reita", username);
            var rolename = await _page.Locator("#user-role").InnerTextAsync();
            StringAssert.Contains("creator", rolename);
        }
    }
}
