using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class FaceMatchController : Controller
    {
        private readonly IAmazonApiService amazonService;

        public FaceMatchController(IAmazonApiService amazonService)
        {
            this.amazonService = amazonService;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Compare(FaceMatchData data)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var originalFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.OriginalFaceImage);
                var secondayFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.MatchFaceImage);

                var faceMatchData = await amazonService.CompareFaceMatch(originalFace, secondayFace);

                // Handle case where no faces are detected in one of the images
                if (faceMatchData.FaceMatches.Count == 0)
                {
                    return Ok(new { match = false, similarity = 0.0 });
                }

                var result = faceMatchData.FaceMatches.Count >= 1;
                var similarity = faceMatchData.FaceMatches[0].Similarity;

                return Ok(new { match = result, similarity = similarity });
            }
            catch (Exception)
            {
                // Log the error (e.g. AWS credentials issues or invalid image formats)
                return StatusCode(500, new { message = "Biometric service is currently unavailable." });
            }
        }
    }
}
