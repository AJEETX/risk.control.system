using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.Models.ViewModel;
using Serilog;

namespace risk.control.system.Controllers.Common
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public HomeController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Get the exception details if needed for logging/display
            var exceptionDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            // Log the error specifically if it wasn't caught elsewhere
            if (exceptionDetails != null)
            {
                // Serilog will pick this up via your existing configuration
                Log.Error(exceptionDetails.Error, "Unhandled exception at {Path}", exceptionDetails.Path);
            }

            return View();
        }

        [HttpGet]
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

        public IActionResult Download(string fileName)
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Logs", fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/json", fileName);
        }
    }
}