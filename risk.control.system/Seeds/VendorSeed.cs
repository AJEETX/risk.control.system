using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class VendorSeed
    {
        static string COUNTRY = CONSTANTS.COUNTRY_AU;

        public static async Task<List<Vendor>> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager)
        {
            var countries = context.Country.ToList();

            //CREATE VENDOR COMPANY
            //var checker = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "checker.com", NAME = "Checker", PHOTO = "/img/checker.png" };
            //var verify = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "verify.com", NAME = "Verify", PHOTO = "/img/verify.png" };
            //var investigate = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "investigate.com", NAME = "Investigate", PHOTO = "/img/investigate.png" };
            //var investigator = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "investigator.com", NAME = "Investigator", PHOTO = "/img/investigation.png" };
            //var greatez = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "greatez.com", NAME = "Greatez", PHOTO = "/img/company.png" };

            //var nicer = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "nicer.com", NAME = "Nicer", PHOTO = "/img/nicer.png" };
            //var demoz = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "demoz.com", NAME = "Demoz", PHOTO = "/img/demo.png" };
            //var investigatos = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "investigatos.com", NAME = "Investigatos", PHOTO = "/img/investigation.png" };
            //var greates = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "greates.com", NAME = "Greates", PHOTO = "/img/company.png" };

            //#if !DEBUG
            COUNTRY = CONSTANTS.COUNTRY_IN;

            //#endif
            var proper = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "proper.com", NAME = "Proper", PHOTO = "/img/proper.png" };
            var honest = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "honest.com", NAME = "Honest", PHOTO = "/img/honest.png" };
            var greater = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "greater.com", NAME = "Greater", PHOTO = "/img/company.png" };
            var investigatoz = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "investigatoz.com", NAME = "Investigatoz", PHOTO = "/img/investigation.png" };

            var servicesTypes = await ServiceTypeSeed.Seed(context);

            var agencies = new List<SeedInput> {
                //checker, 
//#if !DEBUG

                //verify, investigate, investigator, greatez,
                proper, honest,
                //greater, investigatoz
                //, nicer, demoz, investigatos, greates
//#endif
            };
            var vendors = new List<Vendor> { };

            foreach (var agency in agencies)
            {
                var vendor = await AgencyCheckerSeed.Seed(context, webHostEnvironment, customApiCLient, vendorUserManager, agency, servicesTypes);
                vendors.Add(vendor);
            }
            return vendors;
        }
    }
}