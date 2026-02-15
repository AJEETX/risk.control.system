using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DownloadController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<DownloadController> logger;

        public DownloadController(ApplicationDbContext context, ILogger<DownloadController> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<IActionResult> EnquiryFileAttachment(int id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var applicationUser = await context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);
                if (applicationUser == null)
                {
                    return NotFound();
                }

                var fileAttachment = await context.QueryRequest.AsNoTracking().FirstOrDefaultAsync(q => q.QueryRequestId == id);
                if (fileAttachment == null)
                {
                    return NotFound();
                }
                return File(fileAttachment.QuestionImageAttachment, fileAttachment.QuestionImageFileType, fileAttachment.QuestionImageFileName + fileAttachment.QuestionImageFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting file attachment for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IActionResult> EnquiryReplyFileAttachment(int id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var applicationUser = await context.ApplicationUser.AsNoTracking().Where(u => u.Email == userEmail).FirstOrDefaultAsync();
                if (applicationUser == null)
                {
                    return NotFound();
                }

                var fileAttachment = await context.QueryRequest.AsNoTracking().FirstOrDefaultAsync(q => q.QueryRequestId == id);
                if (fileAttachment == null)
                {
                    return NotFound();
                }
                return File(fileAttachment.AnswerImageAttachment, fileAttachment.AnswerImageFileType, fileAttachment.AnswerImageFileName + fileAttachment.AnswerImageFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting file attachment for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IActionResult> SupervisorFileAttachment(int id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var applicationUser = await context.ApplicationUser.AsNoTracking().Where(u => u.Email == userEmail).FirstOrDefaultAsync();
                if (applicationUser == null)
                {
                    return NotFound();
                }

                var investigation = await context.Investigations.AsNoTracking().Include(i => i.InvestigationReport).FirstOrDefaultAsync(q => q.InvestigationReport.Id == id);
                if (investigation == null || investigation.InvestigationReport == null)
                {
                    return NotFound();
                }

                var fileAttachment = investigation.InvestigationReport;

                return File(fileAttachment.SupervisorAttachment, fileAttachment.SupervisorFileType, fileAttachment.SupervisorFileName + fileAttachment.SupervisorFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting file attachment for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}