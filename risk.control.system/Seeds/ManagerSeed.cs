using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class ManagerSeed
    {
        public static async Task Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ClientCompanyApplicationUser> userManager,
            ClientCompany clientCompany, PinCode pinCode, string managorEmailwithSuffix, string photo, string firstName, string lastName)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);

            string managerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));
            var managerImage = File.ReadAllBytes(managerImagePath);

            if (managerImage == null)
            {
                managerImage = File.ReadAllBytes(noUserImagePath);
            }
            var manager = new ClientCompanyApplicationUser()
            {
                UserName = managorEmailwithSuffix,
                Email = managorEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = true,
                Active = true,
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                Addressline = "139 Sector 44",
                IsVendorAdmin = false,
                ClientCompany = clientCompany,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = photo,
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
                    //var clientAdminRole = new ApplicationRole(AppRoles.COMPANY_ADMIN.ToString(), AppRoles.COMPANY_ADMIN.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAdminRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.CREATOR.ToString());
                    //var clientCreatorRole = new ApplicationRole(AppRoles.CREATOR.ToString(), AppRoles.CREATOR.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientCreatorRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.Assigner.ToString());
                    //var clientAssignerRole = new ApplicationRole(AppRoles.Assigner.ToString(), AppRoles.Assigner.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAssignerRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.ASSESSOR.ToString());
                    //var clientAssessorRole = new ApplicationRole(AppRoles.ASSESSOR.ToString(), AppRoles.ASSESSOR.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAssessorRole);
                }
            }
        }
    }
}