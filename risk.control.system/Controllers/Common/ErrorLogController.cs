using System.Text.Json;
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
        private readonly ILogger<ErrorLogController> _logger;

        public ErrorLogController(IWebHostEnvironment env, ILogger<ErrorLogController> logger)
        {
            _env = env;
            _logger = logger;
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
            var logPath = Path.Combine(_env.ContentRootPath, CONSTANTS.LogsDirectory);
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
        [Breadcrumb("Error List", FromAction = nameof(ErrorLog))]
        public async Task<IActionResult> Details(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name is missing.");
            }

            // --- SECURITY FIX: Prevent Path Traversal ---
            // Extract only the base file name (strips path manipulators like '../' or 'c:\')
            string safeFileName = Path.GetFileName(fileName);

            string logsFolderPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.LogsDirectory));
            string filePath = Path.GetFullPath(Path.Combine(logsFolderPath, safeFileName));

            // Ensure the resolved full path actually starts inside the intended folder
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

                // --- ASYNC & LOCKING FIX: Non-blocking async reading with shared access ---
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var errorEntry = JsonSerializer.Deserialize<ErrorDetailsViewModel>(line, options);
                        if (errorEntry != null)
                        {
                            viewModel.ErrorDetails.Add(errorEntry);
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed individual NDJSON entries
                        continue;
                    }
                }

                return View(viewModel);
            }
            catch (IOException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error reading log file: {ex.Message}");
                return View(viewModel);
            }
        }
        [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
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