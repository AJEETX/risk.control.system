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
    public class BeneficiaryController(ILogger<BeneficiaryController> logger,
            IBeneficiaryService beneficiaryService,
            IEncryptionService encryptionService,
            IErrorNotifyService errorNotifyService,
            INavigationService navigationService,
            INotyfService notifyService) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly ILogger<BeneficiaryController> _logger = logger;
        private readonly IBeneficiaryService _beneficiaryService = beneficiaryService;
        private readonly IErrorNotifyService _errorNotifyService = errorNotifyService;
        private readonly INavigationService _navigationService = navigationService;
        private readonly INotyfService _notifyService = notifyService;

        public async Task<IActionResult> Create([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState, "OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }

                var encryptedId = _encryptionService.ProtectId(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(encryptedId, "Add Beneficiary", nameof(Create), ControllerName<BeneficiaryController>.Name);

                var model = await _beneficiaryService.GetViewModelAsync(id, userEmail);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error creating beneficiary. Try again.");
                var encryptedId = _encryptionService.ProtectId(id);
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = encryptedId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeneficiaryDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _beneficiaryService.PrepareFailedPostModelAsync(model, userEmail);
                    return View(model);
                }
                var result = await _beneficiaryService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _beneficiaryService.PrepareFailedPostModelAsync(model, userEmail);
                    return View(model);
                }
                _notifyService.Custom($"Beneficiary <b>{model.Name}</b> added successfully", 3, "green", "fas fa-user-tie");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Beneficiary for Case {Id}. {UserEmail}", model.InvestigationTaskId, userEmail);
                _notifyService.Warning("Error creating Beneficiary. Try again. ");
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
                var model = await _beneficiaryService.GetEditViewModelAsync(id, userEmail);

                var encryptedId = _encryptionService.ProtectId(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(encryptedId, "Edit Beneficiary", nameof(Edit), ControllerName<BeneficiaryController>.Name);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Beneficiary {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error editing Beneficiary. Try again.");
                var encryptedId = _encryptionService.ProtectId(id);
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = encryptedId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, BeneficiaryDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _beneficiaryService.PrepareFailedPostModelAsync(model, userEmail);
                    return View(model);
                }

                var (Success, Errors) = await _beneficiaryService.EditAsync(userEmail, model);

                if (!Success)
                {
                    foreach (var error in Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _beneficiaryService.PrepareFailedPostModelAsync(model, userEmail);
                    return View(model);
                }
                _notifyService.Custom($"Beneficiary <b>{model.Name}</b> edited successfully", 3, "orange", "fas fa-user-tie");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Beneficiary Case {Id}. {UserEmail}", model.InvestigationTaskId, userEmail);
                _notifyService.Error("Error editing Beneficiary. Try again.");
            }
            var encryptedId = _encryptionService.ProtectId(id);
            return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = encryptedId });
        }
    }
}