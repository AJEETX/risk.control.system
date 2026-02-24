using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Common;
using risk.control.system.Services.Manager;
using risk.control.system.Services.Report;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = MANAGER.DISPLAY_NAME)]
    public class ManagerController : Controller
    {
        private readonly INotyfService _notifyService;
        private readonly IInvoiceService _invoiceService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<ManagerController> _logger;
        private readonly ICaseDetailService _caseDetailService;

        public ManagerController(INotyfService notifyService,
            IInvoiceService invoiceService,
            INavigationService navigationService,
            ILogger<ManagerController> logger,
            ICaseDetailService caseDetailService)
        {
            _notifyService = notifyService;
            _invoiceService = invoiceService;
            _navigationService = navigationService;
            _logger = logger;
            _caseDetailService = caseDetailService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Assessor));
        }

        [Breadcrumb(title: nameof(Active))]
        public IActionResult Active()
        {
            return View();
        }

        [Breadcrumb(title: "Details", FromAction = nameof(Active))]
        public async Task<IActionResult> ActiveDetail(long id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                _notifyService.Error("Case not found.");
                return RedirectToAction(nameof(Active));
            }

            var userEmail = User.Identity?.Name;

            try
            {
                var model = await _caseDetailService.GetClaimDetailsReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active case {CaseId}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting case detail. Please try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: nameof(Approved))]
        public IActionResult Approved()
        {
            return View();
        }

        [Breadcrumb(title: "Details", FromAction = nameof(Approved))]
        public async Task<IActionResult> ApprovedDetail(long id)
        {
            if (!ModelState.IsValid || id < 1)
            {
                _notifyService.Error("Case Not Found !!!..");
                return RedirectToAction(nameof(Approved));
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var model = await _caseDetailService.GetClaimDetailsReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approved case detail.User: {UserEmail}, CaseId: {CaseId}",
                    userEmail,
                    id);
                _notifyService.Error("Error getting case detail. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: nameof(Rejected))]
        public IActionResult Rejected()
        {
            return View();
        }

        [Breadcrumb("Details", FromAction = nameof(Rejected))]
        public async Task<IActionResult> RejectDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id < 1)
            {
                _notifyService.Error("Case Not Found !!!..");
                return RedirectToAction(nameof(Rejected));
            }
            try
            {
                var model = await _caseDetailService.GetClaimDetailsReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rejected case detail.User: {UserEmail}, CaseId: {CaseId}",
                    userEmail,
                    id);
                _notifyService.Error("Error getting case detail. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> ShowInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id < 1)
            {
                _notifyService.Error("Case Not Found !!!..");
                return RedirectToAction(nameof(Approved));
            }
            try
            {
                var invoice = await _invoiceService.GetInvoice(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetInvoiceBreadcrumb(id, invoice.CaseId.Value, "Manager", "Manager", "Cases", "Approved", "Approved", "ApprovedDetail");

                return View(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice case detail.User: {UserEmail}, CaseId: {CaseId}",
                   userEmail,
                   id);
                _notifyService.Error("Error getting invoice detail. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> PrintInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id <= 0)
            {
                _notifyService.Error("Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            try
            {
                var invoice = await _invoiceService.GetInvoice(id);

                return View(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printing invoice case detail.User: {UserEmail}, CaseId: {CaseId}",
                   userEmail,
                   id);
                _notifyService.Error("Error printing invoice. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}