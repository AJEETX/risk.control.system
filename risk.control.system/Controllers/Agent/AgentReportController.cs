using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;

namespace risk.control.system.Controllers.Agent
{
    [Authorize(Roles = $"{AGENT.DISPLAY_NAME}")]
    public class AgentReportController : Controller
    {
        private readonly IAgentAnswerService answerService;
        private readonly IMediaIdfyService mediaIdfyService;
        private readonly INotyfService notifyService;
        private readonly IAgentSubmitCaseService agentService;

        public AgentReportController(IAgentAnswerService answerService,
            IMediaIdfyService mediaIdfyService,
            INotyfService notifyService,
            IAgentSubmitCaseService agentService)
        {
            this.answerService = answerService;
            this.mediaIdfyService = mediaIdfyService;
            this.notifyService = notifyService;
            this.agentService = agentService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFaceImage(string reportName, string locationName, long locationId, long Id, string latitude, string longitude, long caseId, IFormFile Image, bool isAgent = false)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPs !!!.. Download error");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var userEmail = HttpContext.User.Identity.Name;
            if (Image != null && Image.Length > 0)
            {
                var response = await agentService.PostAgentId(userEmail, reportName, locationName, locationId, caseId, Id, latitude, longitude, isAgent, Image);
                return Json(new { success = true, image = response.Image });
            }
            return BadRequest("Invalid image.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDocumentImage(string reportName, string locationName, long locationId, long Id, string latitude, string longitude, long caseId, IFormFile Image)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPs !!!.. Download error");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var userEmail = HttpContext.User.Identity.Name;
            if (Image != null && Image.Length > 0)
            {
                var response = await agentService.PostDocumentId(userEmail, reportName, locationName, locationId, caseId, Id, latitude, longitude, Image);
                return Json(new { success = true, image = response.Image });
            }
            return BadRequest("Invalid image.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitMediaFile(long caseId, IFormFile Image, string latitude, string longitude, string reportName, string locationName)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPs !!!.. Download error");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            if (Image == null || Image.Length == 0)
                return Json(new { success = false, message = "No file provided." });
            var userEmail = HttpContext.User.Identity.Name;
            var extension = Path.GetExtension(Image.FileName).ToLower();

            var supportedExtensions = new[] { ".mp4", ".webm", ".mov", ".mp3", ".wav", ".aac" };
            if (!supportedExtensions.Contains(extension))
                return Json(new { success = false, message = "Unsupported media format." });
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

            var data = new DocumentData
            {
                LocationName = locationName,
                ReportName = reportName,
                Email = userEmail,
                CaseId = caseId,
                Image = Image,
                LocationLatLong = locationLongLat
            };
            var response = await mediaIdfyService.CaptureMedia(data);
            return Json(new
            {
                success = true,
                extension = extension.TrimStart('.'),
                fileData = Convert.ToBase64String(response.Image)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLocationAnswers(string locationName, long CaseId, List<QuestionTemplate> Questions)
        {
            var userEmail = HttpContext.User.Identity.Name;
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPs !!!.. Download error");
                return this.RedirectToAction<DashboardController>(x => x.Index());
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
                // Re-load data and return view with error
                // e.g. return View(model);
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
            return Redirect("/Agent/GetInvestigate?selectedcase=" + CaseId);
        }
    }
}