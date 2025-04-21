using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Helpers;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb(" Cases")]
        [Authorize(Roles = AGENT.DISPLAY_NAME)]
    public class AgentController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ICaseVendorService vendorService;
        private readonly IInvestigationReportService investigationReportService;

        public AgentController(INotyfService notifyService, ICaseVendorService vendorService, IInvestigationReportService investigationReportService)
        {
            this.notifyService = notifyService;
            this.vendorService = vendorService;
            this.investigationReportService = investigationReportService;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Agent");
        }

        [Breadcrumb(" Tasks")]
        public IActionResult Agent()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb("Submit",FromAction = "Agent")]
        public async Task<IActionResult> GetInvestigate(long selectedcase, bool uploaded = false)
        {
            try
            {
                if (selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be investigate.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await vendorService.GetInvestigate(currentUserEmail, selectedcase, uploaded);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Submitted")]
        public IActionResult Submitted()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }
        [Breadcrumb(title: " Detail", FromAction = "Submitted")]
        public async Task<IActionResult> SubmittedDetail(string id)
        {
            if (id == null)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await investigationReportService.SubmittedDetail(id, currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
