using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Creator;
using risk.control.system.Models;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [Breadcrumb("Cases")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DocumentController : Controller
    {
        private readonly ILogger<CreatorController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentController(ILogger<CreatorController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> GetPolicyDocument(long id)
        {
            var path = await _context.PolicyDetail.AsNoTracking()
                .Where(p => p.PolicyDetailId == id)
                .Select(p => p.DocumentPath)
                .FirstOrDefaultAsync();

            return await ServeFileAsync(path, "Policy", id);
        }

        public async Task<IActionResult> GetCustomerDocument(long id)
        {
            var path = await _context.CustomerDetail.AsNoTracking()
                .Where(x => x.CustomerDetailId == id)
                .Select(x => x.ImagePath)
                .FirstOrDefaultAsync();

            return await ServeFileAsync(path, "Customer", id);
        }

        public async Task<IActionResult> GetBeneficiaryDocument(long id)
        {
            var path = await _context.BeneficiaryDetail.AsNoTracking()
                .Where(x => x.BeneficiaryDetailId == id)
                .Select(x => x.ImagePath)
                .FirstOrDefaultAsync();

            return await ServeFileAsync(path, "Beneficiary", id);
        }

        public async Task<IActionResult> GetCompanyDocument(long id)
        {
            try
            {
                var documentUrl = await _context.ClientCompany
                    .AsNoTracking()
                    .Where(x => x.ClientCompanyId == id)
                    .Select(x => x.DocumentUrl) // Only fetch the string, not the whole object
                    .FirstOrDefaultAsync();

                return await ServeFileAsync(documentUrl, "Company", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company document for ID {CompanyId}", id);
                return StatusCode(500, "An internal error occurred.");
            }
        }

        public async Task<IActionResult> GetAgencyDocument(long id)
        {
            // 1. Efficient Database Query (Select only what you need)
            var documentUrl = await _context.Vendor
                .AsNoTracking()
                .Where(x => x.VendorId == id)
                .Select(v => v.DocumentUrl)
                .FirstOrDefaultAsync();

            return await ServeFileAsync(documentUrl, "Agency", id);
        }

        public async Task<IActionResult> GetUserProfileImage(long id)
        {
            var path = await _context.ApplicationUser.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.ProfilePictureUrl)
                .FirstOrDefaultAsync();

            // Pass the fallback image path as the last argument
            return await ServeFileAsync(path, "User", id, "img/no-user.png");
        }

        public async Task<IActionResult> GetAgentDocument(long id)
        {
            var path = await _context.AgentIdReport.AsNoTracking()
                .Where(x => x.Id == id).Select(x => x.FilePath).FirstOrDefaultAsync();
            return await ServeFileAsync(path, "Agent", id);
        }

        public async Task<IActionResult> GetFaceDocument(long id)
        {
            var path = await _context.DigitalIdReport.AsNoTracking()
                .Where(x => x.Id == id).Select(x => x.FilePath).FirstOrDefaultAsync();
            return await ServeFileAsync(path, "Face", id);
        }

        public async Task<IActionResult> GetOcrDocument(long id)
        {
            var path = await _context.DocumentIdReport.AsNoTracking()
                .Where(x => x.Id == id).Select(x => x.FilePath).FirstOrDefaultAsync();
            return await ServeFileAsync(path, "OCR", id);
        }

        public async Task<IActionResult> GetMediaDocument(long id)
        {
            var path = await _context.MediaReport.AsNoTracking()
                .Where(x => x.Id == id).Select(x => x.FilePath).FirstOrDefaultAsync();

            // ServeFileAsync now handles .mp4, .wav, etc., via the ContentTypeProvider
            return await ServeFileAsync(path, "Media", id);
        }

        private async Task<IActionResult> ServeFileAsync(string? relativePath, string entityName, long id, string? defaultPath = null)
        {
            try
            {
                // 1. Resolve the path: Use the DB path, or fallback to default, or return NotFound
                string? targetPath = !string.IsNullOrWhiteSpace(relativePath) ? relativePath : defaultPath;

                if (string.IsNullOrEmpty(targetPath))
                    return NotFound($"{entityName} document not found.");

                // 2. Build the Full Path
                // If it's a default image, it might be in WebRoot; if it's a document, it's in ContentRoot.
                var fullPath = targetPath == defaultPath
                    ? Path.Combine(_env.WebRootPath, targetPath)
                    : Path.Combine(_env.ContentRootPath, targetPath);

                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogWarning("{Entity} file missing on disk: {Path} (ID: {Id})", entityName, fullPath, id);
                    return NotFound();
                }

                // 3. Automated MIME Type Detection (Handles Video, Audio, and Images)
                var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(fullPath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                // 4. PhysicalFile: The most efficient way to stream data.
                // It automatically handles FileStream, Range Requests (for video seeking), and memory buffering.
                return PhysicalFile(fullPath, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving {Entity} file for ID {Id}", entityName, id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}