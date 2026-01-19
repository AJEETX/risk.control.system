using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class ManagerSeed
    {
        public static async Task Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            ClientCompany clientCompany, PinCode pinCode, string managorEmailwithSuffix, string photo, string firstName, string lastName, IFileStorageService fileStorageService)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);

            string managerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));
            var managerImage = File.ReadAllBytes(managerImagePath);

            if (managerImage == null)
            {
                managerImage = File.ReadAllBytes(noUserImagePath);
            }
            var extension = Path.GetExtension(managerImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(managerImage, extension, clientCompany.Email, "user");
            var manager = new ApplicationUser()
            {
                UserName = managorEmailwithSuffix,
                Email = managorEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = TestingData,
                IsSuperAdmin = false,
                IsClientAdmin = true,
                Active = true,
                PhoneNumber = string.Equals(pinCode.Country.Code, "au", StringComparison.OrdinalIgnoreCase) ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                Addressline = clientCompany.Addressline,
                IsVendorAdmin = false,
                ClientCompany = clientCompany,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.MANAGER,
                Updated = DateTime.Now,
            };
            if (userManager.Users.All(u => u.Id != manager.Id))
            {
                var user = await userManager.FindByEmailAsync(manager.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(manager, TestingData);
                    await userManager.AddToRoleAsync(manager, MANAGER.DISPLAY_NAME);
                }
            }
        }
    }
}