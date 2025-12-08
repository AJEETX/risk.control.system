using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.Data;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

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
            IWebHostEnvironment webHostEnvironment,
            IFeatureManager featureManager,
            INotyfService notifyService, IInvestigationService service,
            IEmpanelledAgencyService empanelledAgencyService,
            IPhoneService phoneService)
        {
            this.logger = logger;
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
        }
        public IActionResult GetPolicyDocument(long id)
        {
            var task = context.Investigations
                .Include(x => x.PolicyDetail)
                .FirstOrDefault(x => x.Id == id);

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
        public IActionResult GetCustomerDocument(long id)
        {
            var customer = context.CustomerDetail
                .FirstOrDefault(x => x.CustomerDetailId == id);

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

        public IActionResult GetBeneficiaryDocument(long id)
        {
            var customer = context.BeneficiaryDetail
                .FirstOrDefault(x => x.BeneficiaryDetailId == id);

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

        public IActionResult GetCompanyDocument(long id)
        {
            var company = context.ClientCompany
                .FirstOrDefault(x => x.ClientCompanyId == id);

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

        public IActionResult GetAgencyDocument(long id)
        {
            var vendor = context.Vendor
                .FirstOrDefault(x => x.VendorId == id);

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
        public IActionResult GetCompanyUserDocument(long id)
        {
            var companyUser = context.ClientCompanyApplicationUser
                .FirstOrDefault(x => x.Id == id);

            if (companyUser.ProfilePictureUrl == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, companyUser.ProfilePictureUrl);

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

        public IActionResult GetAgencyUserDocument(long id)
        {
            var vendorUser = context.VendorApplicationUser
                .FirstOrDefault(x => x.Id == id);

            if (vendorUser.ProfilePictureUrl == null)
                return NotFound();

            var fullPath = Path.Combine(webHostEnvironment.ContentRootPath, vendorUser.ProfilePictureUrl);

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
        public IActionResult GetAgentDocument(long id)
        {
            var agent = context.AgentIdReport
                .FirstOrDefault(x => x.Id == id);

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
        public IActionResult GetFaceDocument(long id)
        {
            var agent = context.DigitalIdReport
                .FirstOrDefault(x => x.Id == id);

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
        public IActionResult GetOcrDocument(long id)
        {
            var agent = context.DocumentIdReport
                .FirstOrDefault(x => x.Id == id);

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
        public IActionResult GetMediaDocument(long id)
        {
            var media = context.MediaReport.FirstOrDefault(x => x.Id == id);

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
