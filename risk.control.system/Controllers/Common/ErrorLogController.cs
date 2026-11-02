using System.Text.Json;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Common
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class ErrorLogController(
        IWebHostEnvironment env,
        INotyfService notifyService,
        ILogger<ErrorLogController> logger) : Controller
    {
        private readonly INotyfService _notifyService = notifyService;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<ErrorLogController> _logger = logger;

        public IActionResult Index()
        {
            return RedirectToAction(nameof(ErrorLog));
        }

        [HttpGet]
        [Breadcrumb("Error Logs")]
        public IActionResult ErrorLog()
        {
            var logPath = Path.Combine(_env.ContentRootPath, CONSTANTS.LogsDirectory);
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

            var files = Directory.GetFiles(logPath, "*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new LogFileViewModel
                {
                    FileName = f.Name,
                    FullPath = f.FullName,
                    Date = f.CreationTime.ToLocalTime(),
                    SizeKB = f.Length / 1024
                }).ToList();

            return View(files);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest();
            }

            var safeFileName = Path.GetFileName(fileName);
            var logPath = Path.Combine(_env.ContentRootPath, CONSTANTS.LogsDirectory);
            var filePath = Path.Combine(logPath, safeFileName);

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Attempt to open the file to verify it's not locked by Serilog
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If it successfully opens, it's not locked
                    }

                    // -> HERE: Since it successfully opened and closed, it's safe to delete!
                    System.IO.File.Delete(filePath);

                    // Optional: Add a success notification using your notify service
                    _notifyService.Success("Log file deleted successfully.");
                }
                catch (Exception ex)
                {
                    // The file is currently in use/locked by Serilog (active log file)
                    _logger.LogError(ex, "Cannot delete the active log file currently in use by the system.");
                    _notifyService.Custom("Cannot delete the active log file currently in use by the system", 3, "red", "fa fa-lock");
                }
            }

            return RedirectToAction(nameof(ErrorLog));
        }
        [HttpGet]
        [Breadcrumb("Error List", FromAction = nameof(ErrorLog))]
        public async Task<IActionResult> Details(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name is missing.");
            }

            string safeFileName = Path.GetFileName(fileName);
            string logsFolderPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.LogsDirectory));
            string filePath = Path.GetFullPath(Path.Combine(logsFolderPath, safeFileName));

            if (!filePath.StartsWith(logsFolderPath, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(filePath))
            {
                return NotFound("Log file not found.");
            }

            var viewModel = new ErrorDetailsViewModelList
            {
                ErrorDetails = new List<ErrorDetailsViewModel>(),
                FileName = safeFileName
            };

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Read the entire file content asynchronously to handle concatenated objects safely
                string fileContent;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true))
                using (var reader = new StreamReader(stream))
                {
                    fileContent = await reader.ReadToEndAsync();
                }

                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
                    var reader = new Utf8JsonReader(utf8Bytes);

                    // Parse multiple consecutive JSON objects even if they share lines
                    while (JsonDocument.TryParseValue(ref reader, out var jsonDoc))
                    {
                        try
                        {
                            var errorEntry = jsonDoc.Deserialize<ErrorDetailsViewModel>(options);
                            if (errorEntry != null)
                            {
                                viewModel.ErrorDetails.Add(errorEntry);
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip malformed individual entries
                            continue;
                        }
                    }
                }

                viewModel.ErrorDetails = viewModel.ErrorDetails.OrderByDescending(o => o.Timestamp).ToList();
                return View(viewModel);
            }
            catch (IOException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error reading log file: {ex.Message}");
                return View(viewModel);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);

            string logsDirectoryPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.LogsDirectory));

            var relativePath = Path.Combine(logsDirectoryPath, safeFileName);
            var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, relativePath));

            if (!fullPath.StartsWith(logsDirectoryPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Malicious path detected: {FileName}", safeFileName);
                return BadRequest("Invalid file name.");
            }

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Log file not found: {FileName}", safeFileName);
                return NotFound("Log file not found.");
            }

            return PhysicalFile(fullPath, "application/json", safeFileName);
        }
    }
}