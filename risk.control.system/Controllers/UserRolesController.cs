using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using System.Security.Claims;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Seeds;

namespace risk.control.system.Controllers
{
    public class UserRolesController : Controller
    {
    private readonly SignInManager<Models.ApplicationUser> signInManager;
        private readonly UserManager<Models.ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;

        public UserRolesController(UserManager<Models.ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager,
            SignInManager<Models.ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
        }
        public async Task<IActionResult> Index(string userId)
        {
            var userRoles = new List<UserRoleViewModel>();
            //ViewBag.userId = userId;
            Models.ApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            //ViewBag.UserName = user.UserName;
            foreach (var role in roleManager.Roles)
            {
                var userRoleViewModel = new UserRoleViewModel
                {
                    RoleId = role.Id.ToString(),
                    RoleName = role.Name
                };
                if (await userManager.IsInRoleAsync(user, role.Name))
                {
                    userRoleViewModel.Selected = true;
                }
                else
                {
                    userRoleViewModel.Selected = false;
                }
                userRoles.Add(userRoleViewModel);
            }
            var model = new UserRolesViewModel
            {
                UserId = userId,
                UserName = user.UserName,
                UserRoleViewModel = userRoles
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(string id, ManageUserRolesViewModel model)
        {
            var user = await userManager.FindByIdAsync(id);
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            result = await userManager.AddToRolesAsync(user, model.UserRoles.Where(x => x.Selected).Select(y => y.RoleName));
            var currentUser = await userManager.GetUserAsync(User);
            await signInManager.RefreshSignInAsync(currentUser);

            return RedirectToAction("Index", new { userId = id });
        }
        private static async Task SeedSuperAdminAsync(UserManager<Models.ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            //Seed portal User
            var portalAdmin = new Models.ApplicationUser()
            {
                UserName = "portal-admin@admin.com",
                Email = "portal-admin@admin.com",
                FirstName = "Portal",
                LastName = "Admin",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
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
                await SeedClaimsForSuperAdmin(roleManager);
            }
        }
        private async static Task SeedClaimsForSuperAdmin(RoleManager<ApplicationRole> roleManager)
        {
            var adminRole = await roleManager.FindByNameAsync(AppRoles.PortalAdmin.ToString());
            await AddPermissionClaim(roleManager, adminRole, "Products");
        }

        public static async Task AddPermissionClaim(RoleManager<ApplicationRole> roleManager, ApplicationRole role, string module)
        {
            var allClaims = await roleManager.GetClaimsAsync(role);
            var allPermissions = Permissions.GeneratePermissionsForModule(module);
            foreach (var permission in allPermissions)
            {
                if (!allClaims.Any(a => a.Type == "Permission" && a.Value == permission))
                {
                    await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                }
            }
        }
    }
}
