using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class ItrController(
        UserManager<ApplicationUser> userManager,
        INotyfService notifyService,
        ILogger<ItrController> logger,
        IItrVerificationService itrVerificationService) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<ItrController> _logger = logger;
        private readonly IItrVerificationService _itrVerificationService = itrVerificationService;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User Unauthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }

            var model = new ItrViewModel
            {
                RemainingTries = 5 - (user?.ItrVerificationCount ?? 0)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(IFormFile file)
        {
            var model = new ItrViewModel
            {
                PdfFile = file
            };
            model.IsProcessed = false;
            model.ErrorMessage = string.Empty;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User Unauthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }

            // Sync limit state layout
            model.RemainingTries = 5 - (user?.ItrVerificationCount ?? 0);

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (model.PdfFile == null || !model.PdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("PdfFile", "Please upload a valid document format with a .pdf extension.");
                return View("Index", model);
            }

            if (user!.ItrVerificationCount >= 5)
            {
                _notifyService.Error("Your tracking validation limit has been reached.");
                model.ErrorMessage = "Document Analysis limit reached (5/5)";
                return View("Index", model);
            }

            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await model.PdfFile.CopyToAsync(stream);
                }

                // Call your internal cryptographic and tool metadata analysis service
                model.IsSignatureValid = _itrVerificationService.IsPdfSignatureValid(tempFilePath);
                model.IsMetadataClean = _itrVerificationService.CheckMetadataTampering(tempFilePath);
                model.IsProcessed = true;

                // Process application limit tracking incremental ticks
                user.ItrVerificationCount++;
                await _userManager.UpdateAsync(user);

                model.RemainingTries = 5 - user.ItrVerificationCount;
                _notifyService.Success("ITR verification process successfully executed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ITR verification for user {UserId}", user.Id);
                model.IsProcessed = true;
                model.ErrorMessage = $"Error processing security verification: {ex.Message}";
                model.RemainingTries = 5 - user.ItrVerificationCount;
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }

            return View("Index", model);
        }
    }
}