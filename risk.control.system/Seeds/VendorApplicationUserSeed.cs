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
        public static async Task Seed(ApplicationDbContext context, EntityEntry<Country> indiaCountry, UserManager<VendorApplicationUser> userManager, Vendor vendor)
        {
            string adminEmailwithSuffix = AGENCY_ADMIN.USERNAME + "@" + vendor.Email;
            //Seed Vendor Admin
            var vaMailBox = new Mailbox
            {
                Name = adminEmailwithSuffix
            };

            var pinCode = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE);
            var district = context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);
            var state = context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);

            var vendorAdmin = new VendorApplicationUser()
            {
                Mailbox = vaMailBox,
                UserName = adminEmailwithSuffix,
                Email = adminEmailwithSuffix,
                FirstName = AGENCY_ADMIN.FIRST_NAME,
                LastName = AGENCY_ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = true,
                Addressline = "123 Benaras Gali",
                PhoneNumber = "9876543210",
                VendorId = vendor.VendorId,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = AGENCY_ADMIN.PROFILE_IMAGE
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

            string supervisorEmailwithSuffix = SUPERVISOR.USERNAME + "@" + vendor.Email;

            var vsMailBox = new Mailbox
            {
                Name = supervisorEmailwithSuffix
            };
            var investigatePinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4);
            var investigateDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == investigatePinCode.District.DistrictId);
            var investigateState = context.State.FirstOrDefault(s => s.StateId == investigateDistrict.State.StateId);

            var vendorSupervisor = new VendorApplicationUser()
            {
                Mailbox = vsMailBox,
                UserName = supervisorEmailwithSuffix,
                Email = supervisorEmailwithSuffix,
                FirstName = SUPERVISOR.FIRST_NAME,
                LastName = SUPERVISOR.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                PhoneNumber = "9876543211",
                VendorId = vendor.VendorId,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "123 Pakki Gali",
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = investigateDistrict?.DistrictId ?? default!,
                StateId = investigateState?.StateId ?? default!,
                PinCodeId = investigatePinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = SUPERVISOR.PROFILE_IMAGE
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
            string agentEmailwithSuffix = AGENT.USERNAME + "@" + vendor.Email;
            var faMailBox = new Mailbox
            {
                Name = agentEmailwithSuffix
            };

            var checkerPinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
            var checkerDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == checkerPinCode.District.DistrictId);
            var checkerState = context.State.FirstOrDefault(s => s.StateId == checkerDistrict.State.StateId);
            var vendorAgent = new VendorApplicationUser()
            {
                Mailbox = faMailBox,
                UserName = agentEmailwithSuffix,
                Email = agentEmailwithSuffix,
                FirstName = AGENT.FIRST_NAME,
                LastName = AGENT.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                VendorId = vendor.VendorId,
                PhoneNumber = "9876003210",
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                Addressline = "99 Mandir ke paas",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = checkerDistrict?.DistrictId ?? default!,
                StateId = checkerState?.StateId ?? default!,
                PinCodeId = checkerPinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = AGENT.PROFILE_IMAGE
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