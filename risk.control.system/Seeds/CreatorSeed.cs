using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class CreatorSeed
    {
        public static async Task<ClientCompanyApplicationUser> Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ClientCompanyApplicationUser> userManager,
            ClientCompany clientCompany, PinCode pinCode, string creatorEmailwithSuffix, string photo, string firstName, string lastName, IFileStorageService fileStorageService)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", NO_USER);

            string creatorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));

            var creatorImage = File.ReadAllBytes(creatorImagePath);

            if (creatorImage == null)
            {
                creatorImage = File.ReadAllBytes(noUserImagePath);
            }
            var extension = Path.GetExtension(creatorImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(creatorImage, extension, clientCompany.Email, "user");
            var clientCreator = new ClientCompanyApplicationUser()
            {
                UserName = creatorEmailwithSuffix,
                Email = creatorEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                Active = true,
                EmailConfirmed = true,
                Password = TestingData,
                ClientCompany = clientCompany,
                PhoneNumberConfirmed = true,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = clientCompany.Addressline,
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                IsVendorAdmin = false,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                ProfilePicture = creatorImage,
                Role = AppRoles.CREATOR,
                UserRole = CompanyRole.CREATOR,
                Updated = DateTime.Now,
            };
            if (userManager.Users.All(u => u.Id != clientCreator.Id))
            {
                var user = await userManager.FindByEmailAsync(clientCreator.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientCreator, TestingData);
                    await userManager.AddToRoleAsync(clientCreator, AppRoles.CREATOR.ToString());
                    //var clientCreatorRole = new ApplicationRole(AppRoles.CREATOR.ToString(), AppRoles.CREATOR.ToString());
                    //clientCreator.ApplicationRoles.Add(clientCreatorRole);
                }
            }
            return clientCreator;
        }
    }
}