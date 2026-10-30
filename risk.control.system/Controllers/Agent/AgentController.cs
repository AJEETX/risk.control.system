using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Agent
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = AGENT.DISPLAY_NAME)]
    public class AgentController(INotyfService notifyService, IAgentCaseDetailService agentCaseDetailService, ILogger<AgentController> logger) : Controller
    {
        private readonly INotyfService _notifyService = notifyService;
        private readonly IAgentCaseDetailService _agentCaseDetailService = agentCaseDetailService;
        private readonly ILogger<AgentController> _logger = logger;

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
        public async Task<IActionResult> GetInvestigate([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, bool uploaded = false)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("No case selected!!!. Please select case to be investigate.");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
                }

                var model = await _agentCaseDetailService.GetInvestigate(userEmail, id, uploaded);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for case {Id}. {UserEmail}.", id, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
        }

        [Breadcrumb(title: "Submitted")]
        public IActionResult Submitted()
        {
            return View();
        }

        [Breadcrumb(title: "Detail", FromAction = nameof(Submitted))]
        public async Task<IActionResult> SubmittedDetail([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            if (!ModelState.IsValid || id == 0)
            {
                _notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
            try
            {
                var model = await _agentCaseDetailService.GetInvestigatedForAgent(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for case {Id}. {UserEmail}.", id, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
        }
    }
}