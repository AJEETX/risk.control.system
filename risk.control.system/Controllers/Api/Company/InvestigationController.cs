using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Services;
using static risk.control.system.AppConstant.Applicationsettings;
using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class InvestigationController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly IInvestigationService service;

        public InvestigationController(ApplicationDbContext context, IInvestigationService service)
        {
            _context = context;
            this.service = service;
        }

        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetAuto")]
        public async Task<IActionResult> GetAuto(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var response = await service.GetAuto(currentUserEmail, draw, start, length, search, caseType, orderColumn, orderDir);

            return Ok(response);

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

            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);

            var totalReadyToAssign = await service.GetAutoCount(userEmail);
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            if (uploadId > 0)
            {
                var file = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
                if (file == null)
                {
                    return NotFound(new { success = false, message = "File not found." });
                }
                if (file.ClaimsId != null && file.ClaimsId.Count > 0)
                {
                    totalReadyToAssign = totalReadyToAssign + file.ClaimsId.Count;
                }
            }


            var files = await _context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId && ((f.UploadedBy == userEmail && !f.Deleted) || isManager)).ToListAsync();
            var result = files.OrderBy(o => o.CreatedOn).Select(file => new
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
            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var file = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            if (file == null)
            {
                return NotFound(new { success = false, message = "File not found." });
            }
            var totalReadyToAssign = await service.GetAutoCount(userEmail);
            var totalForAssign = totalReadyToAssign + file.ClaimsId?.Count;
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);
            var result = new
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

    }
}