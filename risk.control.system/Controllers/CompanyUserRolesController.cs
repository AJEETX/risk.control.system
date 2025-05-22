using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Edit Role", FromAction = "Details", FromController = typeof(CompanyUserController))]
    public class CompanyUserRolesController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;

        public CompanyUserRolesController(UserManager<ApplicationUser> userManager,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService,
            SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            smsService = SmsService;
            this.signInManager = signInManager;
        }

        public async Task<IActionResult> Index(string userId)
        {
            var userRoles = new List<CompanyUserRoleViewModel>();
            //ViewBag.userId = userId;
            ClientCompanyApplicationUser user = (ClientCompanyApplicationUser)await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                notifyService.Error("user not found!");
                return NotFound();
            }
            //ViewBag.UserName = user.UserName;
            foreach (var role in roleManager.Roles.Where(r =>
                r.Name.Contains(AppRoles.COMPANY_ADMIN.ToString()) ||
                r.Name.Contains(AppRoles.CREATOR.ToString()) ||
                r.Name.Contains(AppRoles.ASSESSOR.ToString())))
            {
                var userRoleViewModel = new CompanyUserRoleViewModel
                {
                    RoleId = role.Id.ToString(),
                    RoleName = role?.Name
                };
                if (await userManager.IsInRoleAsync(user, role?.Name))
                {
                    userRoleViewModel.Selected = true;
                }
                else
                {
                    userRoleViewModel.Selected = false;
                }
                userRoles.Add(userRoleViewModel);
            }
            var model = new CompanyUserRolesViewModel
            {
                UserId = userId,
                CompanyId = user.ClientCompanyId.Value,
                UserName = user.UserName,
                CompanyUserRoleViewModel = userRoles
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string userId, CompanyUserRolesViewModel model)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.Updated = DateTime.Now;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            result = await userManager.AddToRolesAsync(user, model.CompanyUserRoleViewModel.Where(x => x.Selected).Select(y => y.RoleName));
            var currentUser = await userManager.GetUserAsync(User);
            await signInManager.RefreshSignInAsync(currentUser);

            await smsService.DoSendSmsAsync(user.PhoneNumber, "User role edited . Email : " + user.Email);

            notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
            return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { Id = model.CompanyId });
        }
    }
}