using Microsoft.AspNetCore.Identity;

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
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var customApiCLient = scope.ServiceProvider.GetRequiredService<ICustomApiClient>();
            var httpAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            context.Database.EnsureCreated();

            //context.Database.Migrate();

            //check for users
            if (context.ApplicationUser.Any())
            {
                return; //if user is not empty, DB has been seed
            }

            //CREATE ROLES
            await RoleSeeder.SeedAsync(roleManager);

            //PermissionModuleSeed.SeedClaim(context);

            await BsbSeed.LoadBsbData(context);

            await ClientCompanySetupSeed.Seed(context);

            await StartCountryWiseSeed.Seed(context, webHostEnvironment, userManager, customApiCLient, fileStorageService);
        }
    }
}