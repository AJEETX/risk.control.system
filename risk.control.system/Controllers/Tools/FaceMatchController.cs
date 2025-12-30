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
        public async Task<IActionResult> FaceMatch(FaceMatchData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var originalFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.OriginalFaceImage);
            var secondayFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.MatchFaceImage);
            var faceMatchData = await amazonService.CompareFaceMatch(originalFace, secondayFace);
            var result = faceMatchData.FaceMatches.Count >= 1;
            var similarity = faceMatchData.FaceMatches[0].Similarity;
            return Ok(new { match = result, similarity = similarity });
        }
    }
}
