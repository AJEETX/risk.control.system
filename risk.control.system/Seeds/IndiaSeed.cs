using Microsoft.AspNetCore.Identity;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class IndiaSeed
    {
        public static async Task<int> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager,
            ICustomApiClient customApiCLient, List<Country> countries, List<InvestigationServiceType> servicesTypes, IFileStorageService fileStorageService)
        {
            string COUNTRY_CODE = "IN";
            var PINCODE = 122003;
            var india = countries.FirstOrDefault(c => c.Code == COUNTRY_CODE);
            var indiaPincodes = await PinCodeStateSeed.CsvRead_IndiaAsync();
            var indianStates = indiaPincodes
                .Where(s =>
                string.Equals(s.StateName, "haryana", StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(s.StateName, "delhi", StringComparison.OrdinalIgnoreCase)
                ||
                s.StateCode.Equals("up", StringComparison.CurrentCultureIgnoreCase)
                )
                .Select(g => g.StateCode).Distinct()?.ToList();
            var filteredInPincodes = indiaPincodes.Where(g => indianStates.Contains(g.StateCode))?.ToList();
            await PinCodeStateSeed.SeedPincode(context, filteredInPincodes, india);
            await context.SaveChangesAsync(null, false);

            var checker = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "checker.in",
                NAME = "Checker Inc India",
                PHOTO = "/img/checker.png",
                ADDRESSLINE = "12 MG Road",
                BRANCH = "Main Office",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var crucible = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "crucible.in",
                NAME = "Crucible Inc India",
                PHOTO = "/img/crucible.jpg",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var cyber = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "cyber.com",
                NAME = "Cyber Inc India",
                PHOTO = "/img/cyber.png",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var honest = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "honest.in",
                PHOTO = "/img/honest.png",
                NAME = "Honest Inc India",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var hubris = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "hubris.in",
                NAME = "Hubris Inc India",
                PHOTO = "/img/hubris.jpg",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var investigate = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "investigate.in",
                NAME = "Investigate Inc India",
                PHOTO = "/img/investigate.png",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var investigation = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "investigation.in",
                NAME = "Investigation Inc India",
                PHOTO = "/img/investigation.png",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var nicer = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "nicer.in",
                NAME = "Nicer Inc India",
                PHOTO = "/img/nicer.png",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var proper = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "proper.in",
                PHOTO = "/img/proper.png",
                NAME = "Proper Inc India",
                ADDRESSLINE = "12 MG Road",
                BRANCH = "Main Office",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var sample = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "sample.in",
                NAME = "Sample Inc India",
                PHOTO = "/img/sample.png",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var verify = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "verify.in",
                NAME = "Verify Inc India",
                PHOTO = "/img/verify.png",
                ADDRESSLINE = "12 MG Road",
                BRANCH = "Main Office",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
            var zoom = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = "zoom.in",
                NAME = "Zoom Inc India",
                PHOTO = "/img/zoom.png",
                ADDRESSLINE = "67 Mehrauli Road",
                BRANCH = "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };

            var agencies = new List<SeedInput> { checker, crucible, cyber, honest, hubris, investigate, investigation, nicer, proper, sample, verify, zoom };
            var vendors = new List<Vendor> { };

            foreach (var agency in agencies)
            {
                var vendor = await AgencySeed.Seed(context, webHostEnvironment, customApiCLient, userManager, agency, servicesTypes, fileStorageService);
                vendors.Add(vendor);
            }

            var insurer = new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "insurer.in",
                NAME = "Insurance Inc India",
                PHOTO = "/img/insurer.jpg",
                ADDRESSLINE = "139 Sector 44",
                BRANCH = "Head Office",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PINCODE = PINCODE,
                PHONE = "9876543210"
            };
            var companies = new List<SeedInput> { insurer };
            var agencies2Empanel = vendors.Take(vendors.Count / 2).ToList();
            foreach (var company in companies)
            {
                _ = await Insurer.Seed(context, agencies2Empanel, webHostEnvironment, customApiCLient, userManager, company, fileStorageService);
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