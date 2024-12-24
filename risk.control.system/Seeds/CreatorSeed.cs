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
    public static class CreatorSeed
    {
        public static async Task Seed(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            UserManager<ClientCompanyApplicationUser> userManager, 
            ClientCompany clientCompany,
            IHttpContextAccessor httpAccessor, string companyDomain, PinCode pinCode)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            
            string creatorEmailwithSuffix = Applicationsettings.CREATOR.CODE + "@" + companyDomain;
            var ccMailBox = new Mailbox
            {
                Name = creatorEmailwithSuffix
            };

            string creatorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(CREATOR.PROFILE_IMAGE));

            var creatorImage = File.ReadAllBytes(creatorImagePath);

            if (creatorImage == null)
            {
                creatorImage = File.ReadAllBytes(noUserImagePath);
            }
            var clientCreator = new ClientCompanyApplicationUser()
            {
                Mailbox = ccMailBox,
                UserName = creatorEmailwithSuffix,
                Email = creatorEmailwithSuffix,
                FirstName = CREATOR.FIRST_NAME,
                LastName = CREATOR.LAST_NAME,
                Active = true,
                EmailConfirmed = true,
                Password = Password,
                ClientCompany = clientCompany,
                PhoneNumberConfirmed = true,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "987 Canterbury Road",
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                IsVendorAdmin = false,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = CREATOR.PROFILE_IMAGE,
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
                    await userManager.CreateAsync(clientCreator, Password);
                    await userManager.AddToRoleAsync(clientCreator, AppRoles.CREATOR.ToString());
                    //var clientCreatorRole = new ApplicationRole(AppRoles.CREATOR.ToString(), AppRoles.CREATOR.ToString());
                    //clientCreator.ApplicationRoles.Add(clientCreatorRole);
                }
            }
        }
    }
}