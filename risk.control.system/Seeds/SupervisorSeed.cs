using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class SupervisorSeed
    {
        public static async Task Seed(ApplicationDbContext context, string emailSuffix,
            IWebHostEnvironment webHostEnvironment,
            UserManager<VendorApplicationUser> userManager,
            Vendor vendor, PinCode pinCode, string photo, string firstName, string lastName)
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
            var vendorSupervisor = new VendorApplicationUser()
            {
                UserName = supervisorEmailwithSuffix,
                Email = supervisorEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                Active = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                PhoneNumber = pinCode.Country.Code.ToLowerInvariant() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                Vendor = vendor,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "55 Donvale Road",
                IsVendorAdmin = false,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = photo,
                ProfilePicture = supervisorImage,
                Role = AppRoles.SUPERVISOR,
                UserRole = AgencyRole.SUPERVISOR,
                Updated = DateTime.Now
            };
            if (userManager.Users.All(u => u.Id != vendorSupervisor.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorSupervisor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorSupervisor, Password);
                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.SUPERVISOR.ToString());
                    //var vendorSuperVisorRole = new ApplicationRole(AppRoles.SUPERVISOR.ToString(), AppRoles.Supervisor.ToString());
                    //vendorSupervisor.ApplicationRoles.Add(vendorSuperVisorRole);

                    //await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.AGENT.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.AGENT.ToString(), AppRoles.AGENT.ToString());
                    //vendorSupervisor.ApplicationRoles.Add(vendorAgentRole);
                }
            }
        }
    }
}
