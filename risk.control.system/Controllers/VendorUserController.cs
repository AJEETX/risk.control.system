﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Agency")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorUserController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IPasswordHasher<VendorApplicationUser> passwordHasher;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IToastNotification toastNotification;
        private readonly ApplicationDbContext context;

        public VendorUserController(UserManager<VendorApplicationUser> userManager,
            IPasswordHasher<VendorApplicationUser> passwordHasher,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.passwordHasher = passwordHasher;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            this.toastNotification = toastNotification;
            this.context = context;
            UserList = new List<UsersViewModel>();
        }

        public IActionResult Index(string id)
        {
            ViewData["vendorId"] = id;
            var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies") { Parent = agencysPage };
            var agencyPage2 = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage2, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View();
        }
    }
}