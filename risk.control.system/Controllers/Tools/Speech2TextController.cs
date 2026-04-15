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
    public class Speech2TextController(ILogger<Speech2TextController> logger, INotyfService notifyService, UserManager<ApplicationUser> userManager, ISpeech2TextService speech2TextService) : Controller
    {
        private readonly ILogger<Speech2TextController> _logger = logger;
        private readonly INotyfService _notifyService = notifyService;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ISpeech2TextService _speech2TextService = speech2TextService;

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _notifyService.Error("User UnAuthorized");
                return RedirectToAction(nameof(ToolsController.Try), ControllerName<ToolsController>.Name);
            }
            var model = new Speech2TextData
            {
                RemainingTries = 5 - (user?.Speech2TextCount ?? 0)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertSpeech(Speech2TextData input)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Speech2TextCount >= 5) return RedirectToAction("Index");
            if (input.SpeechInputData == null || input.SpeechInputData.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid audio file.");
                return View("Index", new Speech2TextData { RemainingTries = 5 - user.Speech2TextCount });
            }
            try
            {
                var transcribedText = await _speech2TextService.ConvertSpeech(input);
                // 6. Update User Usage
                user.Speech2TextCount++;
                await _userManager.UpdateAsync(user);

                var model = new Speech2TextData
                {
                    TextData = transcribedText,
                    RemainingTries = 5 - user.Speech2TextCount
                };

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during speech-to-text conversion. {UserEmail}", user.Email);
                var model = new Speech2TextData
                {
                    TextData = "An error occurred while processing your request. Please try again later.",
                    RemainingTries = 5 - user.Speech2TextCount
                };
                return View("Index", model);
            }
        }
    }
}