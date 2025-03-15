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

            var countries = context.Country.ToList();

            //CREATE VENDOR COMPANY
            var checker = new SeedInput { COUNTRY = "au", DOMAIN = "checker.com", NAME = "Checker", PHOTO = "/img/checker.png" };
            var verify = new SeedInput { COUNTRY = "au", DOMAIN = "verify.com", NAME = "Verify", PHOTO = "/img/verify.png" };
            var investigate = new SeedInput { COUNTRY = "au", DOMAIN = "investigate.com", NAME = "Investigate", PHOTO = "/img/investigate.png" };
            var investigator = new SeedInput { COUNTRY = "au", DOMAIN = "investigator.com", NAME = "Investigator", PHOTO = "/img/investigation.png" };
            var greatez = new SeedInput { COUNTRY = "au", DOMAIN = "greatez.com", NAME = "Greatez", PHOTO = "/img/company.png" };

            var proper = new SeedInput { COUNTRY = "in", DOMAIN = "proper.com", NAME = "Proper", PHOTO = "/img/proper.png" };
            var honest = new SeedInput { COUNTRY = "in", DOMAIN = "honest.com", NAME = "Honest", PHOTO = "/img/honest.png" };
            var greater = new SeedInput { COUNTRY = "in", DOMAIN = "greater.com", NAME = "Greater", PHOTO = "/img/company.png" };
            var investigatoz = new SeedInput { COUNTRY = "in", DOMAIN = "investigatoz.com", NAME = "Investigatoz", PHOTO = "/img/investigation.png" };
            
            var nicer = new SeedInput { COUNTRY = "us", DOMAIN = "nicer.com", NAME = "Nicer", PHOTO = "/img/nicer.png" };
            var demoz = new SeedInput { COUNTRY = "us", DOMAIN = "demoz.com", NAME = "Demoz", PHOTO = "/img/demo.png" };
            var investigatos = new SeedInput { COUNTRY = "us", DOMAIN = "investigatos.com", NAME = "Investigatos", PHOTO = "/img/investigation.png" };
            var greates = new SeedInput { COUNTRY = "us", DOMAIN = "greates.com", NAME = "Greates", PHOTO = "/img/company.png" };

            var agencies = new List<SeedInput> { checker, verify, investigate, investigator, greatez, proper, honest, greater, investigatoz, nicer, demoz, investigatos, greates };
            var vendors = new List<Vendor> { };

            foreach (var agency in agencies)
            {
                var vendor = await AgencyCheckerSeed.Seed(context, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, vendorUserManager, agency);
                vendors.Add(vendor);
            }
            return vendors;
        }
    }
}