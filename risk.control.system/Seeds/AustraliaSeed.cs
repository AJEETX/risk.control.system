using Microsoft.AspNetCore.Identity;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class AustraliaSeed
    {
        public static async Task<int> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager,
            ICustomApiClient customApiCLient, List<Country> countries, List<InvestigationServiceType> servicesTypes, IFileStorageService fileStorageService)
        {
            const string DOMAIN_SUFFIX = ".com";
            const string COUNTRY_CODE = "AU";
            const int PINCODE = 3131;
            var au = countries.FirstOrDefault(c => c.Code == COUNTRY_CODE);
            var auPincodes = await PinCodeStateSeed.CsvRead_Au(0);
            var auStates = auPincodes.Where(s =>
                string.Equals(s.StateCode, "vic", StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(s.StateCode, "qld", StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(s.StateCode, "nsw", StringComparison.OrdinalIgnoreCase)
                ).Select(g => g.StateCode).Distinct()?.ToList();
            var filteredAuPincodes = auPincodes.Where(g => auStates!.Contains(g.StateCode))?.ToList();
            await PinCodeStateSeed.SeedPincode(context, filteredAuPincodes!, au!);
            await context.SaveChangesAsync(null, false);
            var checker = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "checker" + DOMAIN_SUFFIX,
                NAME = "Checker Inc Australia",
                PHOTO = "/img/checker.png",
                ADDRESSLINE = "57 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "733112",
                BANK = "Westpac Banking Corporation",
                PHONE = "432854196"
            };
            var crucible = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "crucible" + DOMAIN_SUFFIX,
                NAME = "Crucible Inc Australia",
                PHOTO = "/img/crucible.jpg",
                ADDRESSLINE = "12 faulkner Street",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var cyber = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "cyber" + DOMAIN_SUFFIX,
                NAME = "Cyber Inc Australia",
                PHOTO = "/img/cyber.png",
                ADDRESSLINE = "94 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var honest = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "honest" + DOMAIN_SUFFIX,
                NAME = "Honest Inc Australia",
                PHOTO = "/img/honest.png",
                ADDRESSLINE = "117 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var hubris = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "hubris" + DOMAIN_SUFFIX,
                NAME = "Hubris Inc Australia",
                PHOTO = "/img/hubris.jpg",
                ADDRESSLINE = "12 faulkner Street",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var investigate = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "investigate" + DOMAIN_SUFFIX,
                NAME = "Investigate Inc Australia",
                PHOTO = "/img/investigate.png",
                ADDRESSLINE = "11 Barter Crescent",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var investigation = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "investigation" + DOMAIN_SUFFIX,
                NAME = "Investigation Inc Australia",
                PHOTO = "/img/investigation.png",
                ADDRESSLINE = "11 Jacana Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var nicer = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "nicer" + DOMAIN_SUFFIX,
                NAME = "Nicer Inc Australia",
                PHOTO = "/img/nicer.png",
                ADDRESSLINE = "45 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var proper = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "proper" + DOMAIN_SUFFIX,
                NAME = "Proper Inc Australia",
                PHOTO = "/img/proper.png",
                ADDRESSLINE = "11 Jacana Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var sample = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "sample" + DOMAIN_SUFFIX,
                NAME = "Sample Inc Australia",
                PHOTO = "/img/sample.png",
                ADDRESSLINE = "33 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var verify = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "verify" + DOMAIN_SUFFIX,
                NAME = "Verify Inc Australia",
                PHOTO = "/img/verify.png",
                ADDRESSLINE = "67 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };
            var zoom = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "zoom" + DOMAIN_SUFFIX,
                NAME = "Zoom Inc Australia",
                PHOTO = "/img/zoom.png",
                ADDRESSLINE = "12 Jackson Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PHONE = "432854196"
            };

            var agencies = new List<SeedInput> {
                checker,
                crucible,
                cyber
                ,honest, hubris, investigate, investigation, nicer, proper, sample, verify,  zoom
            };
            var vendors = new List<Vendor> { };
            foreach (var agency in agencies)
            {
                var vendor = await AgencySeed.Seed(context, webHostEnvironment, customApiCLient, userManager, agency, servicesTypes, fileStorageService);
                vendors.Add(vendor);
            }
            var insurer = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "insurer" + DOMAIN_SUFFIX,
                NAME = "Insurer Company Austalia",
                PHOTO = "/img/insurer.jpg",
                ADDRESSLINE = "109 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "733127",
                BANK = "Westpac Banking Corporation",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var companies = new List<SeedInput> { insurer };
            var agencies2Empanel = vendors.Take(vendors.Count / 2).ToList();
            foreach (var company in companies)
            {
                _ = await Insurer.Seed(context, agencies2Empanel, webHostEnvironment, customApiCLient, userManager, company, fileStorageService);
            }
            await context.SaveChangesAsync(null, false);
            var pincode = auPincodes.FirstOrDefault(p => p.Code == PINCODE);
            if (pincode == null)
            {
                return auPincodes.FirstOrDefault()!.Code;
            }
            return pincode.Code;
        }
    }
}