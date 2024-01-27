using CsvHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using AspNetCoreHero.ToastNotification.Abstractions;

namespace risk.control.system.Controllers
{
    public class UploadsController : Controller
    {
        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private readonly ApplicationDbContext _context;
        private readonly IFtpService ftpService;
        private readonly IHttpClientService httpClientService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly IClaimsVendorService vendorService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;
        private static HttpClient httpClient = new();

        public UploadsController(ApplicationDbContext context,
            IFtpService ftpService,
            IHttpClientService httpClientService,
            IClaimsInvestigationService claimsInvestigationService,
            IMailboxService mailboxService,
            UserManager<ClientCompanyApplicationUser> userManager,
            INotyfService notifyService,
            IClaimsVendorService vendorService,
            IWebHostEnvironment webHostEnvironment,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification)
        {
            _context = context;
            this.ftpService = ftpService;
            this.httpClientService = httpClientService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.vendorService = vendorService;
            this.webHostEnvironment = webHostEnvironment;
            this.roleManager = roleManager;
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

            var fileuploadViewModel = await LoadAllFiles(userEmail);
            ViewBag.Message = TempData["Message"];
            return View(fileuploadViewModel);
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

        private async Task<FileUploadViewModel> LoadAllFiles(string userEmail)
        {
            var viewModel = new FileUploadViewModel();
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            viewModel.FilesOnFileSystem = await _context.FilesOnFileSystem.Where(f => f.CompanyId == company.ClientCompanyId).ToListAsync();
            return viewModel;
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
            using var ds = new MemoryStream();
            digitalImage.CopyTo(ds);
            var imageByte = ds.ToArray();
            await vendorService.PostFaceId(userEmail, selectedcase, digitalIdLatitude, digitalIdLongitude, imageByte);

            notifyService.Custom($"Digital Id Image Uploaded", 3, "green", "fas fa-portrait");
            return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedcase);
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

            using var ds = new MemoryStream();
            panImage.CopyTo(ds);
            var imageByte = ds.ToArray();
            await vendorService.PostDocumentId(userEmail, selectedclaim, documentIdLatitude, documentIdLongitude, imageByte);

            notifyService.Custom($"Digital Id Image Uploaded", 3, "green", "fas fa-mobile-alt");
            return Redirect("/ClaimsVendor/GetInvestigate?selectedcase=" + selectedclaim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FileUpload(IFormFile postedFile, string uploadtype)
        {
            if (postedFile != null)
            {
                UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                if (uploadType == UploadType.FTP)
                {
                    await FtpUploadClaims(postedFile);

                    notifyService.Custom($"Ftp Downloaded Claims ready", 3, "green", "far fa-file-powerpoint");

                    return RedirectToAction("Draft", "ClaimsInvestigation");
                }

                if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                {
                    try
                    {
                        await FileUploadClaims(postedFile);

                        notifyService.Custom($"File uploaded Claims ready", 3, "green", "far fa-file-powerpoint");

                        return RedirectToAction("Draft", "ClaimsInvestigation");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            notifyService.Custom($"Upload Error. Pls try again", 3, "red", "far fa-file-powerpoint");

            return RedirectToAction("Draft", "ClaimsInvestigation");
        }

        private async Task FileUploadClaims(IFormFile postedFile)
        {
            try
            {
                string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-file");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string docPath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(docPath))
                {
                    Directory.CreateDirectory(docPath);
                }
                string fileName = Path.GetTempFileName();
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(postedFile.FileName);
                fileNameWithoutExtension += DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss");

                string filePath = Path.Combine(path, fileName);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }
                var userEmail = HttpContext.User.Identity.Name;

                await ftpService.UploadFile(userEmail, filePath, docPath, fileNameWithoutExtension);

                await SaveUpload(postedFile, filePath, "File upload", userEmail);

                var rows = _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task FtpUploadClaims(IFormFile postedFile)
        {
            try
            {
                string folder = Path.Combine(webHostEnvironment.WebRootPath, "document");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(folder, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }
                var wc = new WebClient
                {
                    Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA),
                };
                var response = wc.UploadFile(Applicationsettings.FTP_SITE + fileName, filePath);

                var data = Encoding.UTF8.GetString(response);

                var userEmail = HttpContext.User.Identity.Name;

                SaveUpload(postedFile, filePath, "Ftp upload", userEmail);

                await ftpService.DownloadFtp(userEmail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task SaveUpload(IFormFile file, string filePath, string description, string uploadedBy)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var company = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == uploadedBy);
            var fileModel = new FileOnFileSystemModel
            {
                CreatedOn = DateTime.UtcNow,
                FileType = file.ContentType,
                Extension = extension,
                Name = fileName,
                Description = description,
                FilePath = filePath,
                UploadedBy = uploadedBy,
                CompanyId = company.ClientCompanyId
            };
            _context.FilesOnFileSystem.Add(fileModel);
            await _context.SaveChangesAsync();
        }
    }
}