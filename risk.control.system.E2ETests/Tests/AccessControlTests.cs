using risk.control.system.E2ETests.Infrastructure;

namespace risk.control.system.E2ETests.Tests;

/// <summary>
/// Users must not reach pages of other roles. Access-denied redirects to /Account/Login.
/// </summary>
[TestFixture]
public class AccessControlTests : E2ETestBase
{
    public record Forbidden(Role Role, string Path)
    {
        public override string ToString() => $"{Role} -> {Path}";
    }

    private static readonly Forbidden[] ForbiddenPages =
    {
        new(Role.Creator, "/ClientCompany/Companies"),
        new(Role.Creator, "/Manager/Active"),
        new(Role.Creator, "/Assessor/Assess"),
        new(Role.Creator, "/ActiveAgency/Agencies"),
        new(Role.Assessor, "/AddAssign/New"),
        new(Role.Assessor, "/ManageCompanyUser/Users"),
        new(Role.Manager, "/ManageCompanyUser/Users"),
        new(Role.CompanyAdmin, "/AddAssign/New"),
        new(Role.AgencyAdmin, "/Manager/Active"),
        new(Role.AgencyAdmin, "/AddAssign/New"),
        new(Role.AgencyAdmin, "/ClientCompany/Companies"),
        new(Role.Agent, "/VendorInvestigation/Allocate"),
        new(Role.Agent, "/AgencyUser/Users"),
        new(Role.PortalAdmin, "/AddAssign/New"),
    };

    [TestCaseSource(nameof(ForbiddenPages))]
    public async Task Role_CannotOpen_OtherRolesPage(Forbidden forbidden)
    {
        await LoginAsAsync(forbidden.Role);

        await Page.GotoAsync(forbidden.Path);

        await Expect(Page).ToHaveURLAsync(LoginUrl);
    }
}
