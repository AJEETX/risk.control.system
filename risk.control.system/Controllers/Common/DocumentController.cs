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
    [Breadcrumb(" Cases")]
    [ApiExplorerSettings(IgnoreApi = true)]
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
            try
            {
                var policyDetail = await context.PolicyDetail.AsNoTracking().FirstOrDefaultAsync(x => x.PolicyDetailId == id);

                if (policyDetail?.DocumentPath == null)
                    return NotFound();

                var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, policyDetail.DocumentPath);

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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving policy document for investigation {InvestigationId}", id);
                throw;
            }
        }

        public async Task<IActionResult> GetCustomerDocument(long id)
        {
            try
            {
                var customer = await context.CustomerDetail.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving customer document for customer {CustomerId}", id);
                throw;
            }
        }

        public async Task<IActionResult> GetBeneficiaryDocument(long id)
        {
            try
            {
                var customer = await context.BeneficiaryDetail.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving beneficiary document for beneficiary {BeneficiaryId}", id);
                throw;
            }
        }

        public async Task<IActionResult> GetCompanyDocument(long id)
        {
            try
            {
                var company = await context.ClientCompany.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving company document for company {CompanyId}", id);
                throw;
            }
        }

        public async Task<IActionResult> GetAgencyDocument(long id)
        {
            try
            {
                var vendor = await context.Vendor.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving Agency document for agency {AgencyId}", id);
                throw;
            }
        }

        public async Task<IActionResult> GetUserProfileImage(long id)
        {
            try
            {
                var user = await context.ApplicationUser.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving User Profile image {Id}", id);

                throw;
            }
        }

        public async Task<IActionResult> GetAgentDocument(long id)
        {
            try
            {
                var agent = await context.AgentIdReport.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving Agent Profile image {Id}", id);

                throw;
            }
        }

        public async Task<IActionResult> GetFaceDocument(long id)
        {
            try
            {
                var agent = await context.DigitalIdReport.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving Person Profile image {Id}", id);

                throw;
            }
        }

        public async Task<IActionResult> GetOcrDocument(long id)
        {
            try
            {
                var agent = await context.DocumentIdReport.AsNoTracking()
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

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving OCR image {Id}", id);

                throw;
            }
        }

        public async Task<IActionResult> GetMediaDocument(long id)
        {
            try
            {
                var media = await context.MediaReport.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving Media File {Id}", id);

                throw;
            }
        }
    }
}