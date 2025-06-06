using Microsoft.AspNetCore.Identity;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class ClientVendorSeed
    {
        public static async Task<(List<Vendor> vendors, List<ClientCompany> companyIds)> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager, UserManager<VendorApplicationUser> vendorUserManager)
        {
            var globalSetting = new GlobalSettings
            {
            };
            var newGlobalSetting = await context.GlobalSettings.AddAsync(globalSetting);
            await context.SaveChangesAsync(null, false);

            var vendors = await VendorSeed.Seed(context, webHostEnvironment, customApiCLient, vendorUserManager);

            var companies = await CompanyInsurer.Seed(context, vendors, webHostEnvironment, customApiCLient, clientUserManager);

            return (vendors, companies);
        }
    }
}