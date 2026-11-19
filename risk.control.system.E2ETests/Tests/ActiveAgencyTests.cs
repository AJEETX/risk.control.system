using risk.control.system.E2ETests.Infrastructure;

namespace risk.control.system.E2ETests.Tests;

[TestFixture]
public class ActiveAgencyTests : E2ETestBase
{
    [SetUp]
    public Task SignInAsync() => LoginAsManagerAsync();

    [Test]
    public async Task ActiveAgencies_LoadsEmpanelledAgencies()
    {
        var apiResponse = Page.WaitForResponseAsync(r => r.Url.Contains("/api/Company/GetAllEmpanelledAgencies"));

        await Page.GotoAsync("/ActiveAgency/Agencies");

        var response = await apiResponse;
        Assert.That(response.Status, Is.EqualTo(200), "Empanelled agencies API should succeed");

        await Expect(Page.Locator(".card-title", new() { HasTextString = "Active Agencies" })).ToBeVisibleAsync();

        var rows = Page.Locator("#dataTable tbody tr");
        await Expect(rows.First).ToBeVisibleAsync();
        await Expect(Page.Locator("#dataTable tbody")).ToContainTextAsync(TestSettings.ExpectedAgencyDomain);
    }

    [Test]
    public async Task Depanel_WithoutSelection_IsBlockedByAlert()
    {
        await Page.GotoAsync("/ActiveAgency/Agencies");
        await Expect(Page.Locator("#dataTable tbody tr").First).ToBeVisibleAsync();

        // drawCallback enables the button once rows are loaded.
        var depanel = Page.Locator("#empanel-vendors");
        await Expect(depanel).ToBeEnabledAsync();

        await depanel.ClickAsync();

        // Client-side guard prevents the POST; seeded data is left intact.
        await Expect(Page.GetByText("Please select agency to depanel?")).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/ActiveAgency/Agencies", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task BackButton_ReturnsToDashboard()
    {
        await Page.GotoAsync("/ActiveAgency/Agencies");
        await Page.Locator("#back").ClickAsync();

        // Dashboard/Index is the default route, so the link resolves to "/".
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"^https?://[^/]+/(Dashboard(/Index)?)?/?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
