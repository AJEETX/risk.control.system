using Microsoft.AspNetCore.Identity;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class StartCountryWiseSeed
    {
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

            // seed INDIA
            string COUNTRY_CODE = "IN";
            string PINCODE = "122003";
            //var india = countries.FirstOrDefault(c => c.Code.ToLower() == COUNTRY_CODE.ToLower());
            //var indiaPincodes = await PinCodeStateSeed.CsvRead_India();
            //var indianStates = indiaPincodes.Where(s =>
            //    s.StateName.ToLower() == "haryana"
            //    ||
            //    s.StateName.ToLower() == "delhi"
            //    ||
            //    s.StateCode.ToLower() == "up"
            //    ).Select(g => g.StateCode).Distinct()?.ToList();
            //var filteredInPincodes = indiaPincodes.Where(g => indianStates.Contains(g.StateCode))?.ToList();
            //await PinCodeStateSeed.SeedPincode(context, filteredInPincodes, india);
            //await context.SaveChangesAsync(null, false);

            var proper = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "proper.com", PHOTO = "/img/proper.png", NAME = "Proper", ADDRESSLINE = "12 MG Road", BRANCH = "Main Office", BANK = "SBI", PINCODE = PINCODE };
            var honest = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "honest.com", PHOTO = "/img/honest.png", NAME = "Honest", ADDRESSLINE = "67 Mehrauli Road", BRANCH = "Gurgaon", BANK = "ICICI", PINCODE = PINCODE };
            var agencies = new List<SeedInput> { proper, honest };
            var vendors = new List<Vendor> { };

            //foreach (var agency in agencies)
            //{
            //    var vendor = await AgencyCheckerSeed.Seed(context, webHostEnvironment, customApiCLient, vendorUserManager, agency, servicesTypes);
            //    vendors.Add(vendor);
            //}

            var insurer = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "canara.com", NAME = "Canara", PHOTO = "/img/insurer.jpg", ADDRESSLINE = "139 Sector 44", BRANCH = "Head Office", BANK = "HDFC", PINCODE = PINCODE };
            var companies = new List<SeedInput> { insurer };
            //foreach (var company in companies)
            //{
            //    _ = await InsurerAllianz.Seed(context, vendors, webHostEnvironment, customApiCLient, clientUserManager, company);
            //}
            //await context.SaveChangesAsync(null, false);

            // end seed INDIA

            // seed AUSTRALIA
            COUNTRY_CODE = "AU";
            PINCODE = "3131";
            var au = countries.FirstOrDefault(c => c.Code.ToLower() == COUNTRY_CODE.ToLower());
            var auPincodes = await PinCodeStateSeed.CsvRead_Au(0);
            var auStates = auPincodes.Where(s => s.StateCode.ToLower() == "vic"
                //#if !DEBUG

                || s.StateCode.ToLower() == "qld"
                || s.StateCode.ToLower() == "nsw"
                //#endif

                ).Select(g => g.StateCode).Distinct()?.ToList();
            var filteredAuPincodes = auPincodes.Where(g => auStates.Contains(g.StateCode))?.ToList();
            await PinCodeStateSeed.SeedPincode(context, filteredAuPincodes, au);
            await context.SaveChangesAsync(null, false);
            var checker = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "checker.com", NAME = "Checker", PHOTO = "/img/checker.png", ADDRESSLINE = "57 Mahoneys Road", BRANCH = "Forest Hill", BANK = "NAB", PINCODE = PINCODE };
            var verify = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "verify.com", NAME = "Verify", PHOTO = "/img/verify.png", ADDRESSLINE = "67 Mahoneys Road", BRANCH = "Forest Hill", BANK = "CBA", PINCODE = PINCODE };
            agencies = new List<SeedInput> { checker, verify };
            vendors = new List<Vendor> { };
            foreach (var agency in agencies)
            {
                var vendor = await AgencyCheckerSeed.Seed(context, webHostEnvironment, customApiCLient, vendorUserManager, agency, servicesTypes);
                vendors.Add(vendor);
            }
            var canara = new SeedInput { COUNTRY = COUNTRY_CODE, DOMAIN = "insurer.com", NAME = "Insurer", PHOTO = "/img/insurer.jpg", ADDRESSLINE = "109 Mahoneys Road", BRANCH = "Forest Hill", BANK = "WPA", PINCODE = PINCODE };
            companies = new List<SeedInput> { canara };
            foreach (var company in companies)
            {
                _ = await InsurerAllianz.Seed(context, vendors, webHostEnvironment, customApiCLient, clientUserManager, company);
            }
            await context.SaveChangesAsync(null, false);

            // end seed AUSTRALIA

            // seed USA
            //var us = countries.FirstOrDefault(c => c.Code.ToLower() == "us");
            //var usPincodes = await PinCodeStateSeed.CsvRead_Us();
            //var usStates = usPincodes.Where(s => s.StateCode.ToLower() == "nc" ||
            //    s.StateCode.ToLower() == "ny"
            //    ).Select(g => g.StateCode).Distinct()?.ToList();
            //var filteredUsPincodes = usPincodes.Where(g => usStates.Contains(g.StateCode))?.ToList();
            //await PinCodeStateSeed.SeedPincode(context, filteredUsPincodes, us);

            var randomPinCode = filteredAuPincodes.FirstOrDefault();

            await PortalAdminSeed.Seed(context, webHostEnvironment, userManager, roleManager, randomPinCode.Code);
        }
    }
}