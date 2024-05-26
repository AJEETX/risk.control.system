using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using risk.control.system.ui.test.Setup;
using risk.control.system.AppConstant;

namespace risk.control.system.ui.test.Endpoint
{
    [Parallelizable(ParallelScope.Self)]
    public class Login : SelfHostedPageTest<Program>
    {
        public static string webAppUrl;

        public Login() :
            base(services =>
            {
                // configure needed services, like mocked db access, fake mail service, etc.
            })
        { }
        [OneTimeSetUp]
        public void Init()
        {
            webAppUrl = GetServerAddress();
        }

        [Test]
        public async Task Login_Successful()
        {
            //given
            await using var browser = await Playwright.Chromium.LaunchAsync(new() { Headless = false });
            var context = await browser.NewContextAsync(new()
            {
                RecordVideoDir = "test/",
                RecordVideoSize = new RecordVideoSize() { Width = 800, Height = 600 }
            });
            var page = await context.NewPageAsync();
            var loginPageUlr = webAppUrl + "/Account/Login";
            await page.GotoAsync(loginPageUlr);

            //when
            await page.GetByTitle("Email").SelectOptionAsync(Applicationsettings.INSURER_CREATOR);
            await page.GetByTitle("password").FillAsync(Applicationsettings.Password);
            var locator = page.GetByRole(AriaRole.Button, new() {  NameString = "Login" });

            await locator.HoverAsync();
            await locator.ClickAsync();

            //await page.GetByTestId("logintest").ClickAsync();


            //then
            await page.ScreenshotAsync(new() { Path = CreateImagePath("Login_Successful.png","test", "login") });
            await page.CloseAsync();
            await context.CloseAsync();

            //await Expect(page).ToHaveURLAsync(new Regex(".*Logout"));

        }
        static string CreateImagePath(string filename, string folder, string subfolder)
        {
            var directoryPath = Path.Combine(folder, subfolder);
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            return Path.Combine(directoryPath, filename);
        }
        //[Test]
        //public async Task Clicking_ContactButton_Goes_To_ContactForm()
        //{
        //    await Page.GotoAsync(webAppUrl);
        //    var formButton = Page.Locator("text=Open Contact Form");
        //    await formButton.ClickAsync();
        //    await Expect(Page).ToHaveURLAsync(new Regex(".*Home/Form"));
        //}

        //[Test]
        //public async Task Filling_And_Submitting_ContactForm_Goes_To_SuccessPage()
        //{
        //    await Page.GotoAsync($"{webAppUrl}/Home/Form");
        //    await Page.Locator("text=First name").FillAsync("Néstor");
        //    await Page.Locator("text=Last name").FillAsync("Campos");
        //    await Page.Locator("text=Email address").FillAsync("nestor@gmail.com");
        //    await Page.Locator("text=Birth date").FillAsync("1989-03-16");
        //    await Page.Locator("text=Send").ClickAsync();
        //    await Expect(Page).ToHaveURLAsync(new Regex(".*Home/Success"));
        //}

        //[Test]
        //public async Task Filling_Invalid_Email_Should_Show_ValidationError()
        //{
        //    await Page.GotoAsync($"{webAppUrl}/Home/Form");

        //    ILocator emailValidationLocator = Page.Locator("text=The Email address field is not a valid e-mail address.");
        //    await Expect(emailValidationLocator).Not.ToBeVisibleAsync();

        //    await Page.Locator("text=Email address").FillAsync("nestorgmail.com");
        //    await Page.Locator("text=Send").ClickAsync();

        //    await Expect(Page).ToHaveURLAsync(new Regex(".*Home/Form"));
        //    await Expect(emailValidationLocator).ToBeVisibleAsync();
        //}

    }
}
