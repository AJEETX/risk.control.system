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
    public class CustomerController(ILogger<CustomerController> logger,
            IErrorNotifyService errorNotifyService,
            IEncryptionService encryptionService,
            INavigationService navigationService,
            ICustomerService customerService,
            INotyfService notifyService) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly ILogger<CustomerController> _logger = logger;
        private readonly IErrorNotifyService _errorNotifyService = errorNotifyService;
        private readonly INavigationService _navigationService = navigationService;
        private readonly ICustomerService _customerService = customerService;
        private readonly INotyfService _notifyService = notifyService;

        [HttpGet]
        public async Task<IActionResult> Create([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState, "OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(AddAssignController.Create), ControllerName<AddAssignController>.Name);
                }
                var encryptedId = _encryptionService.ProtectId(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(encryptedId, "Add Customer", nameof(Create), ControllerName<CustomerController>.Name);

                var model = await _customerService.GetCreateViewModelAsync(id, userEmail);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error creating customer. Try Again");
                var encryptedId = _encryptionService.ProtectId(id);
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = encryptedId });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CustomerDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }

                var result = await _customerService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }

                _notifyService.Custom($"Customer <b>{model.Name}</b> added successfully", 3, "green", "fas fa-user-plus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                _notifyService.Error("Error creating customer.Try Again");
            }
            var encryptedId = _encryptionService.ProtectId(model.InvestigationTaskId!.Value);
            return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = encryptedId });
        }

        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(AddAssignController.Create), ControllerName<AddAssignController>.Name);
                }

                var model = await _customerService.GetEditViewModelAsync(id, userEmail);

                var encryptedId = _encryptionService.ProtectId(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(encryptedId, "Edit Customer", nameof(Edit), ControllerName<CustomerController>.Name); ;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error editing Customer.Try Again");
                var encryptedId = _encryptionService.ProtectId(id);
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, CustomerDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }
                var (Success, Errors) = await _customerService.EditAsync(userEmail, model);

                if (!Success)
                {
                    foreach (var error in Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _customerService.PrepareMetadataAsync(model, userEmail);
                    return View(model);
                }

                _notifyService.Custom($"Customer <b>{model.Name}</b> edited successfully", 3, "orange", "fas fa-user-plus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing customer {Id}. {UserEmail}", model.CustomerDetailId, userEmail);
                _notifyService.Error("Error edting customer. Try again.");
            }
            var encryptedId = _encryptionService.ProtectId(id);
            return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = encryptedId });
        }
    }
}