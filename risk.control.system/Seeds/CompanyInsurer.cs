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
    public class CompanyInsurer
    {
        static string COUNTRY = CONSTANTS.COUNTRY_AU;

        public static async Task< List<ClientCompany>> Seed(ApplicationDbContext context, List<Vendor> vendors, IWebHostEnvironment webHostEnvironment,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager)
        {
            var allianz = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "allianz.com", NAME = "Allianz", PHOTO = "/img/allianz.png" };
            var insurer = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "insurer.com", NAME = "Insurer", PHOTO = "/img/insurer.jpg" };
#if !DEBUG
            COUNTRY = CONSTANTS.COUNTRY_IN;
#endif
            var canara = new SeedInput { COUNTRY = COUNTRY, DOMAIN = "canara.com", NAME = "Allianz", PHOTO = "/img/chl.jpg" };
            
            var companies = new List<SeedInput> {
                //allianz
                //,
                insurer
                ,
#if !DEBUG
                canara
#endif

            };

            foreach (var company in companies)
            {
                _ = await InsurerAllianz.Seed(context, vendors, webHostEnvironment, customApiCLient, clientUserManager, company);
            }

            await context.SaveChangesAsync(null, false);
            return null;
        }
    }
}