using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class OcrController(
        ILogger<OcrController> logger,
        INotyfService notifyService,
        IGoogleService googleService,
        IFileStorageService fileStorageService,
        UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<OcrController> _logger = logger;
        private readonly IGoogleService _googleService = googleService;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager = userManager; // Add this

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }
            var model = new DocumentOcrData
            {
                RemainingTries = 5 - (user?.OcrCount ?? 0)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OcrDocument(DocumentOcrData data)
        {
            var validationError = await ValidateOcrRequest(data);
            if (validationError != null) return validationError;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }
            try
            {
                var result = await ProcessOcrAsync(user, data.DocumentImage!);

                return Ok(new
                {
                    description = result,
                    remaining = 5 - user.OcrCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR Error for user {UserId}", user.Id);
                return Ok(new
                {
                    description = "Error processing document",
                    remaining = 5 - user.OcrCount
                });
            }
        }

        private async Task<IActionResult?> ValidateOcrRequest(DocumentOcrData data)
        {
            if (data.DocumentImage == null || data.DocumentImage.Length == 0)
                return BadRequest("Please upload a valid image file.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }

            if (user.OcrCount >= 5)
                return StatusCode(403, "OCR usage limit reached (5/5).");

            return null;
        }

        private async Task<string> ProcessOcrAsync(ApplicationUser user, IFormFile image)
        {
            var (file, path) = await _fileStorageService.SaveAsync(image, "tool");
            var ocrData = await _googleService.DetectTextAsync(path);

            if (ocrData == null || !ocrData.Any())
                throw new Exception("Ocr failed to detect text.");

            user.OcrCount++;
            await _userManager.UpdateAsync(user);

            return ocrData.FirstOrDefault()?.Description ?? string.Empty;
        }
    }
}