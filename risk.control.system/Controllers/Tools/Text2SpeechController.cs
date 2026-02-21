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
    public class Text2SpeechController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<Text2SpeechController> _logger;
        private readonly IText2SpeechService _text2SpeechService;

        public Text2SpeechController(UserManager<ApplicationUser> userManager, ILogger<Text2SpeechController> logger, IText2SpeechService text2SpeechService)
        {
            this._userManager = userManager;
            this._logger = logger;
            this._text2SpeechService = text2SpeechService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
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

            var user = await _userManager.GetUserAsync(User);
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
            try
            {
                // 2. Logic to Generate Audio (Placeholder)
                // Replace this with your actual TTS Service logic
                byte[] generatedAudio = await _text2SpeechService.Convert(input.TextData);

                // 3. Update User Count
                user.Text2SpeechCount++;
                await _userManager.UpdateAsync(user);

                // 4. Return the View with Audio data
                var viewModel = new Text2SpeechData
                {
                    TextData = input.TextData,
                    TextOutputAudio = generatedAudio,
                    RemainingTries = 5 - user.Text2SpeechCount
                };

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text-to-speech conversion. {UserId}", user.Email ?? "Anonymous");
                var model = new Text2SpeechData
                {
                    TextData = "An error occurred while processing your request. Please try again later.",
                    RemainingTries = 5 - user.Text2SpeechCount
                };
                return View("Index", model);
            }
        }
    }
}