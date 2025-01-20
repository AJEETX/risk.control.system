using System.Diagnostics.Metrics;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class ClientVendorSeed
    {
        public static async Task<(List<Vendor> vendors, List<ClientCompany> companyIds)> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType,
                    InvestigationServiceType docServiceType, LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager, UserManager<VendorApplicationUser> vendorUserManager)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSetting = new GlobalSettings
            {
                EnableMailbox = true
            };
            var newGlobalSetting = await context.GlobalSettings.AddAsync(globalSetting);
            await context.SaveChangesAsync(null, false);
            var globalSettings = context.GlobalSettings.FirstOrDefault();
            var enableMailbox = globalSettings?.EnableMailbox ?? false;

            var vendors = await VendorSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            var companies = await CompanyInsurer.Seed(context, vendors, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, clientUserManager);

            await context.SaveChangesAsync(null, false);

            //foreach (var vendor in vendors)
            //{
            //    foreach (var insurerCompany in companies)
            //    {
            //        if (vendor.CountryId == insurerCompany.CountryId)
            //        {
            //            vendor.Clients.Add(insurerCompany);
            //        }
            //    }
            //}

            await context.SaveChangesAsync(null, false);
            return (vendors, companies);
        }
    }
}