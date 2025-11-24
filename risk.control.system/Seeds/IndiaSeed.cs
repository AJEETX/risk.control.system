using Microsoft.AspNetCore.Identity;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class IndiaSeed
    {
        public static async Task<string> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, UserManager<VendorApplicationUser> vendorUserManager,
            UserManager<ClientCompanyApplicationUser> clientUserManager, RoleManager<ApplicationRole> roleManager, ICustomApiCLient customApiCLient, IHttpContextAccessor httpAccessor,
            List<Country> countries, List<InvestigationServiceType> servicesTypes)
        {
            string COUNTRY_CODE = "IN";
            string PINCODE = "122003";
            var india = countries.FirstOrDefault(c => c.Code.ToLowerInvariant() == COUNTRY_CODE.ToLowerInvariant());
            var indiaPincodes = await PinCodeStateSeed.CsvRead_IndiaAsync();
            var indianStates = indiaPincodes
                //.Where(s =>
                //s.StateName.ToLowerInvariant() == "haryana" ||
                //s.StateName.ToLowerInvariant() == "delhi" ||
                //s.StateCode.ToLowerInvariant() == "up"
                //)
                .Select(g => g.StateCode).Distinct()?.ToList();
            var filteredInPincodes = indiaPincodes.Where(g => indianStates.Contains(g.StateCode))?.ToList();
            await PinCodeStateSeed.SeedPincode(context, filteredInPincodes, india);
            await context.SaveChangesAsync(null, false);

            var proper = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "proper.com",
                PHOTO = "/img/proper.png",
                NAME = "Proper",
                ADDRESSLINE = "12 MG Road",
                BRANCH = "Main Office",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PINCODE = PINCODE
            };
            var honest = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "honest.com",
                PHOTO = "/img/honest.png",
                NAME = "Honest",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PINCODE = PINCODE
            };
            var agencies = new List<SeedInput> { proper, honest };
            var vendors = new List<Vendor> { };

            foreach (var agency in agencies)
            {
                var vendor = await AgencySeed.Seed(context, webHostEnvironment, customApiCLient, vendorUserManager, agency, servicesTypes);
                vendors.Add(vendor);
            }

            var insurer = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "can-hsbc.com",
                NAME = "Can-Hsbc",
                PHOTO = "/img/insurer.jpg",
                ADDRESSLINE = "139 Sector 44",
                BRANCH = "Head Office",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PINCODE = PINCODE
            };
            var companies = new List<SeedInput> { insurer };
            foreach (var company in companies)
            {
                _ = await InsurerAllianz.Seed(context, vendors, webHostEnvironment, customApiCLient, clientUserManager, company);
            }
            await context.SaveChangesAsync(null, false);
            var pincode = indiaPincodes.FirstOrDefault(p => p.Code == PINCODE);
            if (pincode == null)
            {
                return indiaPincodes.FirstOrDefault().Code;
            }
            return pincode.Code;
        }
    }
}