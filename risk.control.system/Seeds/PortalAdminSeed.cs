using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class PortalAdminSeed
    {
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, int pinCodeCode, IFileStorageService fileStorageService)
        {
            var pinCode = await context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.Code == pinCodeCode);
            string adminImagePath = Path.Combine(env.WebRootPath, "img", Path.GetFileName(PORTAL_ADMIN.PROFILE_IMAGE));
            var adminImage = await File.ReadAllBytesAsync(adminImagePath);
            var extension = Path.GetExtension(adminImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(adminImage, extension, "portal-admin");
            var portalAdmin = GetUserData(pinCode!, relativePath);
            if (userManager.Users.All(u => u.Id != portalAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(portalAdmin.Email!);
                if (user == null)
                {
                    await userManager.CreateAsync(portalAdmin, TestingData);
                    await userManager.AddToRoleAsync(portalAdmin, PORTAL_ADMIN.DISPLAY_NAME);
                }
                var adminRole = await roleManager.FindByNameAsync(AppRoles.PORTAL_ADMIN.ToString()) ?? default!;
                var allClaims = await roleManager.GetClaimsAsync(adminRole);
                string Claims = "Claims";
                var moduleList = new List<string> {
                    nameof(Claims) };
                var modules = context.PermissionModule.ToList();
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
        private static ApplicationUser GetUserData(PinCode pinCode, string relativePath)
        {
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
                IsCompanyAdmin = true,
                IsVendorAdmin = true,
                PhoneNumberConfirmed = true,
                PhoneNumber = string.Equals(pinCode!.Country!.Code, "au", StringComparison.OrdinalIgnoreCase) ? SAMPLE_MOBILE_AUSTRALIA : SAMPLE_MOBILE_INDIA,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.Country.CountryId,
                DistrictId = pinCode?.DistrictId ?? default!,
                StateId = pinCode?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.PORTAL_ADMIN,
                Updated = DateTime.UtcNow
            };
            return portalAdmin;
        }
    }
}