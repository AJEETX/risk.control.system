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
    public static class CompanyAdminSeed
    {
        public static async Task Seed(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            UserManager<ClientCompanyApplicationUser> userManager, 
            ClientCompany clientCompany,
            IHttpContextAccessor httpAccessor, string companyDomain, PinCode pinCode)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            
            string managererEmailwithSuffix = Applicationsettings.MANAGER.CODE + "@" + companyDomain;
            var asMailBox = new Mailbox
            {
                Name = managererEmailwithSuffix
            };
            string managerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(MANAGER.PROFILE_IMAGE));

            var managerImage = File.ReadAllBytes(managerImagePath);

            if (managerImage == null)
            {
                managerImage = File.ReadAllBytes(noUserImagePath);
            }

            var manager = new ClientCompanyApplicationUser()
            {
                Mailbox = asMailBox,
                UserName = managererEmailwithSuffix,
                Email = managererEmailwithSuffix,
                FirstName = MANAGER.FIRST_NAME,
                LastName = MANAGER.LAST_NAME,
                Active = true,
                EmailConfirmed = true,
                ClientCompany = clientCompany,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "453 Main Road",
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                IsVendorAdmin = false,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = MANAGER.PROFILE_IMAGE,
                ProfilePicture = managerImage,
                Role = AppRoles.MANAGER,
                UserRole = CompanyRole.MANAGER,
                Updated = DateTime.Now,
            };
            if (userManager.Users.All(u => u.Id != manager.Id))
            {
                var user = await userManager.FindByEmailAsync(manager.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(manager, Password);
                    await userManager.AddToRoleAsync(manager, AppRoles.MANAGER.ToString());
                    //var clientAssignerRole = new ApplicationRole(AppRoles.Assigner.ToString(), AppRoles.Assigner.ToString());
                    //clientAssigner.ApplicationRoles.Add(clientAssignerRole);
                }
            }
        }
    }
}