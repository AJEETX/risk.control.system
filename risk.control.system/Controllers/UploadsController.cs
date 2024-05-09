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

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Breadcrumb(" Upload Log", FromController = typeof(ClaimsInvestigationController))]
        public async Task<IActionResult> Uploads()
        {
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var files = await _context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail).ToListAsync();
                ViewBag.Message = TempData["Message"];
                return View(new FileUploadViewModel { FilesOnFileSystem = files });
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        public async Task<IActionResult> DownloadLog(long id)
        {
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
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
            catch (Exception)
            {
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
                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-portrait");
                    return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedcase);
                }

                using (var ds = new MemoryStream())
                {
                    digitalImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    await vendorService.PostFaceId(userEmail, selectedcase, digitalIdLatitude, digitalIdLongitude, imageByte);

                    notifyService.Custom($"Photo Image Uploaded", 3, "green", "fas fa-portrait");
                    return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedcase);
                }
            }
            catch (Exception)
            {
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

                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(selectedclaim))
                {
                    notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
                    return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedclaim);
                }

                using (var ds = new MemoryStream())
                {
                    panImage.CopyTo(ds);
                    var imageByte = ds.ToArray();
                    await vendorService.PostDocumentId(userEmail, selectedclaim, documentIdLatitude, documentIdLongitude, imageByte);

                    notifyService.Custom($"Pan card Image Uploaded", 3, "green", "fas fa-mobile-alt");
                    return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedclaim);
                }
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FileUpload(IFormFile postedFile, string uploadtype)
        {
            if(postedFile == null || string.IsNullOrWhiteSpace(uploadtype) || 
                string.IsNullOrWhiteSpace(Path.GetFileName(postedFile.FileName)) ||
                (Path.GetInvalidFileNameChars() == null) ||
                string.IsNullOrWhiteSpace(Path.GetExtension(postedFile.FileName)) ||
                Path.GetExtension(postedFile.FileName) !="zip"
                )
            {
                notifyService.Custom($"Upload Error. Contact Admin", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction("Draft", "ClaimsInvestigation");
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                    if (uploadType == UploadType.FTP)
                    {
                        await ftpService.DownloadFtpFile(userEmail, postedFile);

                        notifyService.Custom($"Ftp download complete ", 3, "green", "far fa-file-powerpoint");

                        return RedirectToAction("Draft", "ClaimsInvestigation");
                    }

                    if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                    {
                        try
                        {
                            await ftpService.UploadFile(userEmail, postedFile);

                            notifyService.Custom($"File upload complete", 3, "green", "far fa-file-powerpoint");

                            return RedirectToAction("Draft", "ClaimsInvestigation");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }

                notifyService.Custom($"Upload Error. Contact Admin", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction("Draft", "ClaimsInvestigation");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}