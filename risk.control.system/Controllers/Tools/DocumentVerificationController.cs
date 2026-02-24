using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class DocumentVerificationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DocumentVerificationController> logger;
        private readonly IImageAnalysisService imageAnalysisService;

        public DocumentVerificationController(UserManager<ApplicationUser> userManager, ILogger<DocumentVerificationController> logger, IImageAnalysisService imageAnalysisService)
        {
            this._userManager = userManager;
            this.logger = logger;
            this.imageAnalysisService = imageAnalysisService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
            }
            var model = new ImageAnalysisViewModel
            {
                RemainingTries = 5 - (user?.DocumentAnalysisCount ?? 0)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (user.DocumentAnalysisCount >= 5)
            {
                return StatusCode(403, new { message = "Document Analysis limit reached (5/5)" });
            }
            try
            {
                var model = await imageAnalysisService.AnalyzeImageAsync(file);

                user.DocumentAnalysisCount++;
                await _userManager.UpdateAsync(user);

                return View("Index", model);
            }
            catch (Exception)
            {
                logger.LogError("Error processing document verification. {UserId}", _userManager.GetUserId(User) ?? "Anonymous");
                var model = new ImageAnalysisViewModel
                {
                    OriginalImageUrl = $"/uploads/{file.FileName}",
                    ElaImageUrl = $"/uploads/ela_{file.FileName}",
                    MetadataFlagged = true,
                    ElaScore = Math.Round(0.0, 2),
                    RemainingTries = 5 - user.DocumentAnalysisCount,
                };
                return View("Index", model);
            }
        }
    }
}