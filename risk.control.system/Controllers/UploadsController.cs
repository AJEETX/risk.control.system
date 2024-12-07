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

namespace risk.control.system.Controllers
{
    public class UploadsController : Controller
    {
        private static string NO_DATA = " NO - DATA ";
        private readonly ApplicationDbContext _context;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IClaimsVendorService vendorService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IToastNotification toastNotification;

        public UploadsController(ApplicationDbContext context,
            IFtpService ftpService,
            INotyfService notifyService,
            IClaimsVendorService vendorService,
            IWebHostEnvironment webHostEnvironment,
            IToastNotification toastNotification)
        {
            _context = context;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.vendorService = vendorService;
            this.webHostEnvironment = webHostEnvironment;
            this.toastNotification = toastNotification;
        }

        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> DownloadLog(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (file == null) return null!;
                var memory = new MemoryStream();
                using (var stream = new FileStream(file.FilePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
                return File(memory, file.FileType, file.Name + file.Extension);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        public async Task<IActionResult> DeleteLog(long id)
        {
            var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (file == null) return null;
            if (System.IO.File.Exists(file.FilePath))
            {
                System.IO.File.Delete(file.FilePath);
            }
            _context.FilesOnFileSystem.Remove(file);
            _context.SaveChanges();
            TempData["Message"] = $"Removed {file.Name + file.Extension} successfully from File System.";
            return RedirectToAction("Uploads");
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FaceUpload(string selectedcase, IFormFile digitalImage, string digitalIdLatitude, string digitalIdLongitude)
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
                    string.IsNullOrWhiteSpace(selectedcase) ||
                    string.IsNullOrWhiteSpace(digitalIdLatitude) || 
                    string.IsNullOrWhiteSpace(Path.GetFileName(digitalImage.FileName)) ||
                    string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(digitalImage.FileName))) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(digitalImage.Name)) ||
                    string.IsNullOrWhiteSpace(digitalIdLongitude))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-portrait");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedcase);
                }

                using (var ds = new MemoryStream())
                {
                    digitalImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await vendorService.PostFaceId(currentUserEmail, selectedcase, digitalIdLatitude, digitalIdLongitude, imageByte);

                    notifyService.Custom($"Photo Image Uploaded", 3, "green", "fas fa-portrait");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedcase);
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
        public async Task<IActionResult> PanUpload(string selectedclaim, IFormFile panImage, string documentIdLatitude, string documentIdLongitude)
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
                if (string.IsNullOrWhiteSpace(selectedclaim))
                {
                    notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
                }

                using (var ds = new MemoryStream())
                {
                    panImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await vendorService.PostDocumentId(currentUserEmail, selectedclaim, documentIdLatitude, documentIdLongitude, imageByte);

                    notifyService.Custom($"Pan card Image Uploaded", 3, "green", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
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
        public async Task<IActionResult> PassportUpload(string selectedclaim, IFormFile passportImage, string passportIdLatitude, string passportIdLongitude)
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
                    (passportImage == null) ||
                    string.IsNullOrWhiteSpace(passportIdLatitude) ||
                    string.IsNullOrWhiteSpace(passportIdLongitude) ||
                    Path.GetInvalidFileNameChars() == null ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(passportImage.FileName)) ||
                    string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(passportImage.FileName))) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(passportImage.Name))
                    )
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedclaim))
                {
                    notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
                }

                using (var ds = new MemoryStream())
                {
                    passportImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await vendorService.PostPassportId(currentUserEmail, selectedclaim, passportIdLatitude, passportIdLongitude, imageByte);

                    notifyService.Custom($"Passport Image Uploaded", 3, "green", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
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
        [RequestSizeLimit(5_000_000)] // Checking for 5 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AudioUpload(string selectedclaim, IFormFile audioFile, string audioLatitude, string audioLongitude)
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
                    (audioFile == null) ||
                    string.IsNullOrWhiteSpace(audioLatitude) ||
                    string.IsNullOrWhiteSpace(audioLongitude) ||
                    Path.GetInvalidFileNameChars() == null ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(audioFile.FileName)) ||
                    string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(audioFile.FileName))) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(audioFile.Name))
                    )
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedclaim))
                {
                    notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
                }

                using (var ds = new MemoryStream())
                {
                    audioFile.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await vendorService.PostAudio(currentUserEmail, selectedclaim, audioLatitude, audioLongitude, Path.GetFileName(audioFile.FileName), imageByte);

                    notifyService.Custom($"Audio Uploaded", 3, "green", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        public IActionResult GetAudioFile(string fileName)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var filePath = Path.Combine("wwwroot/audio", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, "audio/mpeg"); // MIME type for MP3
        }
        [HttpPost]
        [RequestSizeLimit(5_000_000)] // Checking for 5 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VideoUpload(string selectedclaim, IFormFile videoFile, string videoLatitude, string videoLongitude)
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
                    (videoFile == null) ||
                    string.IsNullOrWhiteSpace(videoLatitude) ||
                    string.IsNullOrWhiteSpace(videoLongitude) ||
                    Path.GetInvalidFileNameChars() == null ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(videoFile.FileName)) ||
                    string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(videoFile.FileName))) ||
                    string.IsNullOrWhiteSpace(Path.GetFileName(videoFile.Name))
                    )
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedclaim))
                {
                    notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
                }

                using (var ds = new MemoryStream())
                {
                    videoFile.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    var response = await vendorService.PostVideo(currentUserEmail, selectedclaim, videoLatitude, videoLongitude, Path.GetFileName(videoFile.FileName), imageByte);

                    notifyService.Custom($"Video Uploaded", 3, "green", "fas fa-mobile-alt");
                    return Redirect("/Agent/GetInvestigate?selectedcase=" + selectedclaim);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

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