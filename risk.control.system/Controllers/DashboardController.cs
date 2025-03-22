using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [DefaultBreadcrumb("Home")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService dashboardService;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;

        public DashboardController(IDashboardService dashboardService,
            SignInManager<Models.ApplicationUser> signInManager,
            INotyfService notifyService
            )
        {
            this.dashboardService = dashboardService;
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

                if(userRole.Value.Contains(AppRoles.CREATOR.ToString()))
                {
                    var model = dashboardService.GetCreatorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.PORTAL_ADMIN.ToString()))
                {
                    var model = dashboardService.GetSuperAdminCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.COMPANY_ADMIN.ToString()))
                {
                    var model = dashboardService.GetCompanyAdminCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.ASSESSOR.ToString()))
                {
                    var model = dashboardService.GetAssessorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }

                else if (userRole.Value.Contains(AppRoles.MANAGER.ToString()))
                {
                    var model = dashboardService.GetManagerCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.AGENCY_ADMIN.ToString()) || userRole.Value.Contains(AppRoles.SUPERVISOR.ToString()))
                {
                    var model = dashboardService.GetSupervisorCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else if (userRole.Value.Contains(AppRoles.AGENT.ToString()))
                {
                    var model = dashboardService.GetAgentCount(currentUserEmail, userRole.Value);
                    return View(model);

                }
                else
                {
                    var model = dashboardService.GetClaimsCount(currentUserEmail, userRole.Value);

                    return View(model);
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
                if (userRole.Value.Contains(AppRoles.PORTAL_ADMIN.ToString())
                                || userRole.Value.Contains(AppRoles.COMPANY_ADMIN.ToString())
                                || userRole.Value.Contains(AppRoles.CREATOR.ToString())
                                )
                {
                    Dictionary<string, int> monthlyExpense = dashboardService.CalculateAgencyCaseStatus(userEmail);
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

        public JsonResult GetClaimChart()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            Dictionary<string, int> monthlyExpense = dashboardService.CalculateCaseChart(userEmail);
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