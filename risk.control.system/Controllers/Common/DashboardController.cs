using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.PortalAdmin;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Api;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [DefaultBreadcrumb("Home")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IAgencyDashboardService agencyDashboardService;
        private readonly IAdminDashBoardService adminDashBoardService;
        private readonly ICompanyDashboardService companyDashboardService;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;

        public DashboardController(ILogger<DashboardController> logger,
            IAgencyDashboardService agencyDashboardService,
            IAdminDashBoardService adminDashBoardService,
            ICompanyDashboardService companyDashboardService,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService
            )
        {
            _logger = logger;
            this.agencyDashboardService = agencyDashboardService;
            this.adminDashBoardService = adminDashBoardService;
            this.companyDashboardService = companyDashboardService;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("NOT FOUND!!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(userRole))
                {
                    _logger.LogWarning("User {UserEmail} has no role claim.", userEmail);
                    return new JsonResult(null);
                }

                if (userRole.Contains(CREATOR.DISPLAY_NAME))
                {
                    var model = await companyDashboardService.GetCreatorCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(PORTAL_ADMIN.DISPLAY_NAME))
                {
                    var model = await adminDashBoardService.GetSuperAdminCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(COMPANY_ADMIN.DISPLAY_NAME))
                {
                    var model = await companyDashboardService.GetCompanyAdminCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(ASSESSOR.DISPLAY_NAME))
                {
                    var model = await companyDashboardService.GetAssessorCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(MANAGER.DISPLAY_NAME))
                {
                    var model = await companyDashboardService.GetManagerCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(AGENCY_ADMIN.DISPLAY_NAME) || userRole.Contains(SUPERVISOR.DISPLAY_NAME))
                {
                    var model = await agencyDashboardService.GetSupervisorCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(AGENT.DISPLAY_NAME))
                {
                    var model = await agencyDashboardService.GetAgentCount(userEmail, userRole);
                    return View(model);
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPs !!!...Contact Admin");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await signInManager.SignOutAsync();
                return RedirectToAction(nameof(AccountController.Login), "Account");
            }
        }
    }
}