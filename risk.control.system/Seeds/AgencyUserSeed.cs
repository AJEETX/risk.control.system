using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class AgencyUserSeed
    {
        private static string noUserImagePath = string.Empty;

        public static async Task Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            Vendor vendor, ICustomApiClient customApiCLient, IFileStorageService fileStorageService)
        {
            noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            string adminEmailwithSuffix = AGENCY_ADMIN.CODE + "@" + vendor.Email;

            var pinCode = await context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == vendor.PinCodeId);

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
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                Vendor = vendor,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.AGENCY_ADMIN,
                Updated = DateTime.Now
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

            //Seed Vendor Supervisor
            await SupervisorSeed.Seed(context, SUPERVISOR.CODE, webHostEnvironment, userManager, vendor, pinCode, SUPERVISOR.PROFILE_IMAGE, SUPERVISOR.FIRST_NAME, SUPERVISOR.LAST_NAME, fileStorageService);

            //Seed Vendor Agent
            string agentEmailwithSuffix = AGENT.CODE + "@" + vendor.Email;
            await AgentSeed.Seed(context, agentEmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, vendor.PinCode.Code, AGENT.PROFILE_IMAGE, AGENT.FIRST_NAME, AGENT.LAST_NAME,
                fileStorageService);

            //if (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    string agent2EmailwithSuffix = AGENTX.CODE + "@" + vendor.Email;
            //    await AgentSeed.Seed(context, agent2EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, vendor.PinCode.Code, AGENTX.PROFILE_IMAGE, AGENTX.FIRST_NAME, AGENTX.LAST_NAME, "44 Waverley Road");

            //    string agent3EmailwithSuffix = AGENTY.CODE + "@" + vendor.Email;
            //    await AgentSeed.Seed(context, agent3EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, vendor.PinCode.Code, AGENTY.PROFILE_IMAGE, AGENTY.FIRST_NAME, AGENTX.LAST_NAME, "44 Waverley Road");

            //    string agent4EmailwithSuffix = AGENTZ.CODE + "@" + vendor.Email;
            //    await AgentSeed.Seed(context, agent4EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, vendor.PinCode.Code, AGENTZ.PROFILE_IMAGE, AGENTZ.FIRST_NAME, AGENTZ.LAST_NAME, "44 Waverley Road");
            //}
        }
    }
}