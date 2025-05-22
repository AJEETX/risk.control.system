using Microsoft.AspNetCore.Identity;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class DataSeed
    {
        public static async Task SeedDetails(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ClientCompanyApplicationUser> clientUserManager,
            UserManager<VendorApplicationUser> vendorUserManager,
            ICustomApiCLient customApiCLient,
            IHttpContextAccessor httpAccessor)
        {

            #region BENEFICIARY-RELATION

            await ClientCompanySetupSeed.Seed(context);

            #endregion BENEFICIARY-RELATION

            #region CLIENT/ VENDOR COMPANY

            var (vendors, companyIds) = await ClientVendorSeed.Seed(context, webHostEnvironment, customApiCLient, clientUserManager, vendorUserManager);

            #endregion CLIENT/ VENDOR COMPANY

            #region PERMISSIONS ROLES

            //PermissionModuleSeed.SeedMailbox(context);

            //PermissionModuleSeed.SeedClaim(context);

            #endregion PERMISSIONS ROLES

        }
    }
}
