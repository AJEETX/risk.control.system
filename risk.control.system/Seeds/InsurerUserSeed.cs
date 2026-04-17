using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class InsurerUserSeed
    {
        public static async Task<ApplicationUser> Seed(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager,
            ClientCompany clientCompany, IFileStorageService fileStorageService)
        {
            var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.Email == clientCompany.Email);
            var pinCode = await context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == clientCompany.PinCodeId);
            await CompanyAdminSeed.Seed(context, env, userManager, clientCompany, company!.Email, pinCode!, fileStorageService);
            string creatorEmailwithSuffix = CREATOR.CODE + "@" + company.Email;
            string firstName = CREATOR.FIRST_NAME;
            string lastName = CREATOR.LAST_NAME;
            string photo = CREATOR.PROFILE_IMAGE;
            var creator = await CreatorSeed.Seed(context, env, userManager, clientCompany, pinCode!, creatorEmailwithSuffix, photo, firstName, lastName, fileStorageService);
            string managerEmailwithSuffix = MANAGER.CODE + "@" + company.Email;
            firstName = MANAGER.FIRST_NAME;
            lastName = MANAGER.LAST_NAME;
            photo = MANAGER.PROFILE_IMAGE;
            await ManagerSeed.Seed(context, env, userManager, clientCompany, pinCode!, managerEmailwithSuffix, photo, firstName, lastName, fileStorageService);
            string assessorEmailwithSuffix = ASSESSOR.CODE + "@" + company.Email;
            firstName = ASSESSOR.FIRST_NAME;
            lastName = ASSESSOR.LAST_NAME;
            photo = ASSESSOR.PROFILE_IMAGE;
            await AssessorSeed.Seed(context, env, userManager, clientCompany, pinCode!, assessorEmailwithSuffix, photo, firstName, lastName, fileStorageService);
            return creator;
        }
    }
}