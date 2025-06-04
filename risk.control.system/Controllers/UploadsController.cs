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
        private readonly IWebHostEnvironment webHostEnvironment;

        public UploadsController(ApplicationDbContext context,
            INotyfService notifyService,
            IClaimsAgentService agentService,
            IAgentIdService agentIdService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.notifyService = notifyService;
            this.agentService = agentService;
            this.agentIdService = agentIdService;
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
                var fileBytes = file.ByteData;
                return File(fileBytes, file.FileType, file.Name + file.Extension);
            }
            catch (Exception ex)
            {
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
                return BadRequest(new { success = false, message = "Error deleting file: " + ex.Message });
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
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
        [HttpGet]
        public async Task<IActionResult> GetFaceImage(long id)
        {
            var face = await _context.DigitalIdReport.FindAsync(id);
            if (face?.IdImage != null)
            {
                return File(face.IdImage, "image/jpeg"); // or "image/png" if PNG
            }
            return File("~/img/no-user.png", "image/jpeg");
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
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
        [HttpGet]
        public async Task<IActionResult> GetDocumentImage(long id)
        {
            var doc = await _context.DocumentIdReport.FindAsync(id);
            if (doc?.IdImage != null)
            {
                return File(doc.IdImage, "image/jpeg");
            }
            return File("~/img/no-user.png", "image/jpeg");
        }

        [HttpPost]
        public async Task<IActionResult> UploadMediaFile(long caseId, IFormFile Image, string latitude, string longitude, string reportName, string locationName)
        {
            if (Image == null || Image.Length == 0)
                return Json(new { success = false, message = "No file provided." });
            var currentUserEmail = HttpContext.User.Identity.Name;
            var extension = Path.GetExtension(Image.FileName).ToLower();

            var supportedExtensions = new[] { ".mp4", ".webm", ".mov", ".mp3", ".wav" };
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

        [HttpGet]
        public IActionResult GetMediaFile(long id)
        {
            var report = _context.MediaReport.Find(id);
            if (report == null || report.IdImage == null)
                return NotFound();

            var contentType = report.MediaExtension == "mp4" ? "video/mp4" :
                              report.MediaExtension == "mp3" ? "audio/mpeg" :
                              "application/octet-stream";

            return File(report.IdImage, contentType);
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