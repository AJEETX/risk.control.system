using Microsoft.AspNetCore.Identity;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class IndiaSeed
    {
        private const int PINCODE = 122003;
        private const string DOMAIN_SUFFIX = ".in";
        private const string COUNTRY_CODE = "IN";

        public static async Task<int> Seed(ApplicationDbContext ctx, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, ICustomApiClient apiClient, List<Country> countries, List<InvestigationServiceType> servicesTypes, IFileStorageService fileStorageService)
        {
            var india = countries.FirstOrDefault(c => c.Code == COUNTRY_CODE);
            var indiaPincodes = await PinCodeStateSeed.CsvRead_IndiaAsync();
            var targetStates = new[] { "haryana", "delhi", "up" };
            var indianStates = indiaPincodes
                .Where(s => targetStates.Contains(s.StateName?.ToLower()) || targetStates.Contains(s.StateCode?.ToLower()))
                .Select(g => g.StateCode)
                .Distinct()
                .ToList();
            var filteredInPincodes = indiaPincodes.Where(g => indianStates.Contains(g.StateCode)).ToList();
            await PinCodeStateSeed.SeedPincode(ctx, filteredInPincodes, india!);
            await ctx.SaveChangesAsync(null, false);
            var agencyData = new[]
            {
                new { Name = "Checker", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Crucible", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Cyber", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Honest", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Hubris", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Investigate", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Investigation", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Nicer", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Proper", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Sample", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Verify", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" },
                new { Name = "Zoom", Address = "12 MG Road", IFSC = "SBIN0001234", Bank = "State Bank of India" }
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
                    NAME = $"{item.Name} Inc India",
                    PHOTO = $"/img/{item.Name.ToLower()}.{(isJpg ? "jpg" : "png")}",
                    ADDRESSLINE = item.Address,
                    BRANCH = "Main Office",
                    IFSC = item.IFSC,
                    BANK = item.Bank,
                    PHONE = "9876543210"
                };
                vendors.Add(await AgencySeed.Seed(ctx, env, apiClient, userManager, input, servicesTypes, fileStorageService));
            }
            var agenciesToEmpanel = vendors.Take(vendors.Count / 2).ToList();
            await Insurer.Seed(ctx, agenciesToEmpanel, env, apiClient, userManager, GetInsurer(), fileStorageService);
            await ctx.SaveChangesAsync(null, false);
            return indiaPincodes.FirstOrDefault(p => p.Code == PINCODE)?.Code ?? indiaPincodes.First().Code;
        }
        private static SeedInput GetInsurer()
        {
            return new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                DOMAIN = $"insurer{DOMAIN_SUFFIX}",
                NAME = "Insurance Company India",
                PHOTO = "/img/insurer.jpg",
                ADDRESSLINE = "139 Sector 44",
                BRANCH = "Head Office",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PINCODE = PINCODE,
                PHONE = "9876543210"
            };
        }
    }
}