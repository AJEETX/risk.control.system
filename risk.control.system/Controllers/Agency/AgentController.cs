using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = AGENT.DISPLAY_NAME)]
    public class AgentController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ICaseVendorService vendorService;
        private readonly ILogger<AgentController> logger;

        public AgentController(INotyfService notifyService, ICaseVendorService vendorService, ILogger<AgentController> logger)
        {
            this.notifyService = notifyService;
            this.vendorService = vendorService;
            this.logger = logger;
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
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            return View();
        }

        [Breadcrumb("Submit", FromAction = "Agent")]
        public async Task<IActionResult> GetInvestigate(long selectedcase, bool uploaded = false)
        {
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be investigate.");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await vendorService.GetInvestigate(currentUserEmail, selectedcase, uploaded);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {UserEmail}.", HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Submitted")]
        public IActionResult Submitted()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            return View();
        }

        [Breadcrumb(title: " Detail", FromAction = "Submitted")]
        public async Task<IActionResult> SubmittedDetail(long id)
        {
            if (!ModelState.IsValid || id == 0)
            {
                notifyService.Error("NOT FOUND !!!..");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var model = await vendorService.GetInvestigatedForAgent(currentUserEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {UserEmail}.", HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}