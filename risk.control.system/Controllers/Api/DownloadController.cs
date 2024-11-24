using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

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
        public async Task<IActionResult> InboxDetailsDownloadFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser =await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var userMailbox = _context.Mailbox.Include(m => m.Inbox)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var InboxFile = userMailbox.Inbox.FirstOrDefault(c => c.InboxMessageId == id);

            return InboxFile != null ? File(InboxFile.Attachment, InboxFile.FileType, InboxFile.AttachmentName + InboxFile.Extension) : Problem();
        }
        public async Task<IActionResult> OutboxDetailsDownloadFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var userMailbox = await _context.Mailbox.Include(m => m.Outbox)
                .FirstOrDefaultAsync(c => c.Name == applicationUser.Email);

            OutboxMessage? outBox = userMailbox.Outbox.FirstOrDefault(c => c.OutboxMessageId == id);

            return outBox != null ? File(outBox.Attachment, outBox.FileType, outBox.AttachmentName + outBox.Extension) : Problem();
        }
        public async Task<IActionResult> SentDetailsDownloadFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var userMailbox = await _context.Mailbox.Include(m => m.Sent)
                .FirstOrDefaultAsync(c => c.Name == applicationUser.Email);

            SentMessage? sentBox = userMailbox.Sent.FirstOrDefault(c => c.SentMessageId == id);

            return sentBox != null ? File(sentBox.Attachment, sentBox.FileType, sentBox.AttachmentName + sentBox.Extension) : Problem();
        }
        public async Task<IActionResult> TrashDetailsDownloadFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var userMailbox = await _context.Mailbox.Include(m => m.Trash)
                .FirstOrDefaultAsync(c => c.Name == applicationUser.Email);

            TrashMessage? trash = userMailbox.Trash.FirstOrDefault(c => c.TrashMessageId == id);

            return trash != null ? File(trash.Attachment, trash.FileType, trash.AttachmentName + trash.Extension) : Problem();
        }

        public async Task<IActionResult> EnquiryFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var fileAttachment = _context.QueryRequest.FirstOrDefault(q=>q.QueryRequestId == id);

            return fileAttachment != null ? File(fileAttachment.QuestionImageAttachment, fileAttachment.QuestionImageFileType, fileAttachment.QuestionImageFileName + fileAttachment.QuestionImageFileName): null;
        }
        public async Task<IActionResult> EnquiryReplyFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = await _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefaultAsync();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var fileAttachment = _context.QueryRequest.FirstOrDefault(q => q.QueryRequestId == id);

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

            var fileAttachment = _context.AgencyReport.FirstOrDefault(q => q.AgencyReportId == id);

            return fileAttachment != null ? File(fileAttachment.SupervisorAttachment, fileAttachment.SupervisorFileType, fileAttachment.SupervisorFileName + fileAttachment.SupervisorFileName) : null;
        }
    }
}
