using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class CompanyAdminSeed
    {
        public static async Task Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            ClientCompany clientCompany, string companyDomain, PinCode pinCode, IFileStorageService fileStorageService)
        {
            //Seed client creator
            string adminEmailwithSuffix = COMPANY_ADMIN.CODE + "@" + companyDomain;

            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(COMPANY_ADMIN.PROFILE_IMAGE));

            var adminImage = File.ReadAllBytes(adminImagePath);
            var extension = Path.GetExtension(adminImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(adminImage, extension, companyDomain, "user");
            var admin = new ApplicationUser()
            {
                UserName = adminEmailwithSuffix,
                Email = adminEmailwithSuffix,
                FirstName = COMPANY_ADMIN.FIRST_NAME,
                LastName = COMPANY_ADMIN.LAST_NAME,
                Active = true,
                EmailConfirmed = true,
                ClientCompany = clientCompany,
                PhoneNumberConfirmed = true,
                Password = TestingData,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = clientCompany.Addressline,
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                IsVendorAdmin = false,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.COMPANY_ADMIN,
                Updated = DateTime.Now,
            };
            if (userManager.Users.All(u => u.Id != admin.Id))
            {
                var user = await userManager.FindByEmailAsync(admin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(admin, TestingData);
                    await userManager.AddToRoleAsync(admin, COMPANY_ADMIN.DISPLAY_NAME);
                }
            }
        }
    }
}