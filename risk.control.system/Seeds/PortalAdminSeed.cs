using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;
using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Seeds
{
    public static class PortalAdminSeed
    {
        public static async Task Seed(ApplicationDbContext context, EntityEntry<Country> indiaCountry, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            //Seed portal admin
            var portalAdmin = new ApplicationUser()
            {
                UserName = PORTAL_ADMIN.USERNAME,
                Email = PORTAL_ADMIN.EMAIL,
                FirstName = PORTAL_ADMIN.FIRST_NAME,
                LastName = PORTAL_ADMIN.LAST_NAME,
                Password = Password,
                EmailConfirmed = true,
                isSuperAdmin = true,
                PhoneNumberConfirmed = true,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = PORTAL_ADMIN.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != portalAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(portalAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(portalAdmin, Password);
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.PortalAdmin.ToString());

                    var portalAdminRole = new ApplicationRole(AppRoles.PortalAdmin.ToString(), AppRoles.PortalAdmin.ToString());
                    portalAdmin.ApplicationRoles.Add(portalAdminRole);

                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAdmin.ToString());

                    var clientAdminRole = new ApplicationRole(AppRoles.ClientAdmin.ToString(), AppRoles.ClientAdmin.ToString());


                    portalAdmin.ApplicationRoles.Add(clientAdminRole);
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientCreator.ToString());
                    context.ApplicationRole.Add(new ApplicationRole(AppRoles.ClientCreator.ToString(), AppRoles.ClientCreator.ToString()));
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAssigner.ToString());
                    context.ApplicationRole.Add(new ApplicationRole(AppRoles.ClientAssigner.ToString(), AppRoles.ClientAssigner.ToString()));
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAssessor.ToString());
                    context.ApplicationRole.Add(new ApplicationRole(AppRoles.ClientAssessor.ToString(), AppRoles.ClientAssessor.ToString()));
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorAdmin.ToString());
                    context.ApplicationRole.Add(new ApplicationRole(AppRoles.VendorAdmin.ToString(), AppRoles.VendorAdmin.ToString()));
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorSupervisor.ToString());
                    context.ApplicationRole.Add(new ApplicationRole(AppRoles.VendorSupervisor.ToString(), AppRoles.VendorSupervisor.ToString()));
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorAgent.ToString());
                    context.ApplicationRole.Add(new ApplicationRole(AppRoles.VendorAgent.ToString(), AppRoles.VendorAgent.ToString()));
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
                        if (!allClaims.Any(a => a.Type == PERMISSION && a.Value == modulePermission))
                        {
                            await roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(PERMISSION, modulePermission));
                        }
                    }
                }
            }
        }
    }
}
