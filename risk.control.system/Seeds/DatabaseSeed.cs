using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class DatabaseSeed
    {
        public static async Task SeedDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var vendorUserManager = scope.ServiceProvider.GetRequiredService<UserManager<VendorApplicationUser>>();
            var clientUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ClientCompanyApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var customApiCLient = scope.ServiceProvider.GetRequiredService<ICustomApiCLient>();
            var httpAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            context.Database.EnsureCreated();

            context.Database.Migrate();

            //check for users
            if (context.ApplicationUser.Any())
            {
                return; //if user is not empty, DB has been seed
            }

            //CREATE ROLES
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.PORTAL_ADMIN.ToString().Substring(0, 2).ToUpper(), AppRoles.PORTAL_ADMIN.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.COMPANY_ADMIN.ToString().Substring(0, 2).ToUpper(), AppRoles.COMPANY_ADMIN.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.AGENCY_ADMIN.ToString().Substring(0, 2).ToUpper(), AppRoles.AGENCY_ADMIN.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.CREATOR.ToString().Substring(0, 2).ToUpper(), AppRoles.CREATOR.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.MANAGER.ToString().Substring(0, 2).ToUpper(), AppRoles.MANAGER.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.ASSESSOR.ToString().Substring(0, 2).ToUpper(), AppRoles.ASSESSOR.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.SUPERVISOR.ToString().Substring(0, 2).ToUpper(), AppRoles.SUPERVISOR.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.AGENT.ToString().Substring(0, 2).ToUpper(), AppRoles.AGENT.ToString()));

            await PinCodeStateSeed.CurrenciesCode(context);
            await PinCodeStateSeed.Currencies(context);
            var countries = await PinCodeStateSeed.Countries(context);

            //#if !DEBUG
            // seed INDIA
            var india = countries.FirstOrDefault(c => c.Code.ToLower() == "in");
            var indiaPincodes = await PinCodeStateSeed.CsvRead_India();
            var indianStates = indiaPincodes.Where(s =>
                s.StateName.ToLower() == "haryana"
                ||
                s.StateName.ToLower() == "delhi"
                ||
                s.StateCode.ToLower() == "up"
                ).Select(g => g.StateCode).Distinct()?.ToList();

            var filteredInPincodes = indiaPincodes.Where(g => indianStates.Contains(g.StateCode))?.ToList();

            await PinCodeStateSeed.SeedPincode(context, filteredInPincodes, india);

            //var auPincodes = await PinCodeStateSeed.CsvRead_Au(maxRowCountForDebug);
            //var auStates = auPincodes.Where(s => s.StateCode.ToLower() == "vic"
            //    //#if !DEBUG

            //    || s.StateCode.ToLower() == "qld"
            //    || s.StateCode.ToLower() == "nsw"
            //    //#endif

            //    ).Select(g => g.StateCode).Distinct()?.ToList();

            //var filteredAuPincodes = auPincodes.Where(g => auStates.Contains(g.StateCode))?.ToList();

            //await PinCodeStateSeed.SeedPincode(context, filteredAuPincodes, au);

            // seed USA
            //var us = countries.FirstOrDefault(c => c.Code.ToLower() == "us");
            //var usPincodes = await PinCodeStateSeed.CsvRead_Us();
            //var usStates = usPincodes.Where(s => s.StateCode.ToLower() == "nc" ||
            //    s.StateCode.ToLower() == "ny"
            //    ).Select(g => g.StateCode).Distinct()?.ToList();

            //var filteredUsPincodes = usPincodes.Where(g => usStates.Contains(g.StateCode))?.ToList();

            //await PinCodeStateSeed.SeedPincode(context, filteredUsPincodes, us);

            await context.SaveChangesAsync(null, false);


            var randomPinCode = filteredInPincodes.FirstOrDefault();

            await PortalAdminSeed.Seed(context, webHostEnvironment, userManager, roleManager, randomPinCode.Code);

            await DataSeed.SeedDetails(context, webHostEnvironment, clientUserManager, vendorUserManager, customApiCLient, httpAccessor);
        }
    }
}