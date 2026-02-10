using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class SupervisorSeed
    {
        public static async Task Seed(ApplicationDbContext context, string emailSuffix,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            Vendor vendor, PinCode pinCode, string photo, string firstName, string lastName, IFileStorageService fileStorageService)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);

            string supervisorEmailwithSuffix = emailSuffix + "@" + vendor.Email;

            string supervisorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));
            var supervisorImage = File.ReadAllBytes(supervisorImagePath);

            if (supervisorImage == null)
            {
                supervisorImage = File.ReadAllBytes(noUserImagePath);
            }
            var extension = Path.GetExtension(supervisorImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(supervisorImage, extension, vendor.Email, "user");
            var vendorSupervisor = new ApplicationUser()
            {
                UserName = supervisorEmailwithSuffix,
                Email = supervisorEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                Active = true,
                PhoneNumberConfirmed = true,
                Password = TestingData,
                PhoneNumber = string.Equals(pinCode.Country.Code, "au", StringComparison.OrdinalIgnoreCase) ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                Vendor = vendor,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = vendor.Addressline,
                IsVendorAdmin = false,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.SUPERVISOR,
                Updated = DateTime.UtcNow
            };
            if (userManager.Users.All(u => u.Id != vendorSupervisor.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorSupervisor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorSupervisor, TestingData);
                    await userManager.AddToRoleAsync(vendorSupervisor, SUPERVISOR.DISPLAY_NAME);
                }
            }
        }
    }
}
