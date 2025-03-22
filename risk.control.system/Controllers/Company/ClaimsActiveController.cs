using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using CsvHelper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;
using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Helpers;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    [Breadcrumb(" Cases")]
    public class ClaimsActiveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public ClaimsActiveController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IFtpService ftpService,
            INotyfService notifyService,
            IInvestigationReportService investigationReportService,
            IClaimPolicyService claimPolicyService)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.investigationReportService = investigationReportService;
        }

        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                if (userRole.Value.Contains(AppRoles.CREATOR.ToString()))
                {
                    return RedirectToAction("Active");
                }
                else if (userRole.Value.Contains(AppRoles.ASSESSOR.ToString()))
                {
                    return RedirectToAction("Assessor");
                }
                else if (userRole.Value.Contains(AppRoles.MANAGER.ToString()))
                {
                    return RedirectToAction("Manager");
                }
                else
                {
                    return RedirectToAction("Index", "Dashboard");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }


        [Breadcrumb(title: "Active")]
        public IActionResult Active()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Active")]
        public async Task<IActionResult> ActiveDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}