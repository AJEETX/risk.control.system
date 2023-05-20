using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IMailboxService
    {
        Task NotifyClaimCreation(string userEmail, ClaimsInvestigation claimsInvestigation);
        Task NotifyClaimAssignment(string userEmail, List<string> claims);
    }
    public class MailboxService : IMailboxService
    {
        private static string BaseUrl = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;

        public MailboxService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<ClientCompanyApplicationUser> userManager)
        {
            this._context = context;
            this.httpContextAccessor = httpContextAccessor;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();

            BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}/ClaimsInvestigation/Details/";
            this.userManager = userManager;
        }

        public async Task NotifyClaimAssignment(string userEmail, List<string> claims)
        {
            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            List<string> userEmailsToSend = new();

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == applicationUser.Email);
            if (clientCompanyUser == null)
            {
                userEmailsToSend.Add(userEmail);
            }
            else
            {
                var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.ClientAssigner.ToString()));

                var assignerUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var assignedUser in assignerUsers)
                {
                    var isTrue = await userManager.IsInRoleAsync(assignedUser, assignerRole?.Name);
                    if (isTrue)
                    {
                        userEmailsToSend.Add(assignedUser.Email);
                    }
                }
            }

            var contactMessage = new InboxMessage
            {
                //ReceipientEmail = userEmailToSend,
                Created = DateTime.UtcNow,
                Message = "New case(s) assigned:",
                Subject = "New case(s) assigned:",
                SenderEmail = clientCompanyUser?.Email,
                Priority = ContactMessagePriority.NORMAL,
                SendDate = DateTime.Now,
                Updated = DateTime.Now,
                Read = false,
                UpdatedBy = applicationUser.Email
            };

            var claimsInvestigations = _context.ClaimsInvestigation.Where(v => claims.Contains(v.ClaimsInvestigationCaseId));
            foreach (var claimsInvestigation in claimsInvestigations)
            {
                contactMessage.Message += BaseUrl+ claimsInvestigation.ClaimsInvestigationCaseId;
            }


            foreach (var userEmailToSend in userEmailsToSend)
            {
                var recepientMailbox = _context.Mailbox.Include(m=>m.Inbox).FirstOrDefault(c => c.Name == userEmailToSend);
                contactMessage.ReceipientEmail = recepientMailbox.Name;
                recepientMailbox?.Inbox.Add(contactMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);
            }
            var rows = await _context.SaveChangesAsync();
        }

        public async Task NotifyClaimCreation(string userEmail, ClaimsInvestigation claimsInvestigation)
        {
            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            List<string> userEmailsToSend = new();

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == applicationUser.Email);
            if (clientCompanyUser == null)
            {
                userEmailsToSend.Add(userEmail);
            }
            else
            {
                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.ClientCreator.ToString()));

                var creatorUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var creatorUser in creatorUsers)
                {
                    var isTrue = await userManager.IsInRoleAsync(creatorUser, creatorRole?.Name);
                    if (isTrue)
                    {
                        userEmailsToSend.Add(creatorUser.Email);
                    }
                }
            }
            string claimsUrl = $"<a href={BaseUrl + claimsInvestigation.ClaimsInvestigationCaseId}>url</a>";
            claimsUrl = "<html>" + Environment.NewLine + claimsUrl + Environment.NewLine + "</html>";


            var contactMessage = new InboxMessage
            {
                //ReceipientEmail = userEmailToSend,
                Created = DateTime.UtcNow,
                Message = claimsUrl,
                Subject = "New case created: case Id = " + claimsUrl,
                SenderEmail = clientCompanyUser?.Email,
                Priority = ContactMessagePriority.NORMAL,
                SendDate = DateTime.Now,
                Updated = DateTime.Now,
                Read = false,
                UpdatedBy = applicationUser.Email
            };
            if (claimsInvestigation.Document is not null)
            {
                var messageDocumentFileName = Path.GetFileNameWithoutExtension(claimsInvestigation.Document.FileName);
                var extension = Path.GetExtension(claimsInvestigation.Document.FileName);
                contactMessage.Document = claimsInvestigation.Document;
                using var dataStream = new MemoryStream();
                await contactMessage.Document.CopyToAsync(dataStream);
                contactMessage.Attachment = dataStream.ToArray();
                contactMessage.FileType = claimsInvestigation.Document.ContentType;
                contactMessage.Extension = extension;
                contactMessage.AttachmentName = messageDocumentFileName;
            }

            foreach (var userEmailToSend in userEmailsToSend)
            {
                var recepientMailbox = _context.Mailbox.FirstOrDefault(c => c.Name == userEmailToSend);
                contactMessage.ReceipientEmail = recepientMailbox.Name;
                recepientMailbox?.Inbox.Add(contactMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);
            }
            var rows = await _context.SaveChangesAsync();
        }
    }
}