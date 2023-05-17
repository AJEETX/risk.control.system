using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class VendorApplicationUserSeed
    {
        public static async Task Seed(ApplicationDbContext context, EntityEntry<Country> indiaCountry, UserManager<VendorApplicationUser> userManager, string vendorId)
        {
            //Seed Vendor Admin
            var vaMailBox = new Mailbox
            {
                Name = VENDOR_ADMIN.EMAIL
            };
            var vendorAdmin = new VendorApplicationUser()
            {
                Mailbox = vaMailBox,
                UserName = VENDOR_ADMIN.USERNAME,
                Email = VENDOR_ADMIN.EMAIL,
                FirstName = VENDOR_ADMIN.FIRST_NAME,
                LastName = VENDOR_ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                isSuperAdmin = false,
                VendorId = vendorId,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = VENDOR_ADMIN.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != vendorAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAdmin, Password);
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorAdmin.ToString());
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorSupervisor.ToString());
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorAgent.ToString());
                }
            }

            //Seed Vendor Supervisor
            var vsMailBox = new Mailbox
            {
                Name = VENDOR_SUPERVISOR.EMAIL
            };
            var vendorSupervisor = new VendorApplicationUser()
            {
                Mailbox = vsMailBox,
                UserName = VENDOR_SUPERVISOR.USERNAME,
                Email = VENDOR_SUPERVISOR.EMAIL,
                FirstName = VENDOR_SUPERVISOR.FIRST_NAME,
                LastName = VENDOR_SUPERVISOR.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                VendorId = vendorId,
                isSuperAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = VENDOR_SUPERVISOR.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != vendorSupervisor.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorSupervisor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorSupervisor, Password);
                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.VendorSupervisor.ToString());
                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.VendorAgent.ToString());
                }
            }

            //Seed Vendor Agent
            var faMailBox = new Mailbox
            {
                Name = VENDOR_AGENT.EMAIL
            };
            var vendorAgent = new VendorApplicationUser()
            {
                Mailbox = faMailBox,
                UserName = VENDOR_AGENT.USERNAME,
                Email = VENDOR_AGENT.EMAIL,
                FirstName = VENDOR_AGENT.FIRST_NAME,
                LastName = VENDOR_AGENT.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                VendorId = vendorId,
                isSuperAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = VENDOR_AGENT.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != vendorAgent.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAgent.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAgent, Password);
                    await userManager.AddToRoleAsync(vendorAgent, AppRoles.VendorAgent.ToString());
                }
            }
        }
    }
}
