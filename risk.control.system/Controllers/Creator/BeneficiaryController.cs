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
    public class BeneficiaryController : Controller
    {
        private readonly ILogger<BeneficiaryController> _logger;
        private readonly IBeneficiaryService _beneficiaryService;
        private readonly INavigationService _navigationService;
        private readonly INotyfService _notifyService;

        public BeneficiaryController(ILogger<BeneficiaryController> logger,
            IBeneficiaryService beneficiaryService,
            INavigationService navigationService,
            INotyfService notifyService)
        {
            _logger = logger;
            _beneficiaryService = beneficiaryService;
            _navigationService = navigationService;
            _notifyService = notifyService;
        }

        public async Task<IActionResult> Create(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    ShowErrorNotification();
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }

                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(id, "Add Beneficiary", nameof(Create), ControllerName<BeneficiaryController>.Name);

                var model = await _beneficiaryService.GetViewModelAsync(id, userEmail);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error creating beneficiary. Try again.");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeneficiaryDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    ShowErrorNotification();
                    await _beneficiaryService.PrepareFailedPostModelAsync(model, userEmail);
                    return View(model);
                }
                var result = await _beneficiaryService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    ShowErrorNotification();
                    await _beneficiaryService.PrepareFailedPostModelAsync(model, userEmail);
                    return View(model);
                }
                _notifyService.Custom($"Beneficiary <b>{model.Name}</b> added successfully", 3, "green", "fas fa-user-tie");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Beneficiary for Case {Id}. {UserEmail}", model.InvestigationTaskId, userEmail);
                _notifyService.Warning("Error creating Beneficiary. Try again. ");
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
                var model = await _beneficiaryService.GetEditViewModelAsync(id, userEmail);

                ViewData["BreadcrumbNode"] = _navigationService.GetInvestigationPath(id, "Edit Beneficiary", nameof(Edit), ControllerName<BeneficiaryController>.Name);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Beneficiary {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error editing Beneficiary. Try again.");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BeneficiaryDetail model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    ShowErrorNotification();
                    await _beneficiaryService.PrepareFailedPostModelAsync(model, userEmail);
                    return View(model);
                }

                var result = await _beneficiaryService.EditAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    ShowErrorNotification();
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
            return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.InvestigationTaskId });
        }

        private void ShowErrorNotification()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct();
            _notifyService.Error($"<b>Please fix:</b><br/>{string.Join("<br/>", errors)}");
        }
    }
}