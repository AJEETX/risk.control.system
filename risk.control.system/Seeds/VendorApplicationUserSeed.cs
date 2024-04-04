using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class VendorApplicationUserSeed
    {
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<VendorApplicationUser> userManager, Vendor vendor)
        {
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);

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

            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "agency-admin.jpeg");
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
                ProfilePicture = adminImage
            };
            if (userManager.Users.All(u => u.Id != vendorAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAdmin, Password);
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.AgencyAdmin.ToString());
                    //var vendorAdminRole = new ApplicationRole(AppRoles.AgencyAdmin.ToString(), AppRoles.AgencyAdmin.ToString());
                    //vendorAdmin.ApplicationRoles.Add(vendorAdminRole);

                    //await userManager.AddToRoleAsync(vendorAdmin, AppRoles.Supervisor.ToString());
                    //var vendorSuperVisorRole = new ApplicationRole(AppRoles.Supervisor.ToString(), AppRoles.Supervisor.ToString());
                    //vendorAdmin.ApplicationRoles.Add(vendorSuperVisorRole);

                    //await userManager.AddToRoleAsync(vendorAdmin, AppRoles.Agent.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.Agent.ToString(), AppRoles.Agent.ToString());
                    //vendorAdmin.ApplicationRoles.Add(vendorAgentRole);
                }
            }

            //Seed Vendor Supervisor

            string supervisorEmailwithSuffix = SUPERVISOR.CODE + "@" + vendor.Email;

            var vsMailBox = new Mailbox
            {
                Name = supervisorEmailwithSuffix
            };

            string supervisorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "supervisor.jpeg");
            var supervisorImage = File.ReadAllBytes(supervisorImagePath);

            if (supervisorImage == null)
            {
                supervisorImage = File.ReadAllBytes(noUserImagePath);
            }
            var vendorSupervisor = new VendorApplicationUser()
            {
                Mailbox = vsMailBox,
                UserName = supervisorEmailwithSuffix,
                Email = supervisorEmailwithSuffix,
                FirstName = SUPERVISOR.FIRST_NAME,
                LastName = SUPERVISOR.LAST_NAME,
                EmailConfirmed = true,
                Active = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                Vendor = vendor,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "55 Donvale Road",
                IsVendorAdmin = false,
                CountryId = countryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = SUPERVISOR.PROFILE_IMAGE,
                ProfilePicture = supervisorImage
            };
            if (userManager.Users.All(u => u.Id != vendorSupervisor.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorSupervisor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorSupervisor, Password);
                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.Supervisor.ToString());
                    //var vendorSuperVisorRole = new ApplicationRole(AppRoles.Supervisor.ToString(), AppRoles.Supervisor.ToString());
                    //vendorSupervisor.ApplicationRoles.Add(vendorSuperVisorRole);

                    //await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.Agent.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.Agent.ToString(), AppRoles.Agent.ToString());
                    //vendorSupervisor.ApplicationRoles.Add(vendorAgentRole);
                }
            }

            //Seed Vendor Agent
            string agentEmailwithSuffix = AGENT.CODE + "@" + vendor.Email;
            var faMailBox = new Mailbox
            {
                Name = agentEmailwithSuffix
            };

            string agentImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "agent.jpeg");
            var agentImage = File.ReadAllBytes(agentImagePath);

            if (agentImage == null)
            {
                agentImage = File.ReadAllBytes(noUserImagePath);
            }
            var vendorAgent = new VendorApplicationUser()
            {
                Mailbox = faMailBox,
                UserName = agentEmailwithSuffix,
                Email = agentEmailwithSuffix,
                FirstName = AGENT.FIRST_NAME,
                LastName = AGENT.LAST_NAME,
                EmailConfirmed = true,
                Active = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                Vendor = vendor,
                PhoneNumber = Applicationsettings.USER_MOBILE,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                Addressline = "23 Vincent Avenue",
                CountryId = countryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = AGENT.PROFILE_IMAGE,
                ProfilePicture = agentImage
            };
            if (userManager.Users.All(u => u.Id != vendorAgent.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAgent.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAgent, Password);
                    await userManager.AddToRoleAsync(vendorAgent, AppRoles.Agent.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.Agent.ToString(), AppRoles.Agent.ToString());
                    //vendorAgent.ApplicationRoles.Add(vendorAgentRole);
                }
            }
        }
    }
}