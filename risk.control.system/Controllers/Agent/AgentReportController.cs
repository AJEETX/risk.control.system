using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Agent
{
    [Authorize(Roles = $"{AGENT.DISPLAY_NAME}")]
    public class AgentReportController(IAgentAnswerService answerService,
            IDocumentIdfyService documentIdfyService,
            IEncryptionService encryptionService,
            IFaceIdfyService agentIdService,
            IAgentFaceIdfyService agentFaceIdfyService,
            IMediaIdfyService mediaIdfyService,
            ILogger<AgentReportController> logger,
            INotyfService notifyService) : Controller
    {
        private readonly ILogger<AgentReportController> _logger = logger;
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly IAgentAnswerService _answerService = answerService;
        private readonly IDocumentIdfyService _documentIdfyService = documentIdfyService;
        private readonly IFaceIdfyService _agentIdService = agentIdService;
        private readonly IAgentFaceIdfyService _agentFaceIdfyService = agentFaceIdfyService;
        private readonly IMediaIdfyService _mediaIdfyService = mediaIdfyService;
        private readonly INotyfService _notifyService = notifyService;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFaceImage(FaceData model, bool isAgent)
        {
            try
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
                    var response = await _agentFaceIdfyService.CaptureAgentId(model);
                    return Json(new { success = true, image = response.ByteImage });
                }
                else
                {
                    var response = await _agentIdService.CaptureFaceId(model);
                    return Json(new { success = true, image = response.ByteImage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred submitting face match");
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDocumentImage(DocumentData model)
        {
            try
            {
                model.Email = HttpContext?.User?.Identity?.Name!;
                ModelState.Remove(nameof(model.Email));
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid image.");
                }
                if (model.Image == null || model.Image.Length == 0)
                    return Json(new { success = false, message = "No file provided." });

                var result = await _documentIdfyService.CaptureDocumentId(model);
                return Json(new { success = true, image = result.ByteImage });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred  submitting document");
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitMediaFile(DocumentData model)
        {
            try
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

                var supportedExtensions = new[] { ".mp4", ".webm", ".mov", ".mp3", ".wav", ".aac", ".m4a" };
                if (!supportedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Unsupported media format." });

                var response = await _mediaIdfyService.CaptureMedia(model);
                return Json(new
                {
                    success = true,
                    extension = extension.TrimStart('.'),
                    fileData = Convert.ToBase64String(response.ByteImage!)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred  submitting media file");
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLocationAnswers(string locationName, long CaseId, List<QuestionTemplate> Questions)
        {
            try
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
                var submitted = await _answerService.CaptureAnswers(userEmail, locationName, CaseId, Questions);

                if (submitted)
                {
                    _notifyService.Success("Answer(s) submitted Successfully.");
                }
                else
                {
                    _notifyService.Error("Error in submitting Answer(s).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred submitting Answer(s).");
                _notifyService.Error("Error in submitting Answer(s).");
            }
            var encryptedId = _encryptionService.ProtectId(CaseId);
            return Redirect($"/Agent/GetInvestigate/{encryptedId}");
        }
    }
}