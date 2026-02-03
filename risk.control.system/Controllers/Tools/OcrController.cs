using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.AppConstant;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class OcrController : Controller
    {
        private readonly IGoogleService googleService;
        private readonly IFileStorageService fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager; // Add this

        public OcrController(
            IGoogleService googleService,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> userManager) // Inject this
        {
            this.googleService = googleService;
            this.fileStorageService = fileStorageService;
            this._userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
            }
            var model = new DocumentOcrData
            {
                RemainingTries = 5 - (user?.OcrCount ?? 0)
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> OcrDocument(DocumentOcrData data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Get current user and check limit
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
            }

            if (user.OcrCount >= 5)
            {
                return StatusCode(403, "OCR usage limit reached (5/5).");
            }

            // 2. Perform the OCR Task
            var (file, path) = await fileStorageService.SaveAsync(data.DocumentImage, "tool");
            var ocrData = await googleService.DetectTextAsync(path);

            if (ocrData == null || ocrData.Count == 0)
            {
                return BadRequest("Ocr failed to detect text.");
            }

            // 3. Increment the count and save to Database
            user.OcrCount++;
            await _userManager.UpdateAsync(user);

            var ocrDetail = ocrData.FirstOrDefault();
            var description = ocrDetail != null ? ocrDetail.Description : string.Empty;

            // 4. Return description (and optionally remaining count)
            return Ok(new
            {
                description = description,
                remaining = 5 - user.OcrCount // Send remaining count back to JS
            });
        }
    }
}