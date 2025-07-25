﻿using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Role")]
    public class UserRolesController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;

        public UserRolesController(UserManager<ApplicationUser> userManager,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ISmsService smsService,
            SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            this.smsService = smsService;
            this.signInManager = signInManager;
        }

        public async Task<IActionResult> Index(string userId)
        {
            var userRoles = new List<UserRoleViewModel>();
            //ViewBag.userId = userId;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                notifyService.Error("user not found!");
                return NotFound();
            }
            //ViewBag.UserName = user.UserName;
            foreach (var role in roleManager.Roles)
            {
                var userRoleViewModel = new UserRoleViewModel
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
            var model = new UserRolesViewModel
            {
                UserId = userId,
                UserName = user.UserName,
                UserRoleViewModel = userRoles
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string userId, UserRolesViewModel model)
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
            result = await userManager.AddToRolesAsync(user, model.UserRoleViewModel.Where(x => x.Selected).Select(y => y.RoleName));
            var currentUser = await userManager.GetUserAsync(User);
            await signInManager.RefreshSignInAsync(currentUser);

            notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
            return RedirectToAction(nameof(UserController.Index), "User");
        }
    }
}