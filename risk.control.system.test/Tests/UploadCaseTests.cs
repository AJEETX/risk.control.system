namespace risk.control.system.test.Tests
{
    internal class UploadCaseTests : PlaywrightTestBase
    {
        private static string url = $"{MvcServerFixture.BaseUrl}/CaseUpload/Uploads";

        [TestCase("creator@insurer.com", "R1$kcontrol!")]
        public async Task UploadCaseFile_ShouldUploadMultipleCaseAndDisplayUploadFileInfo(string email, string password)
        {
            // Goto Upload case page
            await LoginAsync(email, password);
            await _page!.WaitForURLAsync($"{BaseUrl}/");
            await _page!.ClickAsync("#file-upload-link");
            //await _page!.WaitForURLAsync($"{url}/");

            //choose file
            string filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "upload", "au.zip");
            // Upload file
            await _page.SetInputFilesAsync("#postedFile", filePath);
            await _page.WaitForFunctionAsync(
                """() => document.querySelector("#UploadFileButton")?.disabled === false""");

            // Upload file
            await _page.ClickAsync("#UploadFileButton");
            await _page.ClickAsync("button:has-text(\"Upload\")");

            Assert.That(_page.Url.StartsWith(url));
            //Assert.That(_page.Url, Does.Match($"{url}?uploadid=\\d+"));
        }

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