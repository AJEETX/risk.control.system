using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class FaceMatchController(
        IAmazonApiService amazonService,
        INotyfService notifyService,
        ILogger<FaceMatchController> logger,
        UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly IAmazonApiService amazonService = amazonService;
        private readonly ILogger<FaceMatchController> logger = logger;
        private readonly INotyfService _notifyService = notifyService;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }
            var model = new FaceMatchData
            {
                RemainingTries = 5 - (user?.FaceMatchCount ?? 0)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Compare(FaceMatchData data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Check Usage Limit before calling AWS
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }

            if (user.FaceMatchCount >= 5)
            {
                return StatusCode(403, new { message = "Face Match limit reached (5/5)" });
            }
            try
            {
                // 2. Perform Biometric Comparison
                var originalFace = await VerificationHelper.GetBytesFromIFormFile(data.OriginalFaceImage);
                var secondayFace = await VerificationHelper.GetBytesFromIFormFile(data.MatchFaceImage);
                var faceMatchData = await amazonService.CompareFaceMatch(originalFace, secondayFace);

                // 3. Increment Count in Database
                user.FaceMatchCount++;
                await _userManager.UpdateAsync(user);

                // 4. Handle Result
                if (faceMatchData.FaceMatches.Count == 0)
                {
                    return Ok(new { match = false, similarity = 0.0, remaining = 5 - user.FaceMatchCount });
                }

                var result = faceMatchData.FaceMatches.Count >= 1;
                var similarity = faceMatchData.FaceMatches[0].Similarity;

                return Ok(new
                {
                    match = result,
                    similarity = similarity,
                    remaining = 5 - user.FaceMatchCount // Send remaining count back to JS
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during face comparison. {UserId}", _userManager.GetUserId(User) ?? "Anonymous");

                return Ok(new
                {
                    match = false,
                    similarity = 0.0,
                    remaining = 5 - user.FaceMatchCount  // Send remaining count back to JS
                });
            }
        }
    }
}