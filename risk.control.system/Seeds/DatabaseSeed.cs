using Microsoft.AspNetCore.Identity;

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

            //context.Database.Migrate();

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

            await ClientCompanySetupSeed.Seed(context);

            await StartCountryWiseSeed.Seed(context, webHostEnvironment, userManager, vendorUserManager, clientUserManager, roleManager, customApiCLient, httpAccessor);
        }
    }
}