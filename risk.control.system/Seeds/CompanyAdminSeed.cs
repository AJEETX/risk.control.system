using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class CompanyAdminSeed
    {
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, ClientCompany clientCompany, string companyDomain, PinCode pinCode, IFileStorageService fileStorageService)
        {
            string adminEmailwithSuffix = COMPANY_ADMIN.CODE + "@" + companyDomain;
            var adminImage = await File.ReadAllBytesAsync(Path.Combine(env.WebRootPath, "seed", Path.GetFileName(COMPANY_ADMIN.PROFILE_IMAGE)));
            var (fileName, relativePath) = await fileStorageService.SaveAsync(adminImage, Path.GetExtension(Path.Combine(env.WebRootPath, "seed", Path.GetFileName(COMPANY_ADMIN.PROFILE_IMAGE))), companyDomain, "user");
            var admin = new ApplicationUser()
            {
                UserName = adminEmailwithSuffix,
                Email = adminEmailwithSuffix,
                FirstName = COMPANY_ADMIN.FIRST_NAME,
                LastName = COMPANY_ADMIN.LAST_NAME,
                EmailConfirmed = true,
                ClientCompanyId = clientCompany.ClientCompanyId,
                PhoneNumberConfirmed = true,
                Password = TestingData,
                IsCompanyAdmin = true,
                Addressline = clientCompany.Addressline,
                PhoneNumber = string.Equals(pinCode.Country!.Code, "au", StringComparison.OrdinalIgnoreCase) ? SAMPLE_MOBILE_AUSTRALIA : SAMPLE_MOBILE_INDIA,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.COMPANY_ADMIN,
                Updated = DateTime.UtcNow,
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