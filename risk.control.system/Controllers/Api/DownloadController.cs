using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;

namespace risk.control.system.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DownloadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DownloadController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> EnquiryFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var fileAttachment = await _context.QueryRequest.FirstOrDefaultAsync(q => q.QueryRequestId == id);

            return fileAttachment != null ? File(fileAttachment.QuestionImageAttachment, fileAttachment.QuestionImageFileType, fileAttachment.QuestionImageFileName + fileAttachment.QuestionImageFileName) : null;
        }
        public async Task<IActionResult> EnquiryReplyFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var fileAttachment = await _context.QueryRequest.FirstOrDefaultAsync(q => q.QueryRequestId == id);

            return fileAttachment != null ? File(fileAttachment.AnswerImageAttachment, fileAttachment.AnswerImageFileType, fileAttachment.AnswerImageFileName + fileAttachment.AnswerImageFileName) : null;
        }
        public async Task<IActionResult> SupervisorFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var investigation = await _context.Investigations.Include(i => i.InvestigationReport).FirstOrDefaultAsync(q => q.InvestigationReport.Id == id);
            var fileAttachment = investigation.InvestigationReport;

            return fileAttachment != null ? File(fileAttachment.SupervisorAttachment, fileAttachment.SupervisorFileType, fileAttachment.SupervisorFileName + fileAttachment.SupervisorFileName) : null;
        }
    }
}
