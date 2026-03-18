using Microsoft.AspNetCore.Identity;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class AgencyAdmiSeed
    {
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, Vendor vendor, PinCode pinCode, IFileStorageService fileStorageService)
        {
            var noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            string adminEmailwithSuffix = AGENCY_ADMIN.CODE + "@" + vendor.Email;

            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(AGENCY_ADMIN.PROFILE_IMAGE));
            var adminImage = File.ReadAllBytes(adminImagePath);

            if (adminImage == null)
            {
                adminImage = File.ReadAllBytes(noUserImagePath);
            }
            var extension = Path.GetExtension(adminImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(adminImage, extension, vendor.Email, "user");

            var vendorAdmin = new ApplicationUser()
            {
                UserName = adminEmailwithSuffix,
                Email = adminEmailwithSuffix,
                FirstName = AGENCY_ADMIN.FIRST_NAME,
                LastName = AGENCY_ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Active = true,
                Password = TestingData,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = true,
                Addressline = vendor.Addressline,
                PhoneNumber = string.Equals(pinCode.Country.Code, "au", StringComparison.OrdinalIgnoreCase) ? SAMPLE_MOBILE_AUSTRALIA : SAMPLE_MOBILE_INDIA,
                Vendor = vendor,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.AGENCY_ADMIN,
                Updated = DateTime.UtcNow
            };
            if (userManager.Users.All(u => u.Id != vendorAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAdmin, TestingData);
                    await userManager.AddToRoleAsync(vendorAdmin, AGENCY_ADMIN.DISPLAY_NAME);
                }
            }
        }
    }
}