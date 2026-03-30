using Microsoft.AspNetCore.Identity;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class AustraliaSeed
    {
        private const string DOMAIN_SUFFIX = ".com";
        private const string COUNTRY_CODE = "AU";
        private const int PINCODE = 3131;

        public static async Task<int> Seed(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, ICustomApiClient customApiCLient, List<Country> countries,
            List<InvestigationServiceType> servicesTypes, IFileStorageService fileStorageService)
        {
            var au = countries.FirstOrDefault(c => c.Code == COUNTRY_CODE);
            var auPincodes = await PinCodeStateSeed.CsvRead_Au(0);
            var targetStates = new[] { "vic", "qld", "nsw" };
            var filteredAuPincodes = auPincodes.Where(p => targetStates.Contains(p.StateCode?.ToLower())).ToList();
            await PinCodeStateSeed.SeedPincode(context, filteredAuPincodes, au!);
            await context.SaveChangesAsync(null, false);
            var agencyData = new[]
            {
                new { Name = "Checker", Address = "57 Mahoneys Road", IFSC = "733112", Bank = "Westpac Banking Corporation" },
                new { Name = "Crucible", Address = "12 faulkner Street", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Cyber", Address = "94 Mahoneys Road", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Honest", Address = "117 Mahoneys Road", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Hubris", Address = "12 faulkner Street", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Investigate", Address = "11 Barter Crescent", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Investigation", Address = "11 Jacana Road", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Nicer", Address = "45 Mahoneys Road", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Proper", Address = "11 Jacana Road", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Sample", Address = "33 Mahoneys Road", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Verify", Address = "67 Mahoneys Road", IFSC = "083251", Bank = "National Australia Bank Limited" },
                new { Name = "Zoom", Address = "12 Jackson Road", IFSC = "083251", Bank = "National Australia Bank Limited" }
            };
            var vendors = new List<Vendor>();
            foreach (var item in agencyData)
            {
                var isJpg = item.Name == "Crucible" || item.Name == "Hubris";
                var input = new SeedInput
                {
                    COUNTRY = COUNTRY_CODE,
                    PINCODE = PINCODE,
                    DOMAIN = item.Name.ToLower() + DOMAIN_SUFFIX,
                    NAME = $"{item.Name} Inc Australia",
                    PHOTO = $"/img/{item.Name.ToLower()}.{(isJpg ? "jpg" : "png")}",
                    ADDRESSLINE = item.Address,
                    BRANCH = "Forest Hill",
                    IFSC = item.IFSC,
                    BANK = item.Bank,
                    PHONE = "432854196"
                };
                vendors.Add(await AgencySeed.Seed(context, env, customApiCLient, userManager, input, servicesTypes, fileStorageService));
            }
            var insurerInput = GetInsurer();
            await Insurer.Seed(context, vendors.Take(vendors.Count / 2).ToList(), env, customApiCLient, userManager, insurerInput, fileStorageService);
            await context.SaveChangesAsync(null, false);
            return auPincodes.FirstOrDefault(p => p.Code == PINCODE)?.Code ?? auPincodes.First().Code;
        }
        private static SeedInput GetInsurer()
        {
            return new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = "insurer" + DOMAIN_SUFFIX,
                NAME = "Insurer Company Australia",
                PHOTO = "/img/insurer.jpg",
                ADDRESSLINE = "109 Mahoneys Road",
                BRANCH = "Forest Hill",
                IFSC = "733127",
                BANK = "Westpac Banking Corporation",
                PINCODE = PINCODE,
                PHONE = "432854196"
            };
        }
    }
}