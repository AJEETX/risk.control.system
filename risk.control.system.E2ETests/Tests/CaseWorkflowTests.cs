using System.Text.RegularExpressions;
using risk.control.system.E2ETests.Infrastructure;

namespace risk.control.system.E2ETests.Tests;

/// <summary>
/// Drives real case workflows through the UI across roles. These tests CHANGE DATA, so they only run when
/// this test run started the app with its own fresh database (see AppHost / E2E_FRESH_DB).
///
/// Lifecycle: creator adds case + customer + beneficiary -> auto-assigns to agency (checker.com)
/// -> agency admin tasks the agent -> agent submits report -> agency admin submits to company
/// -> assessor approves or rejects.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CaseWorkflowTests : E2ETestBase
{
    private static readonly string WwwRoot = Path.Combine(AppHost.FindProjectDirectory(), "wwwroot");
    private static readonly string DocumentImage = Path.Combine(WwwRoot, "img", "document.jpeg");
    private static readonly string PersonImage = Path.Combine(WwwRoot, "seed", "agent.jpeg");

    private static bool _aiDisabled;

    [SetUp]
    public void RequireDisposableDatabase()
    {
        if (!AppHostSetup.StartedWithFreshDatabase)
        {
            Assert.Ignore("Workflow tests change data; they run only when the test run starts the app with a fresh database.");
        }
    }

    [Test]
    public async Task Case_FullLifecycle_Approved()
    {
        await RunLifecycleAsync(approve: true);
    }

    [Test]
    public async Task Case_FullLifecycle_Rejected()
    {
        await RunLifecycleAsync(approve: false);
    }

    [Test]
    public async Task Creator_CanWithdraw_AssignedCase()
    {
        var caseNumber = await CreateAndAssignCaseAsync();

        await Page.GotoAsync("/CaseActive/Active");
        var row = await FindRowAsync(caseNumber);
        await (await RowElementAsync(row, "a.btn-info")).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/CaseActive/ActiveDetail", RegexOptions.IgnoreCase));

        await Page.Locator("#submit-case").ClickAsync();
        await ConfirmAsync("Withdraw");

        // Withdrawn cases go back to the creator's Add/Assign list and can be reassigned.
        await Expect(Page).ToHaveURLAsync(new Regex("/AddAssign/New", RegexOptions.IgnoreCase), new() { Timeout = 15_000 });
        await FindRowAsync(caseNumber);
    }

    [Test]
    public async Task Agency_CanDecline_AssignedCase()
    {
        var caseNumber = await CreateAndAssignCaseAsync();
        await LogoutAsync();

        await LoginAsAsync(Role.AgencyAdmin);
        await Page.GotoAsync("/VendorInvestigation/Allocate");
        var row = await FindRowAsync(caseNumber);
        await (await RowElementAsync(row, "a.btn-info.detail")).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/VendorInvestigation/CaseDetail", RegexOptions.IgnoreCase));

        await Page.Locator("#submit-case").ClickAsync();
        await ConfirmAsync("DECLINE");
        await Expect(Page).Not.ToHaveURLAsync(new Regex("/VendorInvestigation/CaseDetail", RegexOptions.IgnoreCase), new() { Timeout = 15_000 });
        await LogoutAsync();

        // Declined cases return to the creator.
        await LoginAsAsync(Role.Creator);
        await Page.GotoAsync("/AddAssign/New");
        await FindRowAsync(caseNumber);
    }

    private async Task RunLifecycleAsync(bool approve)
    {
        var caseNumber = await CreateAndAssignCaseAsync();
        await LogoutAsync();

        // Agency admin tasks the case to the agent.
        await LoginAsAsync(Role.AgencyAdmin);
        await Page.GotoAsync("/VendorInvestigation/Allocate");
        var row = await FindRowAsync(caseNumber);
        await (await RowElementAsync(row, "input[name=id]")).CheckAsync();
        await Page.Locator("#allocatedcase").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/VendorInvestigation/SelectAgent", RegexOptions.IgnoreCase));

        var agentRadio = Page.Locator("#dataTable tbody input[name=selectedcase]").First;
        await Expect(agentRadio).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await agentRadio.CheckAsync();
        await Page.Locator("#allocatedcase").ClickAsync();
        await ConfirmAsync("Allocate agent");
        await ExpectToastAsync("Tasked");
        await LogoutAsync();

        // Agent captures evidence and submits the investigation.
        await LoginAsAsync(Role.Agent);
        await Page.GotoAsync("/Agent/Agent");
        row = await FindRowAsync(caseNumber);
        await (await RowElementAsync(row, "input[name=id]")).CheckAsync();
        await Page.Locator("#allocatedcase").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Agent/GetInvestigate", RegexOptions.IgnoreCase));

        var withEvidence = TestSettings.FakeExternalServices;
        if (withEvidence)
        {
            // Reject path: the agent's selfie at the cemetery does not match the agent's profile photo.
            await UploadRequiredEvidenceAsync(agentSelfieMatches: approve);
        }

        await Page.Locator("textarea#remarks").FillAsync("E2E agent remarks: verified on site.");
        await Page.Locator("#terms_and_conditions").CheckAsync();
        await Page.Locator("#submit-case").ClickAsync();
        await ConfirmAsync("SUBMIT");
        await ExpectToastAsync("report submitted");
        await Page.GotoAsync("/Agent/Submitted");
        await FindRowAsync(caseNumber);
        await LogoutAsync();

        // Agency admin reviews and submits the report to the company.
        await LoginAsAsync(Role.AgencyAdmin);
        await Page.GotoAsync("/VendorInvestigation/AgentReport");
        row = await FindRowAsync(caseNumber);
        await (await RowElementAsync(row, "input[name=id]")).CheckAsync();
        await Page.Locator("#allocatedcase").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/VendorInvestigation/ReportDetail", RegexOptions.IgnoreCase));

        if (withEvidence)
        {
            await AssertEvidenceShownAsync();
        }

        await Page.Locator("textarea#supervisorRemarks").FillAsync("E2E supervisor remarks: report checked.");
        // Bootstrap custom checkbox: the input is visually hidden, users click its label.
        await Page.Locator("label[for=selected-case]").ClickAsync();
        await Expect(Page.Locator("#selected-case")).ToBeCheckedAsync();
        await Page.Locator("#allocate-case").ClickAsync();
        await ConfirmAsync("SUBMIT");
        await ExpectToastAsync("Report submitted to Company");
        await LogoutAsync();

        // Assessor decides.
        await LoginAsAsync(Role.Assessor);
        await Page.GotoAsync("/Assessor/Assess");
        row = await FindRowAsync(caseNumber);
        await (await RowElementAsync(row, "input[name=id]")).CheckAsync();
        await Page.Locator("#allocatedcase").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Assessor/ReportDetail", RegexOptions.IgnoreCase));

        if (withEvidence)
        {
            await AssertEvidenceShownAsync();

            // The system recommendation is pre-selected from the evidence validity.
            var recommended = Page.Locator(approve ? "#flexRadioDefault3" : "#flexRadioDefault1");
            await Expect(recommended).ToBeCheckedAsync();
            await Expect(Page.Locator($"label[for={(approve ? "flexRadioDefault3" : "flexRadioDefault1")}]"))
                .ToContainTextAsync("System recommendation");
        }

        await Page.Locator(approve ? "#flexRadioDefault3" : "#flexRadioDefault1").CheckAsync();
        await Page.Locator("#assessorRemarks").FillAsync(approve ? "E2E: approved." : "E2E: rejected.");
        await Page.Locator("#approve-case").ClickAsync();
        await ConfirmAsync(approve ? "APPROVE" : "REJECT");
        await ExpectToastAsync(approve ? "Approved" : "Rejected");

        await Page.GotoAsync(approve ? "/Assessor/Approved" : "/Assessor/Rejected");
        await FindRowAsync(caseNumber);
        await LogoutAsync();

        // Manager sees the outcome too.
        await LoginAsAsync(Role.Manager);
        await Page.GotoAsync(approve ? "/Manager/Approved" : "/Manager/Rejected");
        await FindRowAsync(caseNumber);
    }

    /// <summary>
    /// Uploads the evidence the APPROVE recommendation needs (required locations BENEFICIARY ADDRESS and CEMETERY:
    /// agent selfie, beneficiary face, Aadhaar card, death certificate) plus an optional audio file.
    /// AI services are faked in the app (USE_FAKE_EXTERNAL_SERVICES), see risk.control.system/Services/Fakes.
    /// </summary>
    private async Task UploadRequiredEvidenceAsync(bool agentSelfieMatches)
    {
        // Uploads send the browser location; without it every upload is stored as invalid.
        await Expect(Page.Locator("#latitude")).Not.ToHaveValueAsync("", new() { Timeout = 15_000 });

        const string beneficiaryAddress = "BENEFICIARY ADDRESS";
        const string cemetery = "CEMETERY";

        await UploadFaceAsync(beneficiaryAddress, ".upload-face-btn[data-isagent=true]", PersonImage);
        await UploadFaceAsync(beneficiaryAddress, ".upload-face-btn[data-name='Beneficiary Face']", PersonImage);
        await UploadDocumentAsync(beneficiaryAddress, "Aadhaar_Card", DocumentImage);
        await UploadMediaAsync(beneficiaryAddress, "Audio", new FilePayload
        {
            Name = "e2e-interview.mp3",
            MimeType = "audio/mpeg",
            Buffer = new byte[2048], // content is not inspected; transcription is faked
        });

        await UploadFaceAsync(cemetery, ".upload-face-btn[data-isagent=true]", agentSelfieMatches ? PersonImage : NoMatchImage());
        await UploadDocumentAsync(cemetery, "Death_Certificate", DocumentImage);
    }

    private async Task<ILocator> LocationSectionAsync(string locationName)
    {
        var section = Page.Locator(".location-container")
            .Filter(new() { Has = Page.Locator("h6", new() { HasTextString = locationName }) }).First;
        if (await section.CountAsync() == 0)
        {
            var names = await Page.Locator(".location-container h6").AllInnerTextsAsync();
            Assert.Fail($"Location '{locationName}' not on the agent page. Locations: [{string.Join(" | ", names.Select(n => n.Trim()))}]");
        }

        return section;
    }

    private async Task UploadFaceAsync(string locationName, string buttonSelector, string imagePath)
    {
        var button = (await LocationSectionAsync(locationName)).Locator(buttonSelector).First;
        // get-investigate.js reads the file from the button's table row.
        var input = button.Locator("xpath=ancestor::tr[1]").Locator("input.face-upload");
        await input.SetInputFilesAsync(imagePath);
        await ClickUploadAsync(button, "/AgentReport/SubmitFaceImage");
    }

    private async Task UploadDocumentAsync(string locationName, string reportName, string imagePath)
    {
        var button = (await LocationSectionAsync(locationName)).Locator($".upload-doc-btn[data-name='{reportName}']").First;
        var docId = await button.GetAttributeAsync("data-docid");
        await Page.Locator($"input.doc-upload[data-docid='{docId}']").SetInputFilesAsync(imagePath);
        await ClickUploadAsync(button, "/AgentReport/SubmitDocumentImage");
    }

    private async Task UploadMediaAsync(string locationName, string mediaName, FilePayload file)
    {
        var button = (await LocationSectionAsync(locationName)).Locator($".upload-media-btn[data-name='{mediaName}']").First;
        var mediaId = await button.GetAttributeAsync("data-docid");
        await Page.Locator($"input.media-upload[data-docid='{mediaId}']").SetInputFilesAsync(file);
        await ClickUploadAsync(button, "/AgentReport/SubmitMediaFile");
    }

    private async Task ClickUploadAsync(ILocator button, string endpoint)
    {
        await button.ScrollIntoViewIfNeededAsync();

        // get-investigate.js reports client-side problems (no file, wrong type) with alert().
        string? alertText = null;
        void OnDialog(object? _, IDialog d)
        {
            alertText = d.Message;
            _ = d.DismissAsync();
        }

        Page.Dialog += OnDialog;
        IResponse result;
        try
        {
            var response = Page.WaitForResponseAsync(r => r.Url.Contains(endpoint, StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
            await button.ClickAsync(new() { Timeout = 10_000 });
            try
            {
                result = await response;
            }
            catch (TimeoutException) when (alertText != null)
            {
                throw new AssertionException($"Upload to {endpoint} was not sent; page alert: \"{alertText}\"");
            }
        }
        finally
        {
            Page.Dialog -= OnDialog;
        }

        Assert.That(result.Status, Is.EqualTo(200), $"{endpoint} status");
        var body = await result.TextAsync();
        Assert.That(body, Does.Contain("\"success\":true").IgnoreCase, $"{endpoint} response: {body[..Math.Min(body.Length, 300)]}");
    }

    /// <summary>
    /// Agency and assessor report pages: evidence is rendered inside collapsed cards, so assert on text content.
    /// </summary>
    private async Task AssertEvidenceShownAsync()
    {
        var content = Page.Locator("body");
        // Fake OCR text (Services/Fakes/FakeGoogleOcrService.cs) and fake transcript (FakeSpeech2TextService.cs).
        await Expect(content).ToContainTextAsync("1234 5678 9012");
        await Expect(Page.Locator(".media-transcript").First).ToContainTextAsync("E2E transcript", new() { Timeout = 15_000 });
        // Agent / face photos are served from the case report.
        await Expect(Page.Locator("img[src*='/Document/GetAgentDocument/']").First).ToBeAttachedAsync();
        await Expect(Page.Locator("img[src*='/Document/GetFaceDocument/']").First).ToBeAttachedAsync();
    }

    /// <summary>
    /// A real JPEG with the fake's no-match marker appended after the end-of-image marker (decoders ignore it).
    /// </summary>
    private static string NoMatchImage()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "e2e-nomatch.jpeg");
        if (!File.Exists(path))
        {
            var bytes = File.ReadAllBytes(PersonImage).Concat(System.Text.Encoding.ASCII.GetBytes("E2E-NOMATCH")).ToArray();
            File.WriteAllBytes(path, bytes);
        }

        return path;
    }

    /// <summary>
    /// Creates a complete case (policy, customer, beneficiary) as the creator and auto-assigns it to the agency.
    /// Leaves the creator signed in. Returns the case (contract) number.
    /// </summary>
    private async Task<string> CreateAndAssignCaseAsync()
    {
        await EnsureCompanyAiDisabledAsync();

        var caseNumber = $"E2E{DateTime.UtcNow:MMddHHmmssfff}";

        await LoginAsAsync(Role.Creator);

        // Policy. The form is pre-filled with sample data; only the number and document are set here.
        await Page.GotoAsync("/AddAssign/New");
        await Page.Locator("a.create-policy").ClickAsync();
        await Page.Locator("#create-policy").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/AddAssign/Create", RegexOptions.IgnoreCase));
        await Page.Locator("#ContractNumber").FillAsync(caseNumber);
        await Page.Locator("#createImageInput").SetInputFilesAsync(DocumentImage); // last: enables #create
        await Page.Locator("#create").ClickAsync();
        await ConfirmAsync("Add Case");
        await Expect(Page).ToHaveURLAsync(new Regex("/Creator/Details", RegexOptions.IgnoreCase), new() { Timeout = 15_000 });

        // Customer
        await Page.Locator("#add-customer").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Customer/Create", RegexOptions.IgnoreCase));
        await Expect(Page.Locator("#PinCodeText")).Not.ToHaveValueAsync("", new() { Timeout = 15_000 });
        await Page.Locator("#createImageInput").SetInputFilesAsync(PersonImage);
        await Page.Locator("#create").ClickAsync();
        await ConfirmAsync("Add Customer");
        await Expect(Page).ToHaveURLAsync(new Regex("/Creator/Details", RegexOptions.IgnoreCase), new() { Timeout = 15_000 });

        // Beneficiary
        await Page.Locator("#add-beneficiary").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Beneficiary/Create", RegexOptions.IgnoreCase));
        await Expect(Page.Locator("#PinCodeText")).Not.ToHaveValueAsync("", new() { Timeout = 15_000 });
        await Page.Locator("#createImageInput").SetInputFilesAsync(PersonImage);
        await Page.Locator("#create").ClickAsync();
        await ConfirmAsync("Add Beneficiary");
        await Expect(Page).ToHaveURLAsync(new Regex("/Creator/Details", RegexOptions.IgnoreCase), new() { Timeout = 15_000 });

        // Auto-assign to the empanelled agency.
        await Page.Locator("#assign-list").ClickAsync();
        await ConfirmAsync("Assign");
        await Expect(Page).ToHaveURLAsync(new Regex("/CaseActive/Active", RegexOptions.IgnoreCase), new() { Timeout = 15_000 });
        await FindRowAsync(caseNumber);

        return caseNumber;
    }

    /// <summary>
    /// With AI enabled (the seeded default) the assessor page only links to an external AI service and has no
    /// approve/reject controls. Cases copy the company setting when assigned, so turn it off first, as portal admin.
    /// </summary>
    private async Task EnsureCompanyAiDisabledAsync()
    {
        if (_aiDisabled)
        {
            return;
        }

        await LoginAsAsync(Role.PortalAdmin);
        await Page.GotoAsync("/ClientCompany/Companies");
        var row = await FindRowAsync("insurer.com");
        await (await RowElementAsync(row, "a[id^=detail]")).ClickAsync();
        await Page.Locator("#edit-profile").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/ClientCompany/Edit", RegexOptions.IgnoreCase));

        var aiSwitch = Page.Locator("#AiEnabled");
        if (await aiSwitch.IsCheckedAsync())
        {
            await aiSwitch.UncheckAsync();
            await Page.Locator("#edit").ClickAsync();
            await ConfirmAsync("Save Company");
            await Expect(Page).Not.ToHaveURLAsync(new Regex("/ClientCompany/Edit", RegexOptions.IgnoreCase), new() { Timeout = 15_000 });
        }

        await LogoutAsync();
        _aiDisabled = true;
    }
}
