using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorsController : Controller
    {
        private readonly IManageAgencyService _agencyCreateEditService;
        private readonly INotyfService _notifyService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly ILogger<VendorsController> _logger;
        private readonly string _baseurl;

        public VendorsController(
            IManageAgencyService agencyCreateEditService,
            INotyfService notifyService,
            IErrorNotifyService errorNotifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<VendorsController> logger)
        {
            _agencyCreateEditService = agencyCreateEditService;
            _notifyService = notifyService;
            _errorNotifyService = errorNotifyService;
            _logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseurl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [Breadcrumb("Manage Agency")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Create));
        }

        [Breadcrumb(" Add Agency")]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendor = await _agencyCreateEditService.GetVendorForEditAsync(userEmail);
                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create Agency for {UserEmail}.", userEmail);
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor model, string domainAddress)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);

                    await _agencyCreateEditService.LoadModel(model);
                    return View(model);
                }
                if (!RegexHelper.IsMatch(domainAddress))
                {
                    ModelState.AddModelError("Email", "Invalid email address.");
                    _errorNotifyService.ShowErrorNotification(ModelState);

                    await _agencyCreateEditService.LoadModel(model);
                    return View(model);
                }

                var result = await _agencyCreateEditService.CreateAsync(domainAddress, userEmail, model, _baseurl);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    _errorNotifyService.ShowErrorNotification(ModelState);

                    _notifyService.Error("Please fix validation errors");
                    await _agencyCreateEditService.LoadModel(model);
                    return View(model);
                }
                _notifyService.Custom($"Agency <b>{model.Email}</b>  created successfully.", 3, "green", "fas fa-building");
                return RedirectToAction(nameof(AvailableAgencyController.Agencies), "AvailableAgency");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user. {UserEmail}.", userEmail);
                _notifyService.Error("OOPS !!!..Error creating Agency. Try again.");
                return RedirectToAction(nameof(Create));
            }
        }
    }
}