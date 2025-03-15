using System.Diagnostics.Metrics;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
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


            //CREATE VENDOR COMPANY
            var checker = new SeedInput { COUNTRY = "au", DOMAIN = "checker.com", NAME = "Checker", PHOTO = "/img/checker.png" };
            var verify = new SeedInput { COUNTRY = "au", DOMAIN = "verify.com", NAME = "Verify", PHOTO = "/img/verify.png" };
            var investigate = new SeedInput { COUNTRY = "au", DOMAIN = "investigate.com", NAME = "Investigate", PHOTO = "/img/investigate.png" };
            var proper = new SeedInput { COUNTRY = "in", DOMAIN = "proper.com", NAME = "Proper", PHOTO = "/img/proper.png" };
            var honest = new SeedInput { COUNTRY = "in", DOMAIN = "honest.com", NAME = "Honest", PHOTO = "/img/honest.png" };
            var nicer = new SeedInput { COUNTRY = "us", DOMAIN = "nicer.com", NAME = "Nicer", PHOTO = "/img/nicer.png" };
            var agencies = new List<SeedInput> { checker, verify, investigate, proper, honest, nicer };

            foreach(var agency in agencies)
            {
                var vendor = await AgencyCheckerSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager, agency);
            }
            //var checker = await AgencyCheckerSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager, checkerInput);

            //var verify = await AgencyVerifySeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            //var investigate = await AgencyInvestigateSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            //var proper = await AgencyProperSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);
            
            //var honest = await AgencyHonestSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);
            
            //var nicer = await AgencyNicerSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager);

            var vendors = new List<Vendor> { };

            return vendors;
        }
    }
}