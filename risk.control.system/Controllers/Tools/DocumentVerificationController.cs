using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;


namespace risk.control.system.Controllers.Tools
{
    public class DocumentVerificationController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IImageAnalysisService imageAnalysisService;

        public DocumentVerificationController(UserManager<ApplicationUser> userManager, IImageAnalysisService imageAnalysisService)
        {
            this.userManager = userManager;
            this.imageAnalysisService = imageAnalysisService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);
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
            if (file == null || file.Length == 0) return RedirectToAction("Index");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                if (user.DocumentAnalysisCount >= 5)
                {
                    return StatusCode(403, new { message = "Document Analysis limit reached (5/5)" });
                }

                var model = await imageAnalysisService.AnalyzeImageAsync(file);

                user.DocumentAnalysisCount++;
                await userManager.UpdateAsync(user);

                return View("Index", model);
            }
            catch (Exception)
            {
                // Log exception here
                return StatusCode(500, "Internal server error during processing.");
            }
        }
    }
}
