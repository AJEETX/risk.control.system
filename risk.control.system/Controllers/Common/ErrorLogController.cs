using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Common
{
    [Breadcrumb("General Setup")]
    public class ErrorLogController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public ErrorLogController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(ErrorLog));
        }

        [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
        [HttpGet]
        [Breadcrumb("Error Logs")]
        public IActionResult ErrorLog()
        {
            var logPath = Path.Combine(_env.ContentRootPath, "Logs");
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

            var files = Directory.GetFiles(logPath, "*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new LogFileViewModel
                {
                    FileName = f.Name,
                    FullPath = f.FullName,
                    Date = f.CreationTime,
                    SizeKB = f.Length / 1024
                }).ToList();

            return View(files);
        }

        [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);

            var filePath = Path.Combine(_env.ContentRootPath, "Logs", safeFileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/json", fileName);
        }
    }
}