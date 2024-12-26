using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class VendorApplicationUserSeed
    {
         private static string noUserImagePath = string.Empty;
        public static async Task Seed(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            UserManager<VendorApplicationUser> userManager, 
            Vendor vendor, ICustomApiCLient customApiCLient,
            IHttpContextAccessor httpAccessor)
        {
            noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            string adminEmailwithSuffix = AGENCY_ADMIN.CODE + "@" + vendor.Email;
            //Seed Vendor Admin
            var vaMailBox = new Mailbox
            {
                Name = adminEmailwithSuffix
            };

            var pinCode = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE);
            var district = context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);
            var state = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == pinCode.State.StateId);
            var countryId = context.Country.FirstOrDefault(s => s.CountryId == state.Country.CountryId)?.CountryId ?? default!;

            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(AGENCY_ADMIN.PROFILE_IMAGE));
            var adminImage = File.ReadAllBytes(adminImagePath);

            if (adminImage == null)
            {
                adminImage = File.ReadAllBytes(noUserImagePath);
            }
            var vendorAdmin = new VendorApplicationUser()
            {
                Mailbox = vaMailBox,
                UserName = adminEmailwithSuffix,
                Email = adminEmailwithSuffix,
                FirstName = AGENCY_ADMIN.FIRST_NAME,
                LastName = AGENCY_ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Active = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = true,
                Addressline = "123 Carnegie St",
                PhoneNumber = Applicationsettings.ADMIN_MOBILE,
                Vendor = vendor,
                CountryId = countryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = AGENCY_ADMIN.PROFILE_IMAGE,
                ProfilePicture = adminImage,
                Role = AppRoles.AGENCY_ADMIN,
                UserRole = AgencyRole.AGENCY_ADMIN,
                Updated = DateTime.Now
            };
            if (userManager.Users.All(u => u.Id != vendorAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAdmin, Password);
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.AGENCY_ADMIN.ToString());
                    //var vendorAdminRole = new ApplicationRole(AppRoles.AGENCY_ADMIN.ToString(), AppRoles.AGENCY_ADMIN.ToString());
                    //vendorAdmin.ApplicationRoles.Add(vendorAdminRole);

                    //await userManager.AddToRoleAsync(vendorAdmin, AppRoles.SUPERVISOR.ToString());
                    //var vendorSuperVisorRole = new ApplicationRole(AppRoles.SUPERVISOR.ToString(), AppRoles.SUPERVISOR.ToString());
                    //vendorAdmin.ApplicationRoles.Add(vendorSuperVisorRole);

                    //await userManager.AddToRoleAsync(vendorAdmin, AppRoles.AGENT.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.AGENT.ToString(), AppRoles.AGENT.ToString());
                    //vendorAdmin.ApplicationRoles.Add(vendorAgentRole);
                }
            }

            //Seed Vendor Supervisor
            await SupervisorSeed.Seed(context, SUPERVISOR.CODE, webHostEnvironment, userManager, vendor, pinCode, Applicationsettings.SUPERVISOR.PROFILE_IMAGE,
                Applicationsettings.SUPERVISOR.FIRST_NAME, Applicationsettings.SUPERVISOR.LAST_NAME);


            //Seed Vendor Agent
            //string agentEmailwithSuffix = AGENT.CODE + "@" + vendor.Email;
            //var pinCode1 = CURRENT_PINCODE2;
            //await AgentSeed.Seed(context, agentEmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, pinCode1, Applicationsettings.AGENT.PROFILE_IMAGE,
            //     Applicationsettings.AGENT.FIRST_NAME, Applicationsettings.AGENT.LAST_NAME,"Holland Road");

            string agent2EmailwithSuffix = AGENTX.CODE + "@" + vendor.Email;
            var pinCode2 = CURRENT_PINCODE4;
            await AgentSeed.Seed(context, agent2EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, pinCode2, Applicationsettings.AGENTX.PROFILE_IMAGE,
                                 Applicationsettings.AGENTX.FIRST_NAME, Applicationsettings.AGENTX.LAST_NAME,"44 Waverley Road");

            //string agent3EmailwithSuffix = AGENTY.CODE + "@" + vendor.Email;
            //var pinCode3 = CURRENT_PINCODE5;
            //await SeedAgent.Seed(context, agent3EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, pinCode3, Applicationsettings.AGENTY.PROFILE_IMAGE,
            //Applicationsettings.AGENTY.FIRST_NAME, Applicationsettings.AGENTX.LAST_NAME);

            //string agent4EmailwithSuffix = AGENTZ.CODE + "@" + vendor.Email;
            //var pinCode4 = CURRENT_PINCODE3;
            //await SeedAgent.Seed(context, agent4EmailwithSuffix, webHostEnvironment, customApiCLient, userManager, vendor, pinCode4, Applicationsettings.AGENTZ.PROFILE_IMAGE,
            //Applicationsettings.AGENTZ.FIRST_NAME, Applicationsettings.AGENTZ.LAST_NAME);

        }
    }
}