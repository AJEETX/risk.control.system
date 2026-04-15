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
    public class DocumentVerificationController(
        UserManager<ApplicationUser> userManager,
        INotyfService notifyService,
        ILogger<DocumentVerificationController> logger,
        IImageAnalysisService imageAnalysisService) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ILogger<DocumentVerificationController> logger = logger;
        private readonly INotyfService _notifyService = notifyService;
        private readonly IImageAnalysisService imageAnalysisService = imageAnalysisService;

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }
            var model = new ImageAnalysisViewModel
            {
                RemainingTries = 5 - (user?.DocumentAnalysisCount ?? 0)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // 1. Formal Validation
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "A valid document file is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Or return View(Index) with errors
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }

            if (user.DocumentAnalysisCount >= 5)
            {
                return StatusCode(403, new { message = "Document Analysis limit reached (5/5)" });
            }
            try
            {
                var model = await imageAnalysisService.AnalyzeImageAsync(file!);

                user.DocumentAnalysisCount++;
                await _userManager.UpdateAsync(user);

                return View("Index", model);
            }
            catch (Exception)
            {
                logger.LogError("Error processing document verification. {UserId}", _userManager.GetUserId(User) ?? "Anonymous");
                var model = new ImageAnalysisViewModel
                {
                    OriginalImageUrl = $"/uploads/{Path.GetFileName(file!.FileName)}",
                    ElaImageUrl = $"/uploads/ela_{Path.GetFileName(file!.FileName)}",
                    MetadataFlagged = true,
                    ElaScore = Math.Round(0.0, 2),
                    RemainingTries = 5 - user.DocumentAnalysisCount,
                };
                return View("Index", model);
            }
        }
    }
}