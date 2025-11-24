using System.Data;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;
namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class UploadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly IClaimsAgentService agentService;
        private readonly IAgentIdService agentIdService;
        private readonly ILogger<UploadsController> logger;
        private readonly IWebHostEnvironment webHostEnvironment;

        public UploadsController(ApplicationDbContext context,
            INotyfService notifyService,
            IClaimsAgentService agentService,
            IAgentIdService agentIdService,
            ILogger<UploadsController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.notifyService = notifyService;
            this.agentService = agentService;
            this.agentIdService = agentIdService;
            this.logger = logger;
            this.webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> DownloadLog(long id)
        {
            try
            {
                var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (file == null)
                {
                    notifyService.Error("OOPs !!!.. Download error");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                string zipPath = Path.Combine(webHostEnvironment.WebRootPath, "upload-file", file.Name);
                var fileBytes = System.IO.File.ReadAllBytes(zipPath);
                return File(fileBytes, "application/zip", Path.GetFileName(zipPath));
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        public async Task<IActionResult> DownloadErrorLog(long id)
        {
            try
            {
                var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (file == null)
                {
                    notifyService.Error("OOPs !!!.. Download error");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var fileBytes = file.ErrorByteData;
                var fileName = $"{file.Name}_UploadError_{id}.csv"; // Or use a timestamp

                return File(fileBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [HttpPost]
        public IActionResult DeleteLog(int id)
        {
            var file = _context.FilesOnFileSystem.FirstOrDefault(f => f.Id == id);
            if (file == null)
            {
                return NotFound(new { success = false, message = "File not found." });
            }

            try
            {
                if (System.IO.File.Exists(file.FilePath))
                {
                    System.IO.File.Delete(file.FilePath); // Delete the file from storage
                }
                file.Deleted = true; // Mark as deleted in the database
                _context.FilesOnFileSystem.Update(file); // Remove from database
                _context.SaveChanges();

                return Ok(new { success = true, message = "File deleted successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return BadRequest(new { success = false, message = "Error deleting file: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadFaceImage(string reportName, string locationName, long locationId, long Id, string latitude, string longitude, long caseId, IFormFile Image, bool isAgent = false)
        {
            var currentUserEmail = HttpContext.User.Identity.Name;
            if (Image != null && Image.Length > 0)
            {
                var response = await agentService.PostAgentId(currentUserEmail, reportName, locationName, locationId, caseId, Id, latitude, longitude, isAgent, Image);
                return Json(new { success = true, image = response.Image });
            }
            return BadRequest("Invalid image.");
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocumentImage(string reportName, string locationName, long locationId, long Id, string latitude, string longitude, long caseId, IFormFile Image)
        {
            var currentUserEmail = HttpContext.User.Identity.Name;
            if (Image != null && Image.Length > 0)
            {
                var response = await agentService.PostDocumentId(currentUserEmail, reportName, locationName, locationId, caseId, Id, latitude, longitude, Image);
                return Json(new { success = true, image = response.Image });
            }
            return BadRequest("Invalid image.");
        }

        [HttpPost]
        public async Task<IActionResult> UploadMediaFile(long caseId, IFormFile Image, string latitude, string longitude, string reportName, string locationName)
        {
            if (Image == null || Image.Length == 0)
                return Json(new { success = false, message = "No file provided." });
            var currentUserEmail = HttpContext.User.Identity.Name;
            var extension = Path.GetExtension(Image.FileName).ToLowerInvariant();

            var supportedExtensions = new[] { ".mp4", ".webm", ".mov", ".mp3", ".wav", ".aac" };
            if (!supportedExtensions.Contains(extension))
                return Json(new { success = false, message = "Unsupported media format." });
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

            var data = new DocumentData
            {
                LocationName = locationName,
                ReportName = reportName,
                Email = currentUserEmail,
                CaseId = caseId,
                Image = Image,
                LocationLatLong = locationLongLat
            };
            var response = await agentIdService.GetMedia(data);
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
            var submitted = await agentIdService.Answers(locationName, CaseId, Questions);

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