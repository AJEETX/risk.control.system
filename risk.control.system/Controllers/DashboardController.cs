using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [DefaultBreadcrumb("Home")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService dashboardService;
        private readonly ILogger<DashboardController> logger;
        private readonly IDashboardCountService dashboardCountService;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;

        public DashboardController(IDashboardService dashboardService,
            ILogger<DashboardController> logger,
            IDashboardCountService dashboardCountService,
            SignInManager<Models.ApplicationUser> signInManager,
            INotyfService notifyService
            )
        {
            this.dashboardService = dashboardService;
            this.logger = logger;
            this.dashboardCountService = dashboardCountService;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                if (userRole.Value.Contains(CREATOR.DISPLAY_NAME))
                {
                    var model = await dashboardCountService.GetCreatorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(PORTAL_ADMIN.DISPLAY_NAME))
                {
                    var model = await dashboardCountService.GetSuperAdminCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(COMPANY_ADMIN.DISPLAY_NAME))
                {
                    var model = await dashboardCountService.GetCompanyAdminCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(ASSESSOR.DISPLAY_NAME))
                {
                    var model = await dashboardCountService.GetAssessorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }

                else if (userRole.Value.Contains(MANAGER.DISPLAY_NAME))
                {
                    var model = await dashboardCountService.GetManagerCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AGENCY_ADMIN.DISPLAY_NAME) || userRole.Value.Contains(SUPERVISOR.DISPLAY_NAME))
                {
                    var model = await dashboardCountService.GetSupervisorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AGENT.DISPLAY_NAME))
                {
                    var model = await dashboardCountService.GetAgentCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else
                {

                    return View();
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPs !!!...Contact Admin");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await signInManager.SignOutAsync();
                return RedirectToAction(nameof(AccountController.Login), "Account");
            }
        }

        public async Task<JsonResult> GetAgentClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole != null)
            {
                if (userRole.Value.Contains(MANAGER.DISPLAY_NAME)
                                || userRole.Value.Contains(ASSESSOR.DISPLAY_NAME)
                                || userRole.Value.Contains(COMPANY_ADMIN.DISPLAY_NAME)
                                || userRole.Value.Contains(CREATOR.DISPLAY_NAME)
                                )
                {
                    Dictionary<string, int> monthlyExpense = await dashboardService.CalculateAgencyClaimStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
                else if (userRole.Value.Contains(AGENCY_ADMIN.DISPLAY_NAME) || userRole.Value.Contains(SUPERVISOR.DISPLAY_NAME))
                {
                    Dictionary<string, int> monthlyExpense = await dashboardService.CalculateAgentCaseStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
            }

            return new JsonResult(null);
        }

        public async Task<JsonResult> GetAgentUnderwriting()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole != null)
            {
                if (userRole.Value.Contains(MANAGER.DISPLAY_NAME)
                                || userRole.Value.Contains(ASSESSOR.DISPLAY_NAME)
                                || userRole.Value.Contains(COMPANY_ADMIN.DISPLAY_NAME)
                                || userRole.Value.Contains(CREATOR.DISPLAY_NAME)
                                )
                {
                    Dictionary<string, int> monthlyExpense = await dashboardService.CalculateAgencyUnderwritingStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
                else if (userRole.Value.Contains(AGENCY_ADMIN.DISPLAY_NAME) || userRole.Value.Contains(SUPERVISOR.DISPLAY_NAME))
                {
                    Dictionary<string, int> monthlyExpense = await dashboardService.CalculateAgentCaseStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
            }

            return new JsonResult(null);
        }

        public async Task<JsonResult> GetMonthlyClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            var monthlyExpense = await dashboardService.CalculateMonthlyCaseStatus(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public async Task<JsonResult> GetWeeklyClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = await dashboardService.CalculateWeeklyCaseStatus(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public async Task<JsonResult> GetWeeklyPieClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = await dashboardService.CalculateWeeklyCaseStatusPieClaims(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public async Task<JsonResult> GetWeeklyPieUnderwriting()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = await dashboardService.CalculateWeeklyCaseStatusPieUnderwritings(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public async Task<JsonResult> GetClaimChart()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = await dashboardService.CalculateCaseChart(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public async Task<JsonResult> GetClaimWeeklyTat()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = await dashboardService.CalculateTimespan(userEmail);
            return new JsonResult(monthlyExpense);
        }
    }
}