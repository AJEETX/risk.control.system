using Microsoft.AspNetCore.Identity;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class StartCountryWiseSeed
    {
        static string randomPinCode = string.Empty;
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, UserManager<VendorApplicationUser> vendorUserManager,
            UserManager<ClientCompanyApplicationUser> clientUserManager, RoleManager<ApplicationRole> roleManager, ICustomApiCLient customApiCLient, IHttpContextAccessor httpAccessor)
        {
            var globalSetting = new GlobalSettings
            {
            };
            var newGlobalSetting = await context.GlobalSettings.AddAsync(globalSetting);
            await context.SaveChangesAsync(null, false);
            var servicesTypes = await ServiceTypeSeed.Seed(context);

            await PinCodeStateSeed.CurrenciesCode(context);
            await PinCodeStateSeed.Currencies(context);
            var countries = await PinCodeStateSeed.Countries(context);
            //randomPinCode = await IndiaSeed.Seed(context, webHostEnvironment, userManager, vendorUserManager, clientUserManager, roleManager, customApiCLient, httpAccessor, countries, servicesTypes);
            randomPinCode = await AustraliaSeed.Seed(context, webHostEnvironment, userManager, vendorUserManager, clientUserManager, roleManager, customApiCLient, httpAccessor, countries, servicesTypes);

            // seed USA
            //var us = countries.FirstOrDefault(c => c.Code.ToLower() == "us");
            //var usPincodes = await PinCodeStateSeed.CsvRead_Us();
            //var usStates = usPincodes.Where(s => s.StateCode.ToLower() == "nc" ||
            //    s.StateCode.ToLower() == "ny"
            //    ).Select(g => g.StateCode).Distinct()?.ToList();
            //var filteredUsPincodes = usPincodes.Where(g => usStates.Contains(g.StateCode))?.ToList();
            //await PinCodeStateSeed.SeedPincode(context, filteredUsPincodes, us);

            await PortalAdminSeed.Seed(context, webHostEnvironment, userManager, roleManager, randomPinCode);
        }
    }
}