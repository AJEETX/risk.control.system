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

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public ClaimsInvestigationController(ApplicationDbContext context,
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

        [Breadcrumb(" Claims")]
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
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }


        [Breadcrumb(title: "Active")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Active()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }


        [Breadcrumb(title: " Details", FromAction = "Active")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> ActiveDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}