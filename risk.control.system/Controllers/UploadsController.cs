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
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var files = await _context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId).ToListAsync();
            ViewBag.Message = TempData["Message"];
            return View(new FileUploadViewModel { FilesOnFileSystem = files });
        }

        public async Task<IActionResult> DownloadLog(long id)
        {
            var file = await _context.FilesOnFileSystem.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (file == null) return null;
            var memory = new MemoryStream();
            using (var stream = new FileStream(file.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, file.FileType, file.Name + file.Extension);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FaceUpload(string selectedcase, IFormFile digitalImage, string digitalIdLatitude, string digitalIdLongitude)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-portrait");
                return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedcase);
            }

            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Custom($"Ftp Downloaded Claims ready", 3, "green", "fas fa-portrait");
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(ClaimsVendorController.Agent), "ClaimsVendor");
            }
            using (var ds = new MemoryStream())
            {
                digitalImage.CopyTo(ds);
                var imageByte = ds.ToArray();
                await vendorService.PostFaceId(userEmail, selectedcase, digitalIdLatitude, digitalIdLongitude, imageByte);

                notifyService.Custom($"Digital Id Image Uploaded", 3, "green", "fas fa-portrait");
                return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedcase);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PanUpload(string selectedclaim, IFormFile panImage, string documentIdLatitude, string documentIdLongitude)
        {
            if (string.IsNullOrWhiteSpace(selectedclaim))
            {
                notifyService.Custom($"No claim selected!!!. ", 3, "orange", "fas fa-mobile-alt");
                return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedclaim);
            }

            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Custom($"Ftp Downloaded Claims ready", 3, "green", "fas fa-mobile-alt");
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(ClaimsVendorController.Agent), "ClaimsVendor");
            }

            using (var ds = new MemoryStream())
            {
                panImage.CopyTo(ds);
                var imageByte = ds.ToArray();
                await vendorService.PostDocumentId(userEmail, selectedclaim, documentIdLatitude, documentIdLongitude, imageByte);

                notifyService.Custom($"Digital Id Image Uploaded", 3, "green", "fas fa-mobile-alt");
                return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedclaim);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FileUpload(IFormFile postedFile, string uploadtype)
        {
            var userEmail = HttpContext.User.Identity.Name;
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

            notifyService.Custom($"Upload Error. Contact IT support", 3, "red", "far fa-file-powerpoint");

            return RedirectToAction("Draft", "ClaimsInvestigation");
        }
    }
}