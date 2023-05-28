using System.Web;

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
        Task NotifyClaimAllocationToVendor(string userEmail, string claimsInvestigationId, string vendorId, long caseLocationId);
        Task NotifyClaimAssignmentToAssigner(string userEmail, List<string> claims);
        Task NotifyClaimAssignmentToVendorAgent(string senderUserEmail, string claimId);
    }
    public class MailboxService : IMailboxService
    {
        private static string BaseUrl = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly UserManager<VendorApplicationUser> userVendorManager;

        public MailboxService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<ClientCompanyApplicationUser> userManager, UserManager<VendorApplicationUser> userVendorManager)
        {
            this._context = context;
            this.httpContextAccessor = httpContextAccessor;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();

            BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}/ClaimsInvestigation/Details/";
            this.userManager = userManager;
            this.userVendorManager = userVendorManager;
        }

        public async Task NotifyClaimAllocationToVendor(string userEmail, string claimsInvestigationId, string vendorId, long caseLocationId)
        {
            //1. get vendor admin and supervisor email 

            var supervisorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.VendorSupervisor.ToString()));

            var vendorUsers = _context.VendorApplicationUser.Where(u => u.VendorId == vendorId);

            List<string> userEmailsToSend = new();

            userEmailsToSend.Add(userEmail);
            foreach (var assignedUser in vendorUsers)
            {
                var isTrue = await userVendorManager.IsInRoleAsync(assignedUser, supervisorRole?.Name);
                if (isTrue)
                {
                    userEmailsToSend.Add(assignedUser.Email);
                }
            }

            foreach (var userEmailToSend in userEmailsToSend)
            {
                var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == userEmailToSend);
                var contactMessage = new InboxMessage
                {
                    //ReceipientEmail = userEmailToSend,
                    Created = DateTime.UtcNow,
                    Message = "New case allocated:" + claimsInvestigationId,
                    Subject = "New case allocated:" + claimsInvestigationId,
                    SenderEmail = userEmail,
                    Priority = ContactMessagePriority.URGENT,
                    SendDate = DateTime.Now,
                    Updated = DateTime.Now,
                    Read = false,
                    UpdatedBy = userEmail,
                    ReceipientEmail = recepientMailbox.Name
                };
                recepientMailbox?.Inbox.Add(contactMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);
            }
            
            var rows = await _context.SaveChangesAsync();
        }

        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<string> claims)
        {
            var applicationUser = _context.ApplicationUser.Where(u => u.Email == senderUserEmail).FirstOrDefault();
            List<string> userEmailsToSend = new();

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == applicationUser.Email);
            if (clientCompanyUser == null)
            {
                userEmailsToSend.Add(senderUserEmail);
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

            var claimsInvestigations = _context.ClaimsInvestigation.Where(v => claims.Contains(v.ClaimsInvestigationId));

            foreach (var userEmailToSend in userEmailsToSend)
            {
                var recepientMailbox = _context.Mailbox.Include(m=>m.Inbox).FirstOrDefault(c => c.Name == userEmailToSend);
                var contactMessage = new InboxMessage
                {
                    //ReceipientEmail = userEmailToSend,
                    Created = DateTime.UtcNow,
                    Message = "New case(s) assigned:",
                    Subject = "New case(s) assigned:",
                    SenderEmail = clientCompanyUser?.Email ?? applicationUser.Email,
                    Priority = ContactMessagePriority.NORMAL,
                    SendDate = DateTime.Now,
                    Updated = DateTime.Now,
                    Read = false,
                    UpdatedBy = applicationUser.Email,
                    ReceipientEmail = recepientMailbox.Name
                };
                foreach (var claimsInvestigation in claimsInvestigations)
                {
                    contactMessage.Message += BaseUrl + claimsInvestigation.ClaimsInvestigationId;
                    contactMessage.Subject += BaseUrl + claimsInvestigation.ClaimsInvestigationId;
                }
                recepientMailbox?.Inbox.Add(contactMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);
            }
            var rows = await _context.SaveChangesAsync();
        }

        public Task NotifyClaimAssignmentToVendorAgent(string userEmail, string claimId)
        {
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.VendorAgent.ToString()));

            var claim = _context.ClaimsInvestigation.Include(c=>c.Vendors).Where(c=>c.ClaimsInvestigationId.Equals(claimId)).FirstOrDefault();
            if (claim != null)
            {
            }

            throw new NotImplementedException();
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
            string claimsUrl = $"<a href={BaseUrl + claimsInvestigation.ClaimsInvestigationId}>url</a>";
            claimsUrl = "<html>" + Environment.NewLine + claimsUrl + Environment.NewLine + "</html>";

            //claimsUrl = Html.Raw( HttpUtility.HtmlEncode(claimsUrl));

            var contactMessage = new InboxMessage
            {
                //ReceipientEmail = userEmailToSend,
                Created = DateTime.UtcNow,
                Message = claimsUrl,
                Subject = "New case created: case Id = " + claimsUrl,
                SenderEmail = clientCompanyUser?.Email ?? applicationUser.Email,
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