using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.Helpers.Permissions;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Permission")]
    public class PermissionController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly INotyfService notifyService;

        public PermissionController(RoleManager<ApplicationRole> roleManager, INotyfService notifyService)
        {
            _roleManager = roleManager;
            this.notifyService = notifyService;
        }

        public async Task<ActionResult> Index(string Id)
        {
            var model = new PermissionsViewModel();

            var models = new List<PermissionViewModel>();

            //var moduleList = new List<Type> { typeof(Permissions.Claim), typeof(Permissions.Underwriting) };
            var moduleList = new List<string> { nameof(Underwriting), nameof(Claim) };

            var role = await _roleManager.FindByIdAsync(Id);

            foreach (var module in moduleList)
            {
                var permission = new PermissionViewModel
                {
                    RoleClaims = await GetModulePermission(role, module)
                };
                models.Add(permission);
            }
            model.PermissionViewModels = models;
            model.RoleName = role.Name;
            model.RoleId = Id;
            return View(model);
        }

        private async Task<List<RoleClaimsViewModel>> GetModulePermission(ApplicationRole role, string module)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var modulePermissions = new List<RoleClaimsViewModel>();
            var modulePermission = Permissions.GeneratePermissionsForModule(module);
            foreach (var item in modulePermission)
            {
                modulePermissions.Add(new RoleClaimsViewModel
                {
                    Type = Applicationsettings.PERMISSION,
                    Value = item
                });
            }

            var allClaimValues = modulePermissions.Select(a => a.Value).ToList();
            var roleClaimValues = claims.Select(a => a.Value).ToList();
            var authorizedClaims = allClaimValues.Intersect(roleClaimValues).ToList();
            foreach (var permission in modulePermissions)
            {
                if (authorizedClaims.Any(a => a == permission.Value))
                {
                    permission.Selected = true;
                }
            }
            return modulePermissions;
        }

        public async Task<IActionResult> Update(PermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var item in model.PermissionViewModels)
            {
                var selectedClaims = item.RoleClaims.Where(a => a.Selected).ToList();
                foreach (var claim in selectedClaims)
                {
                    await _roleManager.AddPermissionClaim(role, claim.Value);
                }
            }

            notifyService.Success("roles updated successfully!");
            return RedirectToAction("Index", "Roles", new { Id = model.RoleId });
        }
    }
}