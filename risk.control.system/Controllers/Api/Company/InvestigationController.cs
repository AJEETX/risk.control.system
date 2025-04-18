using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;
using risk.control.system.Services;
using System.Globalization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using risk.control.system.Controllers.Api.Claims;
using Google.Api;
using System.Linq.Expressions;
using System.Linq;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class InvestigationController : ControllerBase
    {
        private const string UNDERWRITING = "underwriting";
        private const string CLAIMS = "claims";
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ICreatorService creatorService;
        private readonly IClaimsService claimsService;
        private readonly IInvestigationService service;

        public InvestigationController(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            IInvestigationService service,
            ICreatorService creatorService,
            IClaimsService claimsService)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            _context = context;
            this.service = service;
            this.webHostEnvironment = webHostEnvironment;
            this.creatorService = creatorService;
            this.claimsService = claimsService;
        }
        
        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetAuto")]
        public async Task<IActionResult> GetAuto(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            
            var response = await service.GetAuto(currentUserEmail,draw,start,length, search, caseType, orderColumn, orderDir);

            return Ok(response);
            
        }

        private static string GetCreatorAutoTimePending(ClaimsInvestigation a, bool assigned = false)
        {
            if (DateTime.Now.Subtract(a.Updated.Value).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Updated.Value).Days} days since created!\"></i>");

            else if (DateTime.Now.Subtract(a.Updated.Value).Days >= 3 || DateTime.Now.Subtract(a.Updated.Value).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(a.Updated.Value).Days} day since created.\"></i>");
            if (DateTime.Now.Subtract(a.Updated.Value).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span>");

            if (DateTime.Now.Subtract(a.Updated.Value).Hours < 24 &&
                DateTime.Now.Subtract(a.Updated.Value).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.Value).Hours == 0 && DateTime.Now.Subtract(a.Updated.Value).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.Value).Minutes == 0 && DateTime.Now.Subtract(a.Updated.Value).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User.Identity.Name;

            var response = await service.GetActive(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

            return Ok(response);
        }

        [HttpGet("GetFilesData/{uploadId?}")]
        public async Task<IActionResult> GetFilesData(int uploadId = 0)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.Include(c=>c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);

            var totalReadyToAssign = await service.GetAutoCount(userEmail);
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            if(uploadId > 0 )
            {
                var file = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
                if (file == null)
                {
                    return NotFound(new { success = false, message = "File not found." });
                }
                if(file.ClaimsId != null && file.ClaimsId.Count > 0)
                {
                    totalReadyToAssign = totalReadyToAssign + file.ClaimsId.Count;
                }
            }
            

            var files = await _context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId && ((f.UploadedBy == userEmail && !f.Deleted) || isManager)).ToListAsync();
            var result = files.OrderBy(o=>o.CreatedOn).Select(file => new
            {
                file.Id,
                SequenceNumber = isManager ? file.CompanySequenceNumber : file.UserSequenceNumber,
                file.Name,
                file.Description,
                file.FileType,
                CreatedOn = file.CreatedOn.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm:ss"),
                file.UploadedBy,
                Status = file.Status,
                file.Message,
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager,
                file.Completed,
                file.DirectAssign,
                UploadedType = file.DirectAssign ? "<i class='fas fa-random i-assign'></i>" : "<i class='fas fa-upload i-upload'></i>",
                TimeTaken = file.CompletedOn != null ? $" {(Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds) < 1 ? 1 :
                Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds))} sec" : "<i class='fas fa-sync fa-spin i-grey'></i>",
            }).ToList();

            return Ok(new { data = result, maxAssignReadyAllowed = maxAssignReadyAllowedByCompany >= totalReadyToAssign });
        }

        [HttpGet("GetFileById/{uploadId}")]
        public async Task<IActionResult> GetFileById(int uploadId)
        {
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser.Include(c=>c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var file =  await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            if (file == null)
            {
                return NotFound(new { success = false, message = "File not found." });
            }
            var totalReadyToAssign = await service.GetAutoCount(userEmail);
            var totalForAssign = totalReadyToAssign + file.ClaimsId?.Count;
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);
            var result =  new
            {
                file.Id,
                SequenceNumber = isManager ? file.CompanySequenceNumber : file.UserSequenceNumber,
                file.Name,
                file.Description,
                file.FileType,
                CreatedOn = file.CreatedOn.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm:ss"),
                file.UploadedBy,
                Status = file.Status,
                file.Completed,
                file.Message,
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager,
                file.DirectAssign,
                UploadedType = file.DirectAssign ? "<i class='fas fa-random i-assign'></i>" : "<i class='fas fa-upload i-upload'></i>",
                TimeTaken = file.CompletedOn != null ? $" {(Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds) < 1 ? 1 : 
                Math.Round((file.CompletedOn.Value - file.CreatedOn.Value).TotalSeconds))} sec" : "<i class='fas fa-sync fa-spin i-grey'></i>",
            };//<i class='fas fa-sync fa-spin'></i>

            return Ok(new { data = result, maxAssignReadyAllowed = maxAssignReadyAllowedByCompany >= totalForAssign });
        }

        [HttpGet("GetPendingAllocations")]
        public async Task<IActionResult> GetPendingAllocations()
        {
            var userEmail = HttpContext.User.Identity.Name;
            var pendingCount = await _context.ClaimsInvestigation.CountAsync(c=>c.UpdatedBy == userEmail && c.STATUS == ALLOCATION_STATUS.PENDING);
            return Ok(new { count = pendingCount });
        }
        private byte[] GetOwner(ClaimsInvestigation a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            if (!string.IsNullOrWhiteSpace(a.UserEmailActionedTo) && a.InvestigationCaseSubStatusId == allocated2agent.InvestigationCaseSubStatusId)
            {
                ownerEmail = a.UserEmailActionedTo;
                var agentProfile = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == ownerEmail)?.ProfilePicture;
                if (agentProfile == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return agentProfile;
            }
            else if (string.IsNullOrWhiteSpace(a.UserEmailActionedTo) &&
                !string.IsNullOrWhiteSpace(a.UserRoleActionedTo)
                && a.AssignedToAgency)
            {
                ownerDomain = a.UserRoleActionedTo;
                var vendorImage = _context.Vendor.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (vendorImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return vendorImage;
            }
            else
            {
                ownerDomain = a.UserRoleActionedTo;
                var companyImage = _context.ClientCompany.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (companyImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return companyImage;
            }

        }

    }
}