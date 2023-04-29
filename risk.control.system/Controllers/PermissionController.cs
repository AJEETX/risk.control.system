using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class PermissionController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public PermissionController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<ActionResult> Index(string Id)
        {
            var model = new PermissionsViewModel();

            var models = new List<PermissionViewModel>();

            var moduleList = new List<Type> { /*typeof(Permissions.Claims),*/ typeof(Permissions.Products) };

            var role = await _roleManager.FindByIdAsync(Id);

            foreach (var module in moduleList)
            {
                var permission = new PermissionViewModel
                {
                    RoleId = Id,
                    RoleName = role.Name,
                    RoleClaims = await GetModulePermission(module, role)
                };
                models.Add(permission);
            }
            model.PermissionViewModels= models;
            model.RoleName = role.Name;
            model.RoleId = Id;
            return View(model);
        }
        private async Task<List<RoleClaimsViewModel>> GetModulePermission(Type type, ApplicationRole role)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var modulePermissions = new List<RoleClaimsViewModel>();
            modulePermissions.GetPermissions(type);
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

            return RedirectToAction("Index","Roles", new { Id = model.RoleId });
        }
    }
}
