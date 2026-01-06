using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class FaceMatchController : Controller
    {
        private readonly IAmazonApiService amazonService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FaceMatchController(IAmazonApiService amazonService, UserManager<ApplicationUser> userManager)
        {
            this.amazonService = amazonService;
            this._userManager = userManager; // Inject UserManager
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
            }
            var model = new FaceMatchData
            {
                RemainingTries = 5 - (user?.FaceMatchCount ?? 0)
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Compare(FaceMatchData data)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // 1. Check Usage Limit before calling AWS
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized("Unauthorized");
                }

                if (user.FaceMatchCount >= 5)
                {
                    return StatusCode(403, new { message = "Face Match limit reached (5/5)" });
                }

                // 2. Perform Biometric Comparison
                var originalFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.OriginalFaceImage);
                var secondayFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.MatchFaceImage);
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "Biometric service is currently unavailable." });
            }
        }
    }
}
