﻿using Google.Api;

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
    public static class VendorApplicationUserSeed
    {
         private static string noUserImagePath = string.Empty;
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<VendorApplicationUser> userManager, Vendor vendor)
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

            string supervisorEmailwithSuffix = SUPERVISOR.CODE + "@" + vendor.Email;

            var vsMailBox = new Mailbox
            {
                Name = supervisorEmailwithSuffix
            };

            string supervisorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(SUPERVISOR.PROFILE_IMAGE));
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

            //Seed Vendor Agent
            string agentEmailwithSuffix = AGENT.CODE + "@" + vendor.Email;
            var pinCode1 = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE2);
            var createAgent = SeedAgent(context,agentEmailwithSuffix, webHostEnvironment, userManager, vendor, pinCode1, Applicationsettings.AGENT.PROFILE_IMAGE);


            //string agent2EmailwithSuffix = AGENTZ.CODE + "@" + vendor.Email;
            //var pinCode2 = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE3);
            //var createAgent2 = SeedAgent(context, agent2EmailwithSuffix, webHostEnvironment, userManager, vendor, pinCode2, Applicationsettings.AGENTZ.PROFILE_IMAGE);


            //string agent3EmailwithSuffix = AGENTX.CODE + "@" + vendor.Email;
            //var pinCode3 = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE4);
            //var createAgent3 = SeedAgent(context, agent3EmailwithSuffix, webHostEnvironment, userManager, vendor, pinCode3, Applicationsettings.AGENTX.PROFILE_IMAGE);


            //string agent4EmailwithSuffix = AGENTY.CODE + "@" + vendor.Email;
            //var pinCode4 = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE5);
            //var createAgent4 = SeedAgent(context, agent4EmailwithSuffix, webHostEnvironment, userManager, vendor, pinCode4, Applicationsettings.AGENTY.PROFILE_IMAGE);
        }

        private static async Task SeedAgent(ApplicationDbContext context, string agentEmailwithSuffix, 
            IWebHostEnvironment webHostEnvironment,
            UserManager<VendorApplicationUser> userManager, 
            Vendor vendor,PinCode pinCode, string photo)
        {
            var district = context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);
            var state = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == pinCode.State.StateId);
            var countryId = context.Country.FirstOrDefault(s => s.CountryId == state.Country.CountryId)?.CountryId ?? default!;
            var faMailBox = new Mailbox
            {
                Name = agentEmailwithSuffix
            };
            var noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            string agentImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));
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
                ProfilePictureUrl = photo,
                ProfilePicture = agentImage,
                Role = AppRoles.AGENT,
                UserRole = AgencyRole.AGENT,
                Updated = DateTime.Now
            };
            if (userManager.Users.All(u => u.Id != vendorAgent.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAgent.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAgent, Password);
                    await userManager.AddToRoleAsync(vendorAgent, AppRoles.AGENT.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.AGENT.ToString(), AppRoles.AGENT.ToString());
                    //vendorAgent.ApplicationRoles.Add(vendorAgentRole);
                }
            }
        }
    }
}