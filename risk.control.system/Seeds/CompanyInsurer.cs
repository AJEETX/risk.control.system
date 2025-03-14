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
    public class CompanyInsurer
    {
        public static async Task< List<ClientCompany>> Seed(ApplicationDbContext context, List<Vendor> vendors, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, 
                    InvestigationServiceType docServiceType, LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager)
        {
            var allianz = await InsurerAllianz.Seed(context, vendors, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, clientUserManager);
            
            var insurer = await InsurerSeed.Seed(context, vendors, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, clientUserManager);

            var insurerCanara = await InsurerCanara.Seed(context, vendors, webHostEnvironment, investigationServiceType, discreetServiceType, docServiceType, lineOfBusiness, httpAccessor, customApiCLient, clientUserManager);
            var companies = new List<ClientCompany> { 
                insurer
                //, insurerCanara 
            };

            await context.SaveChangesAsync(null, false);
            return companies;
        }
    }
}