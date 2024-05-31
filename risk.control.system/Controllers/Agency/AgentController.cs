using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb(" Claims")]
        [Authorize(Roles = AGENT.DISPLAY_NAME)]
    public class AgentController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IClaimsVendorService vendorService;
        private readonly IInvestigationReportService investigationReportService;

        public AgentController(INotyfService notifyService, IClaimsVendorService vendorService, IInvestigationReportService investigationReportService)
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
            return View();
        }

        [Breadcrumb("Submit",FromAction = "Agent")]
        public async Task<IActionResult> GetInvestigate(string selectedcase, bool uploaded = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be investigate.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await vendorService.GetInvestigate(userEmail, selectedcase, uploaded);

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


                var model = await investigationReportService.SubmittedDetail(id);

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
