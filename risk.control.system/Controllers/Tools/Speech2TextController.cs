using System.Net;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Amazon.Rekognition.Model;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Tools
{
    public class Speech2TextController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ISpeech2TextService speech2TextService;

        public Speech2TextController(IWebHostEnvironment env, UserManager<ApplicationUser> userManager, ISpeech2TextService speech2TextService)
        {
            _env = env;
            this.userManager = userManager;
            this.speech2TextService = speech2TextService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
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
            var user = await userManager.GetUserAsync(User);
        if (user == null || user.Speech2TextCount >= 5) return RedirectToAction("Index");

        if (input.SpeechInputData == null || input.SpeechInputData.Length == 0)
        {
            ModelState.AddModelError("", "Please upload a valid audio file.");
            return View("Index", new Speech2TextData { RemainingTries = 5 - user.Speech2TextCount });
        }

            var transcribedText = await speech2TextService.ConvertSpeech(input);
            // 6. Update User Usage
            user.Speech2TextCount++;
            await userManager.UpdateAsync(user);

            var model = new Speech2TextData
        {
            TextData = transcribedText,
            RemainingTries = 5 - user.Speech2TextCount
        };

        return View("Index", model);
    }
    }
}
