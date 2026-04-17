using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class CreatorSeed
    {
        public static async Task<ApplicationUser> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager,
            ClientCompany clientCompany, PinCode pinCode, string creatorEmailwithSuffix, string photo, string firstName, string lastName, IFileStorageService fileStorageService)
        {
            string creatorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "seed", Path.GetFileName(photo));
            var creatorImage = await File.ReadAllBytesAsync(creatorImagePath);
            var extension = Path.GetExtension(creatorImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(creatorImage, extension, clientCompany.Email, "user");
            var clientCreator = new ApplicationUser()
            {
                UserName = creatorEmailwithSuffix,
                Email = creatorEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                Active = true,
                EmailConfirmed = true,
                Password = TestingData,
                ClientCompanyId = clientCompany.ClientCompanyId,
                PhoneNumberConfirmed = true,
                Addressline = clientCompany.Addressline,
                PhoneNumber = string.Equals(pinCode.Country!.Code, "au", StringComparison.OrdinalIgnoreCase) ? SAMPLE_MOBILE_AUSTRALIA : SAMPLE_MOBILE_INDIA,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.CREATOR,
                Updated = DateTime.UtcNow,
            };
            if (userManager.Users.All(u => u.Id != clientCreator.Id))
            {
                var user = await userManager.FindByEmailAsync(clientCreator.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientCreator, TestingData);
                    await userManager.AddToRoleAsync(clientCreator, CREATOR.DISPLAY_NAME);
                }
            }
            return clientCreator;
        }
    }
}