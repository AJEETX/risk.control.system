using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class PortalAdminSeed
    {
        public static async Task Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            string pinCodeCode)
        {

            var pinCode =await context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.Code == pinCodeCode);

            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(PORTAL_ADMIN.PROFILE_IMAGE));
            var adminImage = File.ReadAllBytes(adminImagePath);

            if (adminImage == null)
            {
                adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(USER_PHOTO));
                adminImage = File.ReadAllBytes(adminImagePath);
            }
            //Seed portal admin
            var portalAdmin = new ApplicationUser()
            {
                UserName = PORTAL_ADMIN.USERNAME,
                Email = PORTAL_ADMIN.EMAIL,
                FirstName = PORTAL_ADMIN.FIRST_NAME,
                LastName = PORTAL_ADMIN.LAST_NAME,
                Password = TestingData,
                EmailConfirmed = true,
                IsSuperAdmin = true,
                Active = true,
                Addressline = "11, Main Road",
                IsClientAdmin = true,
                IsVendorAdmin = true,
                PhoneNumberConfirmed = true,
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.Country.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = PORTAL_ADMIN.PROFILE_IMAGE,
                ProfilePicture = adminImage,
                Role = AppRoles.PORTAL_ADMIN,
                Updated = DateTime.Now
            };
            if (userManager.Users.All(u => u.Id != portalAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(portalAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(portalAdmin, TestingData);
                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.PORTAL_ADMIN.ToString());

                    //var portalAdminRole = new ApplicationRole(AppRoles.PORTALADMIN.ToString(), AppRoles.PORTALADMIN.ToString());
                    //portalAdmin.ApplicationRoles.Add(portalAdminRole);

                    //await userManager.AddToRoleAsync(portalAdmin, AppRoles.COMPANY_ADMIN.ToString());
                    ////var clientAdminRole = new ApplicationRole(AppRoles.COMPANY_ADMIN.ToString(), AppRoles.COMPANY_ADMIN.ToString());
                    ////portalAdmin.ApplicationRoles.Add(clientAdminRole);
                    //await userManager.AddToRoleAsync(portalAdmin, AppRoles.CREATOR.ToString());
                    ////context.ApplicationRole.Add(new ApplicationRole(AppRoles.CREATOR.ToString(), AppRoles.CREATOR.ToString()));
                    //await userManager.AddToRoleAsync(portalAdmin, AppRoles.Assigner.ToString());
                    ////context.ApplicationRole.Add(new ApplicationRole(AppRoles.Assigner.ToString(), AppRoles.Assigner.ToString()));
                    //await userManager.AddToRoleAsync(portalAdmin, AppRoles.ASSESSOR.ToString());
                    ////context.ApplicationRole.Add(new ApplicationRole(AppRoles.ASSESSOR.ToString(), AppRoles.ASSESSOR.ToString()));
                    //await userManager.AddToRoleAsync(portalAdmin, AppRoles.AGENCY_ADMIN.ToString());
                    ////context.ApplicationRole.Add(new ApplicationRole(AppRoles.AGENCY_ADMIN.ToString(), AppRoles.AGENCY_ADMIN.ToString()));
                    //await userManager.AddToRoleAsync(portalAdmin, AppRoles.SUPERVISOR.ToString());
                    ////context.ApplicationRole.Add(new ApplicationRole(AppRoles.SUPERVISOR.ToString(), AppRoles.SUPERVISOR.ToString()));
                    //await userManager.AddToRoleAsync(portalAdmin, AppRoles.AGENT.ToString());
                    //context.ApplicationRole.Add(new ApplicationRole(AppRoles.AGENT.ToString(), AppRoles.AGENT.ToString()));
                }

                ////////PERMISSIONS TO MODULES

                var adminRole = await roleManager.FindByNameAsync(AppRoles.PORTAL_ADMIN.ToString()) ?? default!;
                var allClaims = await roleManager.GetClaimsAsync(adminRole);

                //ADD PERMISSIONS

                //var moduleList = new List<string> { nameof(Underwriting), nameof(Claim) };
                //var modules = context.PermissionModule.ToList();

                //foreach (var module in moduleList)
                //{
                //    var modulePermissions = Permissions.GeneratePermissionsForModule(module);

                //    foreach (var modulePermission in modulePermissions)
                //    {
                //        if (!allClaims.Any(a => a.Type == PERMISSION && a.Value == modulePermission))
                //        {
                //            await roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(PERMISSION, modulePermission));
                //        }
                //    }
                //}
            }
        }
    }
}