using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;

namespace risk.control.system.Controllers.Agent
{
    [Authorize(Roles = $"{AGENT.DISPLAY_NAME}")]
    public class AgentReportController : Controller
    {
        private readonly IAgentAnswerService answerService;
        private readonly IDocumentIdfyService documentIdfyService;
        private readonly IFaceIdfyService agentIdService;
        private readonly IAgentFaceIdfyService agentFaceIdfyService;
        private readonly IMediaIdfyService mediaIdfyService;
        private readonly INotyfService notifyService;

        public AgentReportController(IAgentAnswerService answerService,
            IDocumentIdfyService documentIdfyService,
            IFaceIdfyService agentIdService,
            IAgentFaceIdfyService agentFaceIdfyService,
            IMediaIdfyService mediaIdfyService,
            INotyfService notifyService)
        {
            this.answerService = answerService;
            this.documentIdfyService = documentIdfyService;
            this.agentIdService = agentIdService;
            this.agentFaceIdfyService = agentFaceIdfyService;
            this.mediaIdfyService = mediaIdfyService;
            this.notifyService = notifyService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFaceImage(FaceData model, bool isAgent)
        {
            model.Email = HttpContext?.User?.Identity?.Name!;
            ModelState.Remove(nameof(model.Email));
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid Data.");
            }
            if (model.Image == null || model.Image.Length == 0)
                return Json(new { success = false, message = "No file provided." });

            if (isAgent)
            {
                var response = await agentFaceIdfyService.CaptureAgentId(model);
                return Json(new { success = true, image = response.Image });
            }
            else
            {
                var response = await agentIdService.CaptureFaceId(model);
                return Json(new { success = true, image = response.Image });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDocumentImage(DocumentData model)
        {
            model.Email = HttpContext?.User?.Identity?.Name!;
            ModelState.Remove(nameof(model.Email));
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid image.");
            }
            if (model.Image == null || model.Image.Length == 0)
                return Json(new { success = false, message = "No file provided." });

            var result = await documentIdfyService.CaptureDocumentId(model);
            return Json(new { success = true, image = result.Image });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitMediaFile(DocumentData model)
        {
            var userEmail = HttpContext?.User?.Identity?.Name!;
            model.Email = userEmail;
            ModelState.Remove(nameof(model.Email));
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }
            if (model.Image == null || model.Image.Length == 0)
                return Json(new { success = false, message = "No file provided." });
            var extension = Path.GetExtension(model.Image.FileName).ToLower();

            var supportedExtensions = new[] { ".mp4", ".webm", ".mov", ".mp3", ".wav", ".aac" };
            if (!supportedExtensions.Contains(extension))
                return Json(new { success = false, message = "Unsupported media format." });

            var response = await mediaIdfyService.CaptureMedia(model);
            return Json(new
            {
                success = true,
                extension = extension.TrimStart('.'),
                fileData = Convert.ToBase64String(response.Image!)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLocationAnswers(string locationName, long CaseId, List<QuestionTemplate> Questions)
        {
            var userEmail = HttpContext?.User?.Identity?.Name!;
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }
            foreach (var question in Questions)
            {
                if (question.IsRequired && string.IsNullOrEmpty(question.AnswerText))
                {
                    ModelState.AddModelError("", $"Answer required for: {question.QuestionText}");
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Some answers are missing.");
            }
            var submitted = await answerService.CaptureAnswers(userEmail, locationName, CaseId, Questions);

            if (submitted)
            {
                notifyService.Success("Answer(s) submitted Successfully.");
            }
            else
            {
                notifyService.Error("Error in submitting Answer(s).");
            }
            return Redirect("/Agent/GetInvestigate/" + CaseId);
        }
    }
}