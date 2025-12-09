using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class AssessorSeed
    {
        public static async Task Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ClientCompanyApplicationUser> userManager,
            ClientCompany clientCompany, PinCode pinCode, string assessorEmailwithSuffix, string photo, string firstName, string lastName, IFileStorageService fileStorageService)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);

            string assessorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));
            var assessorImage = File.ReadAllBytes(assessorImagePath);

            if (assessorImage == null)
            {
                assessorImage = File.ReadAllBytes(noUserImagePath);
            }
            var extension = Path.GetExtension(assessorImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(assessorImage, extension, clientCompany.Email, "user");
            var clientAssessor = new ClientCompanyApplicationUser()
            {
                UserName = assessorEmailwithSuffix,
                Email = assessorEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = TestingData,
                Active = true,
                ClientCompany = clientCompany,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                IsClientManager = true,
                Addressline = clientCompany.Addressline,
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                ProfilePicture = assessorImage,
                Role = AppRoles.ASSESSOR,
                UserRole = CompanyRole.ASSESSOR,
                Updated = DateTime.Now,
            };
            if (userManager.Users.All(u => u.Id != clientAssessor.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssessor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssessor, TestingData);
                    await userManager.AddToRoleAsync(clientAssessor, AppRoles.ASSESSOR.ToString());
                    //var clientAssessorRole = new ApplicationRole(AppRoles.ASSESSOR.ToString(), AppRoles.ASSESSOR.ToString());
                    //clientAssigner.ApplicationRoles.Add(clientAssessorRole);
                }
            }
        }
    }
}