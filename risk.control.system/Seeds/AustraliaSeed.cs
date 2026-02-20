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
            string COUNTRY_CODE = "AU";
            int PINCODE = 3131;
            var au = countries.FirstOrDefault(c => c.Code == COUNTRY_CODE);
            var auPincodes = await PinCodeStateSeed.CsvRead_Au(0);
            var auStates = auPincodes.Where(s =>
                s.StateCode == "VIC"
                //||
                //s.StateCode.ToLower() == "qld" ||
                //s.StateCode.ToLower() == "nsw"
                ).Select(g => g.StateCode).Distinct()?.ToList();
            var filteredAuPincodes = auPincodes.Where(g => auStates.Contains(g.StateCode))?.ToList();
            await PinCodeStateSeed.SeedPincode(context, filteredAuPincodes, au);
            await context.SaveChangesAsync(null, false);
            var checker = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "checker.com",
                NAME = "Checker",
                PHOTO = "/img/checker.png",
                ADDRESSLINE = "57 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "733112",
                BANK = "Westpac Banking Corporation",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var verify = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "verify.com",
                NAME = "Verify",
                PHOTO = "/img/verify.png",
                ADDRESSLINE = "67 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var honest = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "honest.com",
                NAME = "Honest Inc",
                PHOTO = "/img/honest.png",
                ADDRESSLINE = "117 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var investigate = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "investigate.com",
                NAME = "Investigate Inc",
                PHOTO = "/img/investigate.png",
                ADDRESSLINE = "11 Barter Crescent",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var investigation = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "investigation.com",
                NAME = "Investigation Inc",
                PHOTO = "/img/investigation.png",
                ADDRESSLINE = "11 Jacana Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var proper = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "proper.com",
                NAME = "Proper Inc",
                PHOTO = "/img/proper.png",
                ADDRESSLINE = "11 Jacana Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };

            var nicer = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "nicer.com",
                NAME = "Nicer Inc",
                PHOTO = "/img/nicer.png",
                ADDRESSLINE = "45 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };

            var sample = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "sample.com",
                NAME = "Sample Inc",
                PHOTO = "/img/sample.png",
                ADDRESSLINE = "33 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };

            var cyber = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "cyber.com",
                NAME = "Cyber Inc",
                PHOTO = "/img/cyber.png",
                ADDRESSLINE = "94 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var zoom = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "zoom.com",
                NAME = "Zoom Inc",
                PHOTO = "/img/zoom.png",
                ADDRESSLINE = "12 Jackson Road",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var crucible = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "crucible.com",
                NAME = "Crucible Inc",
                PHOTO = "/img/crucible.jpg",
                ADDRESSLINE = "12 faulkner Street",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var hubris = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "hubris.com",
                NAME = "Hubris Inc",
                PHOTO = "/img/hubris.jpg",
                ADDRESSLINE = "12 faulkner Street",
                BRANCH = "Forest Hill",
                IFSC = "083251",
                BANK = "National Australia Bank Limited",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var agencies = new List<SeedInput> { checker, verify, honest, investigate, investigation, proper, nicer, sample, cyber, zoom, crucible, hubris };
            var vendors = new List<Vendor> { };
            foreach (var agency in agencies)
            {
                var vendor = await AgencySeed.Seed(context, webHostEnvironment, customApiCLient, userManager, agency, servicesTypes, fileStorageService);
                vendors.Add(vendor);
            }
            var insurer = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "insurer.com",
                NAME = "Insurer",
                PHOTO = "/img/insurer.jpg",
                ADDRESSLINE = "109 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "733127",
                BANK = "Westpac Banking Corporation",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
            var companies = new List<SeedInput> { insurer };
            foreach (var company in companies)
            {
                _ = await Insurer.Seed(context, vendors, webHostEnvironment, customApiCLient, userManager, company, fileStorageService);
            }
            await context.SaveChangesAsync(null, false);
            return PINCODE;
        }
    }
}