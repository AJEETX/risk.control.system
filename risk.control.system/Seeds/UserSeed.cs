using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Seeds
{
    public static class UserSeed
    {
        public static async Task Seed(ApplicationDbContext context, EntityEntry<Country> indiaCountry, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            //Seed portal admin
            var portalAdmin = new ApplicationUser()
            {
                UserName = Applicationsettings.PORTAL_ADMIN.USERNAME,
                Email = Applicationsettings.PORTAL_ADMIN.EMAIL,
                FirstName = Applicationsettings.PORTAL_ADMIN.FIRST_NAME,
                LastName = Applicationsettings.PORTAL_ADMIN.LAST_NAME,
                Password = Applicationsettings.Password,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl =Applicationsettings.PORTAL_ADMIN.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != portalAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(portalAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(portalAdmin, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.PortalAdmin.ToString());
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAdmin.ToString());
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientCreator.ToString());
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAssigner.ToString());
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAssessor.ToString());
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorAdmin.ToString());
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorSupervisor.ToString());
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorAgent.ToString());
                }

                ////////PERMISSIONS TO MODULES

                var adminRole = await roleManager.FindByNameAsync(AppRoles.PortalAdmin.ToString()) ?? default!;
                var allClaims = await roleManager.GetClaimsAsync(adminRole);

                //ADD PERMISSIONS

                var moduleList = new List<string> { nameof(Underwriting), nameof(Claim) };

                foreach (var module in moduleList)
                {
                    var modulePermissions = Permissions.GeneratePermissionsForModule(module);

                    foreach (var modulePermission in modulePermissions)
                    {
                        if (!allClaims.Any(a => a.Type == Applicationsettings.PERMISSION && a.Value == modulePermission))
                        {
                            await roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(Applicationsettings.PERMISSION, modulePermission));
                        }
                    }
                }
            }

            //Seed client admin
            var clientAdmin = new ApplicationUser()
            {
                UserName = "client-admin@admin.com",
                Email = "client-admin@admin.com",
                FirstName = "Client",
                LastName = "Admin",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Applicationsettings.Password,
                isSuperAdmin = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = "img/admin.png"
            };
            if (userManager.Users.All(u => u.Id != clientAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAdmin, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAdmin.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientCreator.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssigner.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssessor.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAdmin.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorSupervisor.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAgent.ToString());
                }
            }

            //Seed client creator
            var clientCreator = new ApplicationUser()
            {
                UserName = "client-creator@admin.com",
                Email = "client-creator@admin.com",
                FirstName = "Client",
                LastName = "Creator",
                EmailConfirmed = true,
                Password = Applicationsettings.Password,
                PhoneNumberConfirmed = true,
                isSuperAdmin = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = "img/creator.jpg"
            };
            if (userManager.Users.All(u => u.Id != clientCreator.Id))
            {
                var user = await userManager.FindByEmailAsync(clientCreator.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientCreator, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(clientCreator, AppRoles.ClientCreator.ToString());
                }
            }

            //Seed client assigner
            var clientAssigner = new ApplicationUser()
            {
                UserName = "client-assigner@admin.com",
                Email = "client-assigner@admin.com",
                FirstName = "Client",
                LastName = "Assigner",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Applicationsettings.Password,
                isSuperAdmin = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = "img/assigner.png"
            };
            if (userManager.Users.All(u => u.Id != clientAssigner.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssigner.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssigner, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(clientAssigner, AppRoles.ClientAssigner.ToString());
                }
            }

            //Seed client assessor
            var clientAssessor = new ApplicationUser()
            {
                UserName = "client-assessor@admin.com",
                Email = "client-assessor@admin.com",
                FirstName = "Client",
                LastName = "Assessor",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Applicationsettings.Password,
                isSuperAdmin = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = "img/assessor.png"
            };
            if (userManager.Users.All(u => u.Id != clientAssessor.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssessor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssessor, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(clientAssessor, AppRoles.ClientAssessor.ToString());
                }
            }

            //Seed Vendor Admin
            var vendorAdmin = new ApplicationUser()
            {
                UserName = "vendor-admin@admin.com",
                Email = "vendor-admin@admin.com",
                FirstName = "Vendor",
                LastName = "Admin",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Applicationsettings.Password,
                isSuperAdmin = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = "img/vendor-admin.png"
            };
            if (userManager.Users.All(u => u.Id != vendorAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAdmin, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorAdmin.ToString());
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorSupervisor.ToString());
                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorAgent.ToString());
                }
            }

            //Seed Vendor Admin
            var vendorSupervisor = new ApplicationUser()
            {
                UserName = "vendor-supervisor@admin.com",
                Email = "vendor-supervisor@admin.com",
                FirstName = "Vendor",
                LastName = "Supervisor",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Applicationsettings.Password,
                isSuperAdmin = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = "img/supervisor.png"
            };
            if (userManager.Users.All(u => u.Id != vendorSupervisor.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorSupervisor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorSupervisor, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.VendorSupervisor.ToString());
                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.VendorAgent.ToString());
                }
            }

            //Seed Vendor Admin
            var vendorAgent = new ApplicationUser()
            {
                UserName = "vendor-agent@admin.com",
                Email = "vendor-agent@admin.com",
                FirstName = "Vendor",
                LastName = "Agent",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Applicationsettings.Password,
                isSuperAdmin = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == Applicationsettings.CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(Applicationsettings.CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = "img/agent.jpg"
            };
            if (userManager.Users.All(u => u.Id != vendorAgent.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAgent.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAgent, Applicationsettings.Password);
                    await userManager.AddToRoleAsync(vendorAgent, AppRoles.VendorAgent.ToString());
                }
            }
        }
    }
}
