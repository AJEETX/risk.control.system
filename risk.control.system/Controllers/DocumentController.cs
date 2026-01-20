using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [Breadcrumb(" Cases")]
    public class DocumentController : Controller
    {
        private readonly ILogger<InvestigationController> logger;
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public DocumentController(ILogger<InvestigationController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            this.logger = logger;
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> GetPolicyDocument(long id)
        {
            var task = await context.Investigations
                .Include(x => x.PolicyDetail)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (task?.PolicyDetail?.DocumentPath == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, task.PolicyDetail.DocumentPath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }
        public async Task<IActionResult> GetCustomerDocument(long id)
        {
            var customer = await context.CustomerDetail
                .FirstOrDefaultAsync(x => x.CustomerDetailId == id);

            if (customer?.ImagePath == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, customer.ImagePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }

        public async Task<IActionResult> GetBeneficiaryDocument(long id)
        {
            var customer = await context.BeneficiaryDetail
                .FirstOrDefaultAsync(x => x.BeneficiaryDetailId == id);

            if (customer?.ImagePath == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, customer.ImagePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }

        public async Task<IActionResult> GetCompanyDocument(long id)
        {
            var company = await context.ClientCompany
                .FirstOrDefaultAsync(x => x.ClientCompanyId == id);

            if (company.DocumentUrl == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, company.DocumentUrl);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }

        public async Task<IActionResult> GetAgencyDocument(long id)
        {
            var vendor = await context.Vendor
                .FirstOrDefaultAsync(x => x.VendorId == id);

            if (vendor.DocumentUrl == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, vendor.DocumentUrl);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }
        public async Task<IActionResult> GetUserProfileImage(long id)
        {
            var user = await context.ApplicationUser
                .FirstOrDefaultAsync(x => x.Id == id);
            string fullPath = string.Empty;
            if (user.ProfilePictureUrl != null)
            {
                fullPath = Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl);
            }
            else
            {
                fullPath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-user.png");
            }

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }

        public async Task<IActionResult> GetAgentDocument(long id)
        {
            var agent = await context.AgentIdReport
                .FirstOrDefaultAsync(x => x.Id == id);

            if (agent.FilePath == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, agent.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }
        public async Task<IActionResult> GetFaceDocument(long id)
        {
            var agent = await context.DigitalIdReport
                .FirstOrDefaultAsync(x => x.Id == id);

            if (agent.FilePath == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, agent.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }
        public async Task<IActionResult> GetOcrDocument(long id)
        {
            var agent = await context.DocumentIdReport
                .FirstOrDefaultAsync(x => x.Id == id);

            if (agent.FilePath == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, agent.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, contentType);
        }
        public async Task<IActionResult> GetMediaDocument(long id)
        {
            var media = await context.MediaReport.FirstOrDefaultAsync(x => x.Id == id);

            if (media == null || string.IsNullOrWhiteSpace(media.FilePath))
                return NotFound();

            // Always force root to be inside your Document folder
            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, media.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();

            var contentType = ext switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogg" => "video/ogg",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".wmv" => "video/x-ms-wmv",

                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".aac" => "audio/aac",
                ".flac" => "audio/flac",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

            return File(stream, contentType);  // StreamResult, does NOT load entire file
        }
    }
}
