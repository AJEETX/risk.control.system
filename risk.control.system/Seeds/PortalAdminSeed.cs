using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class PortalAdminSeed
    {
        public static async Task Seed(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            int pinCodeCode,
            IFileStorageService fileStorageService)
        {

            var pinCode = await context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.Code == pinCodeCode);

            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(PORTAL_ADMIN.PROFILE_IMAGE));
            var adminImage = File.ReadAllBytes(adminImagePath);

            if (adminImage == null)
            {
                adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(USER_PHOTO));
                adminImage = File.ReadAllBytes(adminImagePath);
            }
            var extension = Path.GetExtension(adminImagePath);

            var (fileName, relativePath) = await fileStorageService.SaveAsync(adminImage, extension, "portal-admin");

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
                PhoneNumber = string.Equals(pinCode.Country.Code, "au", StringComparison.OrdinalIgnoreCase) ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.Country.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.PORTAL_ADMIN,
                Updated = DateTime.Now
            };
            if (userManager.Users.All(u => u.Id != portalAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(portalAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(portalAdmin, TestingData);
                    await userManager.AddToRoleAsync(portalAdmin, PORTAL_ADMIN.DISPLAY_NAME);
                }

                ////////PERMISSIONS TO MODULES

                //var adminRole = await roleManager.FindByNameAsync(AppRoles.PORTAL_ADMIN.ToString()) ?? default!;
                //var allClaims = await roleManager.GetClaimsAsync(adminRole);

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