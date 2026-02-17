using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CaseCreateEditController : Controller
    {
        private readonly ILogger<CaseCreateEditController> _logger;
        private readonly ICaseCreateEditService _caseCreateEditService;
        private readonly INavigationService _navigationService;
        private readonly INotyfService _notifyService;

        public CaseCreateEditController(ILogger<CaseCreateEditController> logger,
            ICaseCreateEditService createCreateEditService,
            INavigationService navigationService,
            INotyfService notifyService)
        {
            _logger = logger;
            _caseCreateEditService = createCreateEditService;
            _navigationService = navigationService;
            _notifyService = notifyService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(New));
        }

        [Breadcrumb("Add/Assign")]
        public async Task<IActionResult> New()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var state = await _caseCreateEditService.GetCreationStateAsync(currentUserEmail);

                if (state.IsTrial)
                {
                    if (!state.UserCanCreate)
                        _notifyService.Warning($"MAX Case limit = <b>{state.MaxAllowed}</b> reached");
                    else
                        _notifyService.Information($"Limit available = <b>{state.AvailableCount}</b>");
                }

                return View(new CreateClaims
                {
                    UserCanCreate = state.UserCanCreate,
                    HasClaims = state.HasClaims,
                    FileSampleIdentifier = state.FileSampleIdentifier,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Add/Assign page for {UserEmail}", currentUserEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [Breadcrumb("Add New", FromAction = nameof(New))]
        public async Task<IActionResult> Add()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var model = await _caseCreateEditService.Create(userEmail);
                if (model.Trial)
                {
                    if (!model.AllowedToCreate)
                    {
                        _notifyService.Information($"MAX Case limit = <b>{model.TotalCount}</b> reached");
                    }
                    else
                    {
                        _notifyService.Information($"Limit available = <b>{model.AvailableCount}</b>");
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred creating case. {UserEmail}", userEmail);
                _notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(Create));
            }
        }

        [Breadcrumb(title: "Add Case", FromAction = nameof(New))]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var model = await _caseCreateEditService.GetCreateViewModelAsync(userEmail);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred to create case. {UserEmail} ", userEmail);
                _notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(Add));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCaseViewModel model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    ShowErrorNotification();
                    await _caseCreateEditService.LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                var result = await _caseCreateEditService.CreateAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        var isDtoProperty = typeof(PolicyDetailDto).GetProperty(error.Key) != null;

                        string stateKey = isDtoProperty
                            ? $"{nameof(model.PolicyDetailDto)}.{error.Key}"
                            : error.Key;

                        ModelState.AddModelError(stateKey, error.Value);
                    }
                    ShowErrorNotification();

                    await _caseCreateEditService.LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                _notifyService.Success($"Policy #{result.CaseId} created successfully");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = result.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred to create case. {UserEmail} ", userEmail);
                _notifyService.Error("Error creating case detail. Try Again.");
                return RedirectToAction(nameof(Add));
            }
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 0)
                {
                    _notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(New), "CaseCreateEdit");
                }

                var model = await _caseCreateEditService.GetEditPolicyDetail(id);
                if (model == null)
                {
                    _notifyService.Error("Case Not Found!!!");
                    return RedirectToAction(nameof(Add));
                }
                await _caseCreateEditService.LoadDropDowns(model.PolicyDetailDto, userEmail);

                ViewData["BreadcrumbNode"] = _navigationService.GetEditCasePath(id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred to get edit case {Id}. {UserEmail} ", id, userEmail);
                _notifyService.Error("Error editing case detail. Try Again");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = id });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(EditPolicyDto model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    ShowErrorNotification();
                    await _caseCreateEditService.LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                var result = await _caseCreateEditService.EditAsync(userEmail, model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        var isDtoProperty = typeof(PolicyDetailDto).GetProperty(error.Key) != null;

                        string stateKey = isDtoProperty
                            ? $"{nameof(model.PolicyDetailDto)}.{error.Key}"
                            : error.Key;

                        ModelState.AddModelError(stateKey, error.Value);
                    }
                    ShowErrorNotification();

                    await _caseCreateEditService.LoadDropDowns(model.PolicyDetailDto, userEmail);
                    return View(model);
                }
                _notifyService.Custom($"Policy <b>#{result.CaseId}</b> edited successfully", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred to edit case {Id}. {UserEmail} ", model.Id, userEmail);
                _notifyService.Error("OOPs !!!..Error editing Case detail. Try again.");
                return RedirectToAction(nameof(CreatorController.Details), ControllerName<CreatorController>.Name, new { id = model.Id });
            }
        }

        private void ShowErrorNotification()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct();
            _notifyService.Error($"<b>Please fix:</b><br/>{string.Join("<br/>", errors)}");
        }
    }
}