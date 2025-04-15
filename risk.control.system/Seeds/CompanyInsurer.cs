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
        public static async Task< List<ClientCompany>> Seed(ApplicationDbContext context, List<Vendor> vendors, IWebHostEnvironment webHostEnvironment,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager)
        {
            var allianz = new SeedInput { COUNTRY = "au", DOMAIN = "allianz.com", NAME = "Allianz", PHOTO = "/img/allianz.png" };
            var insurer = new SeedInput { COUNTRY = "au", DOMAIN = "insurer.com", NAME = "Insurer", PHOTO = "/img/insurer.jpg" };
            var canara = new SeedInput { COUNTRY = "in", DOMAIN = "canara.com", NAME = "Allianz", PHOTO = "/img/chl.jpg" };
            
            var companies = new List<SeedInput> {
                //allianz
                //,
                insurer
                ,
                canara
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