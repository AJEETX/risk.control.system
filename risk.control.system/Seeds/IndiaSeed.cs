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
            var agencyNames = new[] { "Checker", "Crucible", "Cyber", "Honest", "Hubris", "Investigate", "Investigation", "Nicer", "Proper", "Sample", "Verify", "Zoom" };
            var vendors = new List<Vendor>();
            foreach (var name in agencyNames)
            {
                var agencyInput = CreateSeedInput(name, name.Equals("proper", StringComparison.CurrentCultureIgnoreCase) ||
                    name.Equals("checker", StringComparison.CurrentCultureIgnoreCase) || name.Equals("verify", StringComparison.CurrentCultureIgnoreCase)
                    ? "12 MG Road"
                    : "67 Mehrauli Road");
                var vendor = await AgencySeed.Seed(ctx, env, apiClient, userManager, agencyInput, servicesTypes, fileStorageService);
                vendors.Add(vendor);
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
        private static SeedInput CreateSeedInput(string shortName, string address)
        {
            var isJpg = shortName.Equals("Crucible", StringComparison.OrdinalIgnoreCase) ||
                        shortName.Equals("Hubris", StringComparison.OrdinalIgnoreCase);

            return new SeedInput
            {
                COUNTRY = COUNTRY_CODE,
                PINCODE = PINCODE,
                DOMAIN = shortName.ToLower() + DOMAIN_SUFFIX,
                NAME = $"{shortName} Inc India",
                PHOTO = $"/img/{shortName.ToLower()}.{(isJpg ? "jpg" : "png")}",
                ADDRESSLINE = address,
                BRANCH = address.Contains("MG Road") ? "Main Office" : "Gurgaon",
                IFSC = "SBIN0001234",
                BANK = "State Bank of India",
                PHONE = "9876543210"
            };
        }
    }
}