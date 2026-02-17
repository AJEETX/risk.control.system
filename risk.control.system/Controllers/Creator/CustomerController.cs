using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly INavigationService _navigationService;
        private readonly ICustomerService _customerService;
        private readonly INotyfService _notifyService;

        public CustomerController(ILogger<CustomerController> logger,
            INavigationService navigationService,
            ICustomerService customerService,
            INotyfService notifyService)
        {
            _logger = logger;
            _navigationService = navigationService;
            _customerService = customerService;
            _notifyService = notifyService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.Create), ControllerName<CaseCreateEditController>.Name);
                }
                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(id, "Add Customer", nameof(Create), ControllerName<CustomerController>.Name);

                var model = await _customerService.GetCreateViewModelAsync(id, userEmail);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error creating customer. Try Again");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CustomerDetail model)
        {
            var userEmail = HttpContext.User.Identity.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    ShowErrorNotification();
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }

                var result = await _customerService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    ShowErrorNotification();
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }

                _notifyService.Custom($"Customer <b>{model.Name}</b> added successfully", 3, "green", "fas fa-user-plus");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                _notifyService.Error("Error creating customer.Try Again");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.InvestigationTaskId });
            }
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(CaseCreateEditController.Create), ControllerName<CaseCreateEditController>.Name);
                }

                var model = await _customerService.GetEditViewModelAsync(id, userEmail);

                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(id, "Edit Customer", nameof(Edit), ControllerName<CustomerController>.Name); ;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error editing Customer.Try Again");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(CustomerDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    ShowErrorNotification();
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }
                var result = await _customerService.EditAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    ShowErrorNotification();
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }

                _notifyService.Custom($"Customer <b>{model.Name}</b> edited successfully", 3, "orange", "fas fa-user-plus");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                _notifyService.Error("Error edting customer. Try again.");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.InvestigationTaskId });
            }
        }

        private void ShowErrorNotification()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct();
            _notifyService.Error($"<b>Please fix:</b><br/>{string.Join("<br/>", errors)}");
        }
    }
}