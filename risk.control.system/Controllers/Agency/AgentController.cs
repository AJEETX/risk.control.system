using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Agent;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = AGENT.DISPLAY_NAME)]
    public class AgentController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IAgentCaseDetailService agentCaseDetailService;
        private readonly ILogger<AgentController> logger;

        public AgentController(INotyfService notifyService, IAgentCaseDetailService agentCaseDetailService, ILogger<AgentController> logger)
        {
            this.notifyService = notifyService;
            this.agentCaseDetailService = agentCaseDetailService;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Agent));
        }

        [Breadcrumb("Tasks")]
        public IActionResult Agent()
        {
            return View();
        }

        [Breadcrumb("Submit", FromAction = nameof(Agent))]
        public async Task<IActionResult> GetInvestigate(long selectedcase, bool uploaded = false)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be investigate.");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await agentCaseDetailService.GetInvestigate(userEmail, selectedcase, uploaded);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for case {Id}. {UserEmail}.", selectedcase, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Submitted")]
        public IActionResult Submitted()
        {
            return View();
        }

        [Breadcrumb(title: "Detail", FromAction = nameof(Submitted))]
        public async Task<IActionResult> SubmittedDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid || id == 0)
            {
                notifyService.Error("NOT FOUND !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await agentCaseDetailService.GetInvestigatedForAgent(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for case {Id}. {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}