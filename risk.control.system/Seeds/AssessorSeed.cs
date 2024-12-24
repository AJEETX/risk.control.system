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
    public static class AssessorSeed
    {
        public static async Task Seed(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            UserManager<ClientCompanyApplicationUser> userManager, 
            ClientCompany clientCompany,
            IHttpContextAccessor httpAccessor, string companyDomain, PinCode pinCode)
        {
            //Seed client creator
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);

            string assessorEmailwithSuffix = Applicationsettings.ASSESSOR.CODE + "@" + companyDomain;
            var ssMailBox = new Mailbox
            {
                Name = assessorEmailwithSuffix
            };

            string assessorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(ASSESSOR.PROFILE_IMAGE));
            var assessorImage = File.ReadAllBytes(assessorImagePath);

            if (assessorImage == null)
            {
                assessorImage = File.ReadAllBytes(noUserImagePath);
            }

            var clientAssessor = new ClientCompanyApplicationUser()
            {
                Mailbox = ssMailBox,
                UserName = assessorEmailwithSuffix,
                Email = assessorEmailwithSuffix,
                FirstName = ASSESSOR.FIRST_NAME,
                LastName = ASSESSOR.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                Active = true,
                ClientCompany = clientCompany,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                IsClientManager = true,
                Addressline = "11 Nurlendi Street",
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = ASSESSOR.PROFILE_IMAGE,
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
                    await userManager.CreateAsync(clientAssessor, Password);
                    await userManager.AddToRoleAsync(clientAssessor, AppRoles.ASSESSOR.ToString());
                    //var clientAssessorRole = new ApplicationRole(AppRoles.ASSESSOR.ToString(), AppRoles.ASSESSOR.ToString());
                    //clientAssigner.ApplicationRoles.Add(clientAssessorRole);
                }
            }
        }
    }
}