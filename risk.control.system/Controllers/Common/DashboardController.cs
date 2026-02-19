using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.PortalAdmin;
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
        private readonly IAgencyDashboardService _agencyDashboardService;
        private readonly IAdminDashBoardService _adminDashBoardService;
        private readonly ICompanyDashboardService _companyDashboardService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly INotyfService _notifyService;

        public DashboardController(ILogger<DashboardController> logger,
            IAgencyDashboardService agencyDashboardService,
            IAdminDashBoardService adminDashBoardService,
            ICompanyDashboardService companyDashboardService,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService
            )
        {
            _logger = logger;
            _agencyDashboardService = agencyDashboardService;
            _adminDashBoardService = adminDashBoardService;
            _companyDashboardService = companyDashboardService;
            _signInManager = signInManager;
            _notifyService = notifyService;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

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
                    var model = await _companyDashboardService.GetCreatorCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(PORTAL_ADMIN.DISPLAY_NAME))
                {
                    var model = await _adminDashBoardService.GetSuperAdminCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(COMPANY_ADMIN.DISPLAY_NAME))
                {
                    var model = await _companyDashboardService.GetCompanyAdminCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(ASSESSOR.DISPLAY_NAME))
                {
                    var model = await _companyDashboardService.GetAssessorCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(MANAGER.DISPLAY_NAME))
                {
                    var model = await _companyDashboardService.GetManagerCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(AGENCY_ADMIN.DISPLAY_NAME) || userRole.Contains(SUPERVISOR.DISPLAY_NAME))
                {
                    var model = await _agencyDashboardService.GetSupervisorCount(userEmail, userRole);
                    return View(model);
                }
                else if (userRole.Contains(AGENT.DISPLAY_NAME))
                {
                    var model = await _agencyDashboardService.GetAgentCount(userEmail, userRole);
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
                _notifyService.Error("OOPs !!!...Contact Admin");
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(AccountController.Login), "Account");
            }
        }
    }
}