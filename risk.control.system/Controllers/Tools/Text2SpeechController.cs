using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;

using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Tools
{
    public class Text2SpeechController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IText2SpeechService text2SpeechService;

        public Text2SpeechController(IWebHostEnvironment env, UserManager<ApplicationUser> userManager, IText2SpeechService text2SpeechService)
        {
            _env = env;
            this.userManager = userManager;
            this.text2SpeechService = text2SpeechService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
            }
            var model = new Text2SpeechData
            {
                RemainingTries = 5 - (user?.Text2SpeechCount ?? 0)
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSpeech(Text2SpeechData input)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // 1. Safety Check
            if (user.Text2SpeechCount >= 5)
            {
                ModelState.AddModelError("", "Usage limit exceeded.");
                return View("Index", new Text2SpeechData { RemainingTries = 0 });
            }

            if (string.IsNullOrWhiteSpace(input.TextData))
            {
                return RedirectToAction(nameof(Index));
            }

            // 2. Logic to Generate Audio (Placeholder)
            // Replace this with your actual TTS Service logic
            byte[] generatedAudio = await text2SpeechService.Convert(input.TextData);

            // 3. Update User Count
            user.Text2SpeechCount++;
            await userManager.UpdateAsync(user);

            // 4. Return the View with Audio data
            var viewModel = new Text2SpeechData
            {
                TextData = input.TextData,
                TextOutputAudio = generatedAudio,
                RemainingTries = 5 - user.Text2SpeechCount
            };

            return View("Index", viewModel);
        }
    }
}
