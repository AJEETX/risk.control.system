using Microsoft.AspNetCore.Identity;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class AustraliaSeed
    {
        public static async Task<string> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, UserManager<VendorApplicationUser> vendorUserManager,
            UserManager<ClientCompanyApplicationUser> clientUserManager, RoleManager<ApplicationRole> roleManager, ICustomApiCLient customApiCLient, IHttpContextAccessor httpAccessor,
            List<Country> countries, List<InvestigationServiceType> servicesTypes)
        {
            string COUNTRY_CODE = "AU";
            string PINCODE = "3131";
            var au = countries.FirstOrDefault(c => c.Code.ToLower() == COUNTRY_CODE.ToLower());
            var auPincodes = await PinCodeStateSeed.CsvRead_Au(0);
            var auStates = auPincodes.Where(s => s.StateCode.ToLower() == "vic"
                //|| s.StateCode.ToLower() == "qld"
                || s.StateCode.ToLower() == "nsw"
                ).Select(g => g.StateCode).Distinct()?.ToList();
            var filteredAuPincodes = auPincodes.Where(g => auStates.Contains(g.StateCode))?.ToList();
            await PinCodeStateSeed.SeedPincode(context, filteredAuPincodes, au);
            await context.SaveChangesAsync(null, false);
            var checker = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "checker.com", NAME = "Checker", PHOTO = "/img/checker.png", ADDRESSLINE = "57 Mahoneys Road", BRANCH = "Forest Hill", BANK = "NAB", PINCODE = PINCODE };
            var verify = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "verify.com", NAME = "Verify", PHOTO = "/img/verify.png", ADDRESSLINE = "67 Mahoneys Road", BRANCH = "Forest Hill", BANK = "CBA", PINCODE = PINCODE };
            var agencies = new List<SeedInput> { checker, verify };
            var vendors = new List<Vendor> { };
            foreach (var agency in agencies)
            {
                var vendor = await AgencySeed.Seed(context, webHostEnvironment, customApiCLient, vendorUserManager, agency, servicesTypes);
                vendors.Add(vendor);
            }
            var canara = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "insurer.com", NAME = "Insurer", PHOTO = "/img/insurer.jpg", ADDRESSLINE = "109 Mahoneys Road", BRANCH = "Forest Hill", BANK = "WPA", PINCODE = PINCODE };
            var companies = new List<SeedInput> { canara };
            foreach (var company in companies)
            {
                _ = await InsurerAllianz.Seed(context, vendors, webHostEnvironment, customApiCLient, clientUserManager, company);
            }
            await context.SaveChangesAsync(null, false);
            return PINCODE;
        }
    }
}