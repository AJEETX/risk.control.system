using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class ClientApplicationUserSeed
    {
        public static async Task<ClientCompanyApplicationUser> Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ClientCompanyApplicationUser> userManager,
            ClientCompany clientCompany)
        {
            var company = context.ClientCompany.FirstOrDefault(c => c.Email == clientCompany.Email);
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            var pinCode = context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(p => p.PinCodeId == clientCompany.PinCodeId);

            //Seed client admin
            await CompanyAdminSeed.Seed(context, webHostEnvironment, userManager, clientCompany, company.Email, pinCode);

            //Seed client creator
            string creatorEmailwithSuffix = CREATOR.CODE + "@" + company.Email;
            string firstName = CREATOR.FIRST_NAME;
            string lastName = CREATOR.LAST_NAME;
            string photo = CREATOR.PROFILE_IMAGE;
            var creator = await CreatorSeed.Seed(context, webHostEnvironment, userManager, clientCompany, pinCode, creatorEmailwithSuffix, photo, firstName, lastName);

            //Seed client assigner
            string managerEmailwithSuffix = MANAGER.CODE + "@" + company.Email;
            firstName = MANAGER.FIRST_NAME;
            lastName = MANAGER.LAST_NAME;
            photo = MANAGER.PROFILE_IMAGE;
            await ManagerSeed.Seed(context, webHostEnvironment, userManager, clientCompany, pinCode, managerEmailwithSuffix, photo, firstName, lastName);

            //Seed client assessor
            string assessorEmailwithSuffix = ASSESSOR.CODE + "@" + company.Email;
            firstName = ASSESSOR.FIRST_NAME;
            lastName = ASSESSOR.LAST_NAME;
            photo = ASSESSOR.PROFILE_IMAGE;
            await AssessorSeed.Seed(context, webHostEnvironment, userManager, clientCompany, pinCode, assessorEmailwithSuffix, photo, firstName, lastName);

            return creator;
        }
    }
}