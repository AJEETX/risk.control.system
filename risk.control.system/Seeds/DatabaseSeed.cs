using Microsoft.AspNetCore.Identity;
using Microsoft.FeatureManagement;
using risk.control.system.Models;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class DatabaseSeed
    {
        public static async Task SeedDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var customApiCLient = scope.ServiceProvider.GetRequiredService<ICustomApiClient>();
            var httpAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
            var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

            context.Database.EnsureCreated();

            //check for users
            if (context.ApplicationUser.Any())
            {
                return; //if user is not empty, DB has been seed
            }

            //CREATE ROLES
            await RoleSeeder.SeedAsync(roleManager);

            PermissionModuleSeed.SeedClaim(context);

            await BsbSeed.LoadBsbData(context);

            await InsurerSetupSeed.Seed(context);

            var randomPinCode = await StartCountryWiseSeed.Seed(context, env, userManager, customApiCLient, fileStorageService);

            await PortalAdminSeed.Seed(context, env, userManager, roleManager, randomPinCode, fileStorageService);

            //Upload face images to S3 bucket and migrate existing users to AWS collection
            var amazonApiService = scope.ServiceProvider.GetRequiredService<IAmazonApiService>();
            var base64FileService = scope.ServiceProvider.GetRequiredService<IBase64FileService>();
            await MigrateImagesToAws.MigrateExistingUsersToCollectionAsync(amazonApiService, context, base64FileService, featureManager);
        }
    }
}