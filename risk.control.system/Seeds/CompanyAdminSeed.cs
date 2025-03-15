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
            
            string adminEmailwithSuffix = Applicationsettings.COMPANY_ADMIN.CODE + "@" + companyDomain;
            var asMailBox = new Mailbox
            {
                Name = adminEmailwithSuffix
            };
            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(COMPANY_ADMIN.PROFILE_IMAGE));

            var adminImage = File.ReadAllBytes(adminImagePath);

            if (adminImage == null)
            {
                adminImage = File.ReadAllBytes(noUserImagePath);
            }

            var admin = new ClientCompanyApplicationUser()
            {
                Mailbox = asMailBox,
                UserName = adminEmailwithSuffix,
                Email = adminEmailwithSuffix,
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
                ProfilePictureUrl = COMPANY_ADMIN.PROFILE_IMAGE,
                ProfilePicture = adminImage,
                Role = AppRoles.COMPANY_ADMIN,
                UserRole = CompanyRole.COMPANY_ADMIN,
                Updated = DateTime.Now,
            };
            if (userManager.Users.All(u => u.Id != admin.Id))
            {
                var user = await userManager.FindByEmailAsync(admin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(admin, Password);
                    await userManager.AddToRoleAsync(admin, AppRoles.COMPANY_ADMIN.ToString());
                    //var clientAssignerRole = new ApplicationRole(AppRoles.Assigner.ToString(), AppRoles.Assigner.ToString());
                    //clientAssigner.ApplicationRoles.Add(clientAssignerRole);
                }
            }
        }
    }
}