using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using System.Data;
using AspNetCoreHero.ToastNotification.Abstractions;
using System.IO.Compression;
using risk.control.system.Controllers.Company;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class UploadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly IClaimsAgentService agentService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public UploadsController(ApplicationDbContext context,
            INotyfService notifyService,
            IClaimsAgentService agentService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.notifyService = notifyService;
            this.agentService = agentService;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgentUpload(long selectedcase, IFormFile agentImage, string agentIdLatitude, string agentIdLongitude, bool supervisorPhotoIdUpdate = false)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                if (string.IsNullOrWhiteSpace(currentUserEmail) ||
                    (agentImage == null) ||
                    selectedcase < 1 ||
                    string.IsNullOrWhiteSpace(agentIdLatitude) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(agentImage.FileName)) ||
                    string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(agentImage.FileName))) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(agentImage.Name)) ||
                    string.IsNullOrWhiteSpace(agentIdLongitude))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                using (var ds = new MemoryStream())
                {
                    agentImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await agentService.PostAgentId(currentUserEmail, selectedcase, agentIdLatitude, agentIdLongitude, imageByte);

                    notifyService.Custom($"Agent Image Uploaded", 3, "green", "fas fa-portrait");
                    if (supervisorPhotoIdUpdate)
                    {
                        return Redirect("/Supervisor/GetInvestigateReport?selectedcase=" + selectedcase);
                    }
                    else
                    {
                        return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedcase);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FaceUpload(long selectedcase, IFormFile digitalImage, string digitalIdLatitude, string digitalIdLongitude, bool supervisorPhotoIdUpdate = false)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                if (string.IsNullOrWhiteSpace(currentUserEmail) ||
                    (digitalImage == null) ||
                    selectedcase < 1 ||
                    string.IsNullOrWhiteSpace(digitalIdLatitude) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(digitalImage.FileName)) ||
                    string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(digitalImage.FileName))) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(digitalImage.Name)) ||
                    string.IsNullOrWhiteSpace(digitalIdLongitude))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                using (var ds = new MemoryStream())
                {
                    digitalImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await agentService.PostFaceId(currentUserEmail, selectedcase, digitalIdLatitude, digitalIdLongitude, imageByte);

                    notifyService.Custom($"Photo Image Uploaded", 3, "green", "fas fa-portrait");
                    if (supervisorPhotoIdUpdate)
                    {
                        return Redirect("/Supervisor/GetInvestigateReport?selectedcase=" + selectedcase);
                    }
                    else
                    {
                        return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedcase);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PanUpload(long selectedclaim, IFormFile panImage, string documentIdLatitude, string documentIdLongitude, bool supervisorPanUpdate = false)
        {
            try
            {

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                if (string.IsNullOrWhiteSpace(currentUserEmail) ||
                    (panImage == null) ||
                    selectedclaim < 1 ||
                    string.IsNullOrWhiteSpace(documentIdLatitude) ||
                    string.IsNullOrWhiteSpace(documentIdLongitude) ||
                    Path.GetInvalidFileNameChars() == null ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(panImage.FileName)) ||
                    string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(panImage.FileName))) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(panImage.Name))
                    )
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                using (var ds = new MemoryStream())
                {
                    panImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await agentService.PostDocumentId(currentUserEmail, selectedclaim, documentIdLatitude, documentIdLongitude, imageByte);

                    notifyService.Custom($"Pan card Image Uploaded", 3, "green", "fas fa-mobile-alt");
                    if (supervisorPanUpdate)
                    {
                        return Redirect("/Supervisor/GetInvestigateReport?selectedcase=" + selectedclaim);
                    }
                    else
                    {
                        return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        //[HttpPost]
        //[RequestSizeLimit(2_000_000)] // Checking for 2 MB
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> PassportUpload(string selectedclaim, IFormFile passportImage, string passportIdLatitude, string passportIdLongitude, bool supervisorPassportUpdate = false)
        //{
        //    try
        //    {
        //        var currentUserEmail = HttpContext.User?.Identity?.Name;
        //        if (currentUserEmail == null)
        //        {
        //            notifyService.Error("OOPs !!!..Unauthenticated Access");
        //            return RedirectToAction(nameof(Index), "Dashboard");
        //        }

        //        if (string.IsNullOrWhiteSpace(currentUserEmail) ||
        //            (passportImage == null) ||
        //            string.IsNullOrWhiteSpace(passportIdLatitude) ||
        //            string.IsNullOrWhiteSpace(passportIdLongitude) ||
        //            Path.GetInvalidFileNameChars() == null ||
        //            string.IsNullOrWhiteSpace(Path.GetFileName(passportImage.FileName)) ||
        //            string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(passportImage.FileName))) ||
        //            string.IsNullOrWhiteSpace(Path.GetFileName(passportImage.Name))
        //            )
        //        {
        //            notifyService.Error("OOPs !!!..Contact Admin");
        //            return RedirectToAction(nameof(Index), "Dashboard");
        //        }
        //        if (string.IsNullOrWhiteSpace(selectedclaim))
        //        {
        //            notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
        //            return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
        //        }

        //        using (var ds = new MemoryStream())
        //        {
        //            passportImage.CopyTo(ds);
        //            var imageByte = ds.ToArray();
        //            var response = await agentService.PostPassportId(currentUserEmail, selectedclaim, passportIdLatitude, passportIdLongitude, imageByte);

        //            notifyService.Custom($"Passport Image Uploaded", 3, "green", "fas fa-mobile-alt");
        //            if (supervisorPassportUpdate)
        //            {
        //                return Redirect("/Supervisor/GetInvestigateReport?selectedcase=" + selectedclaim);
        //            }
        //            else
        //            {
        //                return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        notifyService.Error("OOPs !!!..Contact Admin");
        //        return RedirectToAction(nameof(Index), "Dashboard");
        //    }
        //}

        //[HttpPost]
        //[RequestSizeLimit(5_000_000)] // Checking for 5 MB
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AudioUpload(string selectedclaim, IFormFile audioFile, string audioLatitude, string audioLongitude, bool supervisorAudioUpdate = false)
        //{
        //    try
        //    {
        //        var currentUserEmail = HttpContext.User?.Identity?.Name;
        //        if (currentUserEmail == null)
        //        {
        //            notifyService.Error("OOPs !!!..Unauthenticated Access");
        //            return RedirectToAction(nameof(Index), "Dashboard");
        //        }

        //        if (string.IsNullOrWhiteSpace(currentUserEmail) ||
        //            (audioFile == null) ||
        //            string.IsNullOrWhiteSpace(audioLatitude) ||
        //            string.IsNullOrWhiteSpace(audioLongitude) ||
        //            Path.GetInvalidFileNameChars() == null ||
        //            string.IsNullOrWhiteSpace(Path.GetFileName(audioFile.FileName)) ||
        //            string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(audioFile.FileName))) ||
        //            string.IsNullOrWhiteSpace(Path.GetFileName(audioFile.Name))
        //            )
        //        {
        //            notifyService.Error("OOPs !!!..Contact Admin");
        //            return RedirectToAction(nameof(Index), "Dashboard");
        //        }
        //        if (string.IsNullOrWhiteSpace(selectedclaim))
        //        {
        //            notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
        //            if (supervisorAudioUpdate)
        //            {
        //                return Redirect("/Supervisor/GetInvestigateReport?selectedcase=" + selectedclaim);
        //            }
        //            else
        //            {
        //                return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
        //            }
        //        }

        //        using (var ds = new MemoryStream())
        //        {
        //            audioFile.CopyTo(ds);
        //            var imageByte = ds.ToArray();
        //            var response = await agentService.PostAudio(currentUserEmail, selectedclaim, audioLatitude, audioLongitude, Path.GetFileName(audioFile.FileName), imageByte);

        //            notifyService.Custom($"Audio Uploaded", 3, "green", "fas fa-mobile-alt");
        //            return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        notifyService.Error("OOPs !!!..Contact Admin");
        //        return RedirectToAction(nameof(Index), "Dashboard");
        //    }
        //}

        //public IActionResult GetAudioFile(string fileName)
        //{
        //    var currentUserEmail = HttpContext.User?.Identity?.Name;
        //    if (currentUserEmail == null)
        //    {
        //        notifyService.Error("OOPs !!!..Unauthenticated Access");
        //        return RedirectToAction(nameof(Index), "Dashboard");
        //    }

        //    var filePath = Path.Combine("wwwroot/audio", fileName);
        //    if (!System.IO.File.Exists(filePath))
        //    {
        //        return NotFound();
        //    }

        //    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //    return File(fileStream, "audio/mpeg"); // MIME type for MP3
        //}
        //[HttpPost]
        //[RequestSizeLimit(5_000_000)] // Checking for 5 MB
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> VideoUpload(string selectedclaim, IFormFile videoFile, string videoLatitude, string videoLongitude, bool supervisorVideoUpdate = false)
        //{
        //    try
        //    {
        //        var currentUserEmail = HttpContext.User?.Identity?.Name;
        //        if (currentUserEmail == null)
        //        {
        //            notifyService.Error("OOPs !!!..Unauthenticated Access");
        //            return RedirectToAction(nameof(Index), "Dashboard");
        //        }

        //        if (string.IsNullOrWhiteSpace(currentUserEmail) ||
        //            (videoFile == null) ||
        //            string.IsNullOrWhiteSpace(videoLatitude) ||
        //            string.IsNullOrWhiteSpace(videoLongitude) ||
        //            Path.GetInvalidFileNameChars() == null ||
        //            string.IsNullOrWhiteSpace(Path.GetFileName(videoFile.FileName)) ||
        //            string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(videoFile.FileName))) ||
        //            string.IsNullOrWhiteSpace(Path.GetFileName(videoFile.Name))
        //            )
        //        {
        //            notifyService.Error("OOPs !!!..Contact Admin");
        //            return RedirectToAction(nameof(Index), "Dashboard");
        //        }
        //        if (string.IsNullOrWhiteSpace(selectedclaim))
        //        {
        //            notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
        //            return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
        //        }

        //        using (var ds = new MemoryStream())
        //        {
        //            videoFile.CopyTo(ds);
        //            var imageByte = ds.ToArray();
        //            var response = await agentService.PostVideo(currentUserEmail, selectedclaim, videoLatitude, videoLongitude, Path.GetFileName(videoFile.FileName), imageByte);

        //            notifyService.Custom($"Video Uploaded", 3, "green", "fas fa-mobile-alt");
        //            if (supervisorVideoUpdate)
        //            {
        //                return Redirect("/Supervisor/GetInvestigateReport?selectedcase=" + selectedclaim);
        //            }
        //            else
        //            {
        //                return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        notifyService.Error("OOPs !!!..Contact Admin");
        //        return RedirectToAction(nameof(Index), "Dashboard");
        //    }
        //}

        public IActionResult GetVideoFile(string fileName)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var filePath = Path.Combine("wwwroot/video", fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "video/mp4"); // MIME type for MP4
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}