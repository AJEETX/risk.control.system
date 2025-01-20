using System.Diagnostics.Metrics;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class VendorSeed
    {
        public static async Task<List<Vendor>> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, 
                    LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            var enableMailbox = globalSettings?.EnableMailbox ?? false;
            //CREATE VENDOR COMPANY

            var checker = await AgencyCheckerSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            var verify = await AgencyVerifySeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            var investigate = await AgencyInvestigateSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            var proper = await AgencyProperSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);
            var honest = await AgencyHonestSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            var vendors = new List<Vendor> { checker, verify, investigate, proper, honest };
            return vendors;
        }
    }
}