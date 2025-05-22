using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;

namespace risk.control.system.Controllers
{
    [DefaultBreadcrumb("Home")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService dashboardService;
        private readonly IDashboardCountService dashboardCountService;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;

        public DashboardController(IDashboardService dashboardService,
            IDashboardCountService dashboardCountService,
            SignInManager<Models.ApplicationUser> signInManager,
            INotyfService notifyService
            )
        {
            this.dashboardService = dashboardService;
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

                if (userRole.Value.Contains(AppRoles.CREATOR.ToString()))
                {
                    var model = dashboardCountService.GetCreatorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.PORTAL_ADMIN.ToString()))
                {
                    var model = dashboardCountService.GetSuperAdminCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.COMPANY_ADMIN.ToString()))
                {
                    var model = dashboardCountService.GetCompanyAdminCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.ASSESSOR.ToString()))
                {
                    var model = dashboardCountService.GetAssessorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }

                else if (userRole.Value.Contains(AppRoles.MANAGER.ToString()))
                {
                    var model = dashboardCountService.GetManagerCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
                {
                    var model = dashboardCountService.GetSupervisorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.AGENT.ToString()))
                {
                    var model = dashboardCountService.GetAgentCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else
                {

                    return View();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                notifyService.Error("OOPs !!!...Contact Admin");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await signInManager.SignOutAsync();
                return RedirectToAction(nameof(AccountController.Login), "Account");
            }
        }

        public JsonResult GetAgentClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole != null)
            {
                if (userRole.Value.Contains(AppRoles.MANAGER.ToString())
                                || userRole.Value.Contains(AppRoles.ASSESSOR.ToString())
                                || userRole.Value.Contains(AppRoles.COMPANY_ADMIN.ToString())
                                || userRole.Value.Contains(AppRoles.CREATOR.ToString())
                                )
                {
                    Dictionary<string, int> monthlyExpense = dashboardService.CalculateAgencyClaimStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
                else if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
                {
                    Dictionary<string, int> monthlyExpense = dashboardService.CalculateAgentCaseStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
            }

            return new JsonResult(null);
        }

        public JsonResult GetAgentUnderwriting()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole != null)
            {
                if (userRole.Value.Contains(AppRoles.MANAGER.ToString())
                                || userRole.Value.Contains(AppRoles.ASSESSOR.ToString())
                                || userRole.Value.Contains(AppRoles.COMPANY_ADMIN.ToString())
                                || userRole.Value.Contains(AppRoles.CREATOR.ToString())
                                )
                {
                    Dictionary<string, int> monthlyExpense = dashboardService.CalculateAgencyUnderwritingStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
                else if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
                {
                    Dictionary<string, int> monthlyExpense = dashboardService.CalculateAgentCaseStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
            }

            return new JsonResult(null);
        }

        public JsonResult GetMonthlyClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            var monthlyExpense = dashboardService.CalculateMonthlyCaseStatus(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public JsonResult GetWeeklyClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = dashboardService.CalculateWeeklyCaseStatus(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public JsonResult GetWeeklyPieClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = dashboardService.CalculateWeeklyCaseStatusPieClaims(userEmail);
            return new JsonResult(monthlyExpense);
        }


        public JsonResult GetWeeklyPieUnderwriting()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = dashboardService.CalculateWeeklyCaseStatusPieUnderwritings(userEmail);
            return new JsonResult(monthlyExpense);
        }
        public JsonResult GetClaimChart()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = dashboardService.CalculateCaseChart(userEmail);
            return new JsonResult(monthlyExpense);
        }

        public JsonResult GetClaimWeeklyTat()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var monthlyExpense = dashboardService.CalculateTimespan(userEmail);
            return new JsonResult(monthlyExpense);
        }

    }
}