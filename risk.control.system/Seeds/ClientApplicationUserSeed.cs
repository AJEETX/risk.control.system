using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class ClientApplicationUserSeed
    {
        public static async Task Seed(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            UserManager<ClientCompanyApplicationUser> userManager, 
            ClientCompany clientCompany,
            IHttpContextAccessor httpAccessor)
        {
            var company = context.ClientCompany.FirstOrDefault(c => c.Email == clientCompany.Email);
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            var pinCode = context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(p => p.Code == CURRENT_PINCODE);

            //Seed client admin
            await CompanyAdminSeed.Seed(context, webHostEnvironment, userManager, clientCompany, httpAccessor, company.Email, pinCode);

            //Seed client creator
            await CreatorSeed.Seed(context, webHostEnvironment, userManager, clientCompany, httpAccessor,company.Email, pinCode);

            //Seed client assigner
            await ManagerSeed.Seed(context,webHostEnvironment, userManager, clientCompany, httpAccessor, company.Email, pinCode);

            //Seed client assessor
            await AssessorSeed.Seed(context, webHostEnvironment, userManager, clientCompany, httpAccessor, company.Email,pinCode);
        }
    }
}