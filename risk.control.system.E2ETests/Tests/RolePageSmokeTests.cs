using risk.control.system.E2ETests.Infrastructure;

namespace risk.control.system.E2ETests.Tests;

/// <summary>
/// For every seeded role, opens each page from that role's sidebar and checks it renders:
/// no error page, the main element is visible, and the page's data endpoint (if any) returns 200.
/// Read-only.
/// </summary>
[TestFixture]
public class RolePageSmokeTests : E2ETestBase
{
    public record PageCase(Role Role, string Path, string Selector, string? DataEndpoint = null)
    {
        public override string ToString() => $"{Role} {Path}";
    }

    private const string Table = "#dataTable";
    private const string CreateForm = "#create-form";

    private static readonly PageCase[] Pages =
    {
        // Dashboard for every role
        new(Role.PortalAdmin, "/Dashboard/Index", ".small-box"),
        new(Role.CompanyAdmin, "/Dashboard/Index", ".small-box", "/DashboardGraph/GetWeeklyClaim"),
        new(Role.Manager, "/Dashboard/Index", ".small-box", "/DashboardGraph/GetWeeklyClaim"),
        new(Role.Creator, "/Dashboard/Index", ".small-box", "/DashboardGraph/GetWeeklyClaim"),
        new(Role.Assessor, "/Dashboard/Index", ".small-box", "/DashboardGraph/GetWeeklyClaim"),
        new(Role.AgencyAdmin, "/Dashboard/Index", ".small-box"),
        new(Role.Agent, "/Dashboard/Index", ".small-box"),

        // Portal admin
        new(Role.PortalAdmin, "/ClientCompany/Companies", Table, "/api/PortalCompany/AllCompanies"),
        new(Role.PortalAdmin, "/ClientCompany/Create", CreateForm),
        new(Role.PortalAdmin, "/ClientCompany/Agencies", Table, "/api/Agency/AllAgencies"),
        new(Role.PortalAdmin, "/VendorInvoice", "#invoiceTable", "/VendorInvoice/GetInvoicesJson"),
        new(Role.PortalAdmin, "/User/Index", Table, "/api/User/AllUsers"),
        new(Role.PortalAdmin, "/Roles/Index", Table),
        new(Role.PortalAdmin, "/Form/CreateForm", "#designForm"),
        new(Role.PortalAdmin, "/OccupationType/Profile", Table, "/OccupationType/GetOccupations"),
        new(Role.PortalAdmin, "/EducationType/Profile", Table, "/EducationType/GetEducations"),
        new(Role.PortalAdmin, "/AnnualIncome/Profile", Table, "/AnnualIncome/GetAnnualIncomes"),
        new(Role.PortalAdmin, "/CaseEnabler/Profile", Table, "/CaseEnabler/GetCaseEnablers"),
        new(Role.PortalAdmin, "/CostCentre/Profile", Table, "/CostCentre/GetCostCentres"),
        new(Role.PortalAdmin, "/BeneficiaryRelation/Profile", Table, "/BeneficiaryRelation/GetBeneficiaryRelations"),
        new(Role.PortalAdmin, "/Country/Profile", Table, "/Country/GetCountries"),
        new(Role.PortalAdmin, "/State/Profile", Table, "/State/GetStates"),
        new(Role.PortalAdmin, "/District/Profile", Table, "/District/GetDistricts"),
        new(Role.PortalAdmin, "/PinCodes/Profile", Table, "/Pincodes/GetPincodes"),
        new(Role.PortalAdmin, "/ErrorLog/ErrorLog", "table.table"),

        // Company admin
        new(Role.CompanyAdmin, "/ManageCompany/CompanyProfile", ".card-title"),
        new(Role.CompanyAdmin, "/ManageCompanyUser/Users", Table, "/api/Company/Users"),
        new(Role.CompanyAdmin, "/ManageCompanyUser/Create", CreateForm),
        new(Role.CompanyAdmin, "/CaseEnabler/Profile", Table, "/CaseEnabler/GetCaseEnablers"),
        new(Role.CompanyAdmin, "/CostCentre/Profile", Table, "/CostCentre/GetCostCentres"),
        new(Role.CompanyAdmin, "/BeneficiaryRelation/Profile", Table, "/BeneficiaryRelation/GetBeneficiaryRelations"),
        new(Role.CompanyAdmin, "/ReportTemplate/Profile", "#reportTemplatesTable", "/ReportTemplate/GetReportTemplates"),
        new(Role.CompanyAdmin, "/Audit/Profile", Table, "/Audit/GetAudit"),
        new(Role.CompanyAdmin, "/SmsForCase/Profile", Table, "/SmsForCase/GetSms"),
        new(Role.CompanyAdmin, "/Country/Profile", Table, "/Country/GetCountries"),
        new(Role.CompanyAdmin, "/State/Profile", Table, "/State/GetStates"),
        new(Role.CompanyAdmin, "/District/Profile", Table, "/District/GetDistricts"),
        new(Role.CompanyAdmin, "/PinCodes/Profile", Table, "/Pincodes/GetPincodes"),
        new(Role.CompanyAdmin, "/CompanyUserProfile/ChangePassword", CreateForm),

        // Manager
        new(Role.Manager, "/Manager/Active", Table, "/api/Manager/GetActiveCases"),
        new(Role.Manager, "/Manager/Approved", Table, "/api/Manager/GetApprovedCases"),
        new(Role.Manager, "/Manager/Rejected", Table, "/api/Manager/GetRejectedCases"),
        new(Role.Manager, "/CaseUpload/Uploads", Table, "/api/Investigation/GetFilesData"),
        new(Role.Manager, "/ActiveAgency/Agencies", Table, "/api/Company/GetAllEmpanelledAgencies"),
        new(Role.Manager, "/AvailableAgency/Agencies", Table, "/api/Company/AvailableAgency"),
        new(Role.Manager, "/Vendors/Create", CreateForm),

        // Creator
        new(Role.Creator, "/CaseUpload/Uploads", Table, "/api/Investigation/GetFilesData"),
        new(Role.Creator, "/AddAssign/New", Table, "/api/Investigation/GetAuto"),
        new(Role.Creator, "/CaseActive/Active", Table, "/api/Investigation/GetActive"),
        new(Role.Creator, "/CompanyUserProfile/ChangePassword", CreateForm),

        // Assessor
        new(Role.Assessor, "/Assessor/Assess", Table, "/api/Assessor/GetInvestigationReports"),
        new(Role.Assessor, "/Assessor/Review", Table, "/api/Assessor/GetReviewCases"),
        new(Role.Assessor, "/Assessor/Approved", Table, "/api/Assessor/GetApprovededCases"),
        new(Role.Assessor, "/Assessor/Rejected", Table, "/api/Assessor/GetRejectedCases"),

        // Agency admin
        new(Role.AgencyAdmin, "/VendorInvestigation/Allocate", Table, "/api/agency/VendorInvestigation/GetNewCases"),
        new(Role.AgencyAdmin, "/VendorInvestigation/AgentReport", Table, "/api/agency/VendorInvestigation/GetAgentReports"),
        new(Role.AgencyAdmin, "/VendorInvestigation/Active", Table, "/api/agency/VendorInvestigation/GetOpenCases"),
        new(Role.AgencyAdmin, "/VendorInvestigation/Completed", Table, "/api/agency/VendorInvestigation/GetCompleted"),
        new(Role.AgencyAdmin, "/Agency/Profile", ".card-title"),
        new(Role.AgencyAdmin, "/AgencyService/Service", Table, "/api/Agency/AllServices"),
        new(Role.AgencyAdmin, "/AgencyService/Create", CreateForm),
        new(Role.AgencyAdmin, "/AgencyUser/Users", Table, "/api/Agency/GetUsers"),
        new(Role.AgencyAdmin, "/AgencyUser/Create", CreateForm),
        new(Role.AgencyAdmin, "/AgencyUserProfile/ChangePassword", CreateForm),

        // Agent
        new(Role.Agent, "/Agent/Agent", Table, "/api/agency/agent/GetNewCases"),
        new(Role.Agent, "/Agent/Submitted", Table, "/api/agency/agent/GetSubmittedCases"),
    };

    [TestCaseSource(nameof(Pages))]
    public async Task Page_Loads(PageCase page)
    {
        await LoginAsAsync(page.Role);

        Task<IResponse>? data = page.DataEndpoint is null
            ? null
            : Page.WaitForResponseAsync(
                r => r.Url.Contains(page.DataEndpoint, StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 20_000 });

        var response = await Page.GotoAsync(page.Path);
        Assert.That(response?.Status, Is.LessThan(400), $"GET {page.Path}");

        await Expect(Page.Locator(page.Selector).First).ToBeVisibleAsync();

        if (data != null)
        {
            var dataResponse = await data;
            Assert.That(dataResponse.Status, Is.EqualTo(200), $"Data endpoint {page.DataEndpoint}");
        }

        await AssertHealthyPageAsync(page.Path);
    }

    private static IEnumerable<PageCase> SearchablePages =>
        Pages.Where(p => p.Selector == Table && p.DataEndpoint != null && p.DataEndpoint.StartsWith("/api/", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Typing in the table search box sends the term to the server. Queries that EF Core cannot translate
    /// fail even when the table is empty, so this catches them without test data.
    /// </summary>
    [TestCaseSource(nameof(SearchablePages))]
    public async Task Table_Search_DoesNotError(PageCase page)
    {
        await LoginAsAsync(page.Role);
        await Page.GotoAsync(page.Path);
        await Expect(Page.Locator(Table)).ToBeVisibleAsync();

        var search = Page.Locator("#dataTable_filter input");
        if (await search.CountAsync() == 0)
        {
            Assert.Ignore("No search box on this table.");
        }

        var searched = Page.WaitForResponseAsync(
            r => r.Url.Contains(page.DataEndpoint!, StringComparison.OrdinalIgnoreCase)
                 && r.Url.Contains("e2esearch", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 15_000 });
        await search.FillAsync("e2esearch");

        IResponse? response = null;
        try
        {
            response = await searched;
        }
        catch (TimeoutException)
        {
            // Client-side table: searching does not call the server.
        }

        if (response != null)
        {
            Assert.That(response.Status, Is.EqualTo(200), $"Search request to {page.DataEndpoint}");
        }

        await AssertHealthyPageAsync(page.Path);
    }
}
