using System.Net.Http.Headers;

using Amazon.Auth.AccessControlPolicy;
using Amazon.Rekognition.Model;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using Newtonsoft.Json;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace risk.control.system.Services
{
    public interface IMailboxService
    {
        Task NotifyClaimCreation(string userEmail, ClaimsInvestigation claimsInvestigation);

        Task NotifyClaimAllocationToVendor(string userEmail, string policy, string claimsInvestigationId, long vendorId);
        Task NotifyClaimAllocationToVendor(string userEmail, string policy, long claimsInvestigationId, long vendorId);

        Task NotifyClaimAssignmentToAssigner(string userEmail, List<string> claims);

        Task NotifyClaimWithdrawlToCompany(string senderUserEmail, string claimId, long vendorId);

        Task NotifyClaimAssignmentToVendorAgent(string senderUserEmail, string claimId, string agentEmail, long vendorId);

        Task NotifyClaimReportSubmitToVendorSupervisor(string senderUserEmail, string claimIdd);

        Task NotifyClaimReportSubmitToCompany(string senderUserEmail, string claimId);

        Task NotifyClaimReportProcess(string senderUserEmail, string claimId);
        Task NotifySubmitQueryToAgency(string senderUserEmail, string claimId);
        Task NotifySubmitReplyToCompany(string senderUserEmail, string claimId);
    }

    public class MailboxService : IMailboxService
    {
        private const string TEST_PHONE = "61432854196";
        private static string BaseUrl = string.Empty;
        private static string smsBaseUrl = string.Empty;
        private static string AgencyBaseUrl = string.Empty;
        private string FilePath = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly ISmsService smsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly UserManager<VendorApplicationUser> userVendorManager;
        private readonly IFeatureManager featureManager;

        public MailboxService(ApplicationDbContext context,
            ISmsService SmsService,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ClientCompanyApplicationUser> userManager,
            IFeatureManager featureManager,
            UserManager<VendorApplicationUser> userVendorManager)
        {
            this._context = context;
            smsService = SmsService;
            this.httpContextAccessor = httpContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
            this.featureManager = featureManager;
            FilePath = Path.Combine(webHostEnvironment.WebRootPath, "Templates", "WelcomeTemplate.html");
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();

            BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}/InsuranceClaims/Summary4Insurer/";
            AgencyBaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}/InsuranceClaims/Summary4Agency/";
            this.userManager = userManager;
            this.userVendorManager = userVendorManager;

            smsBaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task NotifyClaimAllocationToVendor(string userEmail, string policy, string claimsInvestigationId, long vendorId)
        {
            try
            {
                var supervisorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.SUPERVISOR.ToString()));
                var agencyAdminRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENCY_ADMIN.ToString()));

                var vendorUsers = _context.VendorApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId);

                List<VendorApplicationUser> userEmailsToSend = new();

                foreach (var assignedUser in vendorUsers)
                {
                    var isAdmin = await userVendorManager.IsInRoleAsync(assignedUser, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                    var isSupervisor = await userVendorManager.IsInRoleAsync(assignedUser, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                }

                string claimsUrl = $"{AgencyBaseUrl + claimsInvestigationId}";

                var claimsInvestigation = _context.ClaimsInvestigation
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var userEmailToSend in userEmailsToSend)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == userEmailToSend.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Created = DateTime.Now,
                        Message = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png"),
                        Subject = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}:{claimsInvestigation.InvestigationCaseSubStatus.Name}.",
                        SenderEmail = userEmail,
                        Priority = ContactMessagePriority.URGENT,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = userEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        

                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{policy} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<string> claims)
        {
            var applicationUser = _context.ApplicationUser.Where(u => u.Email == senderUserEmail).FirstOrDefault();
            List<ClientCompanyApplicationUser> userEmailsToSend = new List<ClientCompanyApplicationUser>();

            var clientCompanyUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).FirstOrDefault(c => c.Email == applicationUser.Email);

            var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

            //var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assigner.ToString()));

            var companyUsers = _context.ClientCompanyApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

            foreach (var companyUser in companyUsers)
            {
                var isAssigner = await userManager.IsInRoleAsync(companyUser, creatorRole?.Name);
                if (isAssigner)
                {
                    userEmailsToSend.Add(companyUser);
                }
            }

            var claimsInvestigations = _context.ClaimsInvestigation
                .Include(i => i.PolicyDetail)
                .Include(i => i.InvestigationCaseSubStatus)
                .Where(v => claims.Contains(v.ClaimsInvestigationId));

            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

            foreach (var userEmailToSend in userEmailsToSend)
            {
                var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == userEmailToSend.Email);
                var contactMessage = new InboxMessage
                {
                    //ReceipientEmail = userEmailToSend,
                    Message = "Case(s) assigned ",
                    Created = DateTime.Now,
                    Subject = "Case(s) assigned:",
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
                    string claimsUrl = $"{BaseUrl + claimsInvestigation.ClaimsInvestigationId} ";
                    contactMessage.Subject += claimsInvestigation.PolicyDetail.ContractNumber + ", ";
                    contactMessage.Message += MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png")
                        ;
                    contactMessage.RawMessage += MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png")
                        ;

                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = clientCompanyUser.ClientCompany,
                        Symbol = "fa fa-info i-blue",
                        Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                        Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                        NotifierUserEmail = senderUserEmail
                    };

                    _context.Notifications.Add(notification);
                    claimsInvestigation.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}. ";
                        message += $"Thanks, ";
                        message += $"{applicationUser.Email}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }
                recepientMailbox?.Inbox.Add(contactMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);

            }
            try
            {
                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimWithdrawlToCompany(string senderUserEmail, string claimId, long vendorId)
        {
            try
            {
                var claim = _context.ClaimsInvestigation
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);

                var companyUsers = _context.ClientCompanyApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claim.ClientCompanyId);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));
                var vendorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.SUPERVISOR.ToString()));

                List<ClientCompanyApplicationUser> users = new List<ClientCompanyApplicationUser>();

                foreach (var companyUser in companyUsers)
                {
                    var isCeatorr = await userManager.IsInRoleAsync(companyUser, creatorRole?.Name);
                    if (isCeatorr)
                    {
                        users.Add(companyUser);
                    }
                }

                string claimsUrl = $"{BaseUrl + claimId}";

                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorId);
                var vendorNotification = new StatusNotification
                {
                    Role = vendorRole,
                    Agency = vendor,
                    Symbol = "fa fa-times i-orangered",
                    Message = $"Case #{claim.PolicyDetail.ContractNumber}",
                    Status = claim.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(vendorNotification);
                claim.Notifications.Add(vendorNotification);

                var notification = new StatusNotification
                {
                    Role = creatorRole,
                    Company = company,
                    Symbol = "fa fa-times i-orangered",
                    Message = $"Case #{claim.PolicyDetail.ContractNumber}",
                    Status = claim.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);
                claim.Notifications.Add(notification);

                foreach (var user in users)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == user.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Message = MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claim.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claim.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claim.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png"),
                        Created = DateTime.Now,
                        Subject = $"Case #{claim.PolicyDetail.ContractNumber}:{claim.InvestigationCaseSubStatus.Name}",
                        SenderEmail = senderUserEmail,
                        Priority = ContactMessagePriority.NORMAL,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = senderUserEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claim.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claim.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claim.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claim.PolicyDetail.ContractNumber} : {claim.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        claim.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimAssignmentToVendorAgent(string userEmail, string claimId, string agentEmail, long vendorId)
        {
            try
            {
                var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));

                var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == agentEmail);
                var recepientUser = _context.VendorApplicationUser.Include(c => c.Country).FirstOrDefault(c => c.Email == agentEmail);

                string claimsUrl = $"{AgencyBaseUrl + claimId}";

                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                var claimsInvestigation = _context.ClaimsInvestigation
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = agentRole,
                    Agency = claimsInvestigation.Vendor,
                    AgenctUserEmail = agentEmail,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                var contactMessage = new InboxMessage
                {
                    //ReceipientEmail = userEmailToSend,
                    Message = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png"),
                    Created = DateTime.Now,
                    Subject = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}:{claimsInvestigation.InvestigationCaseSubStatus.Name}.",
                    SenderEmail = userEmail,
                    Priority = ContactMessagePriority.URGENT,
                    SendDate = DateTime.Now,
                    Updated = DateTime.Now,
                    Read = false,
                    UpdatedBy = userEmail,
                    ReceipientEmail = recepientMailbox.Name,
                    RawMessage = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png")
                };
                recepientMailbox?.Inbox.Add(contactMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);

                var rows = await _context.SaveChangesAsync();
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {recepientUser.Email}, ";
                    message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                    message += $"Thanks, ";
                    message += $"{userEmail}";
                        message += $"{smsBaseUrl}";
                    await smsService.DoSendSmsAsync(recepientUser.Country.ISDCode + recepientUser.PhoneNumber, message);
                    claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = recepientUser.Email, RecepicientPhone = recepientUser.PhoneNumber, Message = message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
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
                var managerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.MANAGER.ToString()));

                var companyUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var companyUser in companyUsers)
                {
                    var userInmanagerRole = await userManager.IsInRoleAsync(companyUser, managerRole?.Name);
                    if (userInmanagerRole)
                    {
                        userEmailsToSend.Add(companyUser.Email);
                    }
                }
            }
            string claimsUrl = $"<a href={BaseUrl + claimsInvestigation.ClaimsInvestigationId}>url</a>";
            claimsUrl = "<html>" + Environment.NewLine + claimsUrl + Environment.NewLine + "</html>";

            var contactMessage = new InboxMessage
            {
                //ReceipientEmail = userEmailToSend,
                Message = "Claim created ",
                Created = DateTime.Now,
                RawMessage = claimsUrl,
                Subject = $"Claim created: case Id = {claimsInvestigation.ClaimsInvestigationId}",
                SenderEmail = clientCompanyUser?.Email ?? applicationUser.Email,
                Priority = ContactMessagePriority.NORMAL,
                SendDate = DateTime.Now,
                Updated = DateTime.Now,
                Read = false,
                UpdatedBy = applicationUser.Email
            };
            if (claimsInvestigation.PolicyDetail.Document is not null)
            {
                var messageDocumentFileName = Path.GetFileNameWithoutExtension(claimsInvestigation.PolicyDetail.Document.FileName);
                var extension = Path.GetExtension(claimsInvestigation.PolicyDetail.Document.FileName);
                contactMessage.Document = claimsInvestigation.PolicyDetail.Document;
                using var dataStream = new MemoryStream();
                await contactMessage.Document.CopyToAsync(dataStream);
                contactMessage.Attachment = dataStream.ToArray();
                contactMessage.FileType = claimsInvestigation.PolicyDetail.Document.ContentType;
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
            try
            {
                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task NotifyClaimReportProcess(string senderUserEmail, string claimId)
        {
            try
            {
                var claimsInvestigation = _context.ClaimsInvestigation
                .Include(i => i.PolicyDetail)
                .Include(i => i.Vendor)
                .Include(i => i.InvestigationCaseSubStatus)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);
                var companyUsers = _context.ClientCompanyApplicationUser
                                    .Include(u => u.ClientCompany)
                                    .Include(u => u.Country)
                                    .Where(u => u.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var managerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.MANAGER.ToString()));
                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                List<ApplicationUser> users = new List<ApplicationUser>();
                foreach (var user in companyUsers)
                {
                    var isManager = await userManager.IsInRoleAsync(user, managerRole?.Name);
                    if (isManager)
                    {
                        users.Add(user);
                    }
                    if (claimsInvestigation.IsReviewCase)
                    {
                        var isCreator = await userManager.IsInRoleAsync(user, creatorRole?.Name);
                        if (isCreator)
                        {
                            users.Add(user);
                        }
                    }
                }
                var vendorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENCY_ADMIN.ToString()));
                var vendorUsers = _context.VendorApplicationUser.Where(u => u.VendorId == claimsInvestigation.VendorId);

                foreach(var agencyUser in vendorUsers)
                {
                    var isAgencyUser = await userVendorManager.IsInRoleAsync(agencyUser, vendorRole?.Name);
                    if(isAgencyUser)
                    {
                        users.Add(agencyUser);
                    }
                }

                var vendorNotification = new StatusNotification
                {
                    Role = vendorRole,
                    Agency = claimsInvestigation.Vendor,
                    Symbol = claimsInvestigation.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ? "fa fa-check i-green" : "fa fa-times i-orangered",
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name
                };

                var notification = new StatusNotification
                {
                    Role = managerRole,
                    Company = company,
                    Symbol = claimsInvestigation.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ? "fa fa-check i-green" : "fa fa-times i-orangered",
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = senderUserEmail
                };

                _context.Notifications.Add(vendorNotification);
                claimsInvestigation.Notifications.Add(vendorNotification);
                _context.Notifications.Add(vendorNotification);

                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                string claimsUrl = $"{BaseUrl + claimId}";

                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                foreach (var user in users)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == user.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Message = "Claim process ",
                        Created = DateTime.Now,
                        Subject = "Claim Policy #:" + claimsInvestigation.PolicyDetail.ContractNumber,
                        SenderEmail = senderUserEmail,
                        Priority = ContactMessagePriority.NORMAL,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = senderUserEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw ex;
            }
        }

        public async Task NotifyClaimReportSubmitToCompany(string senderUserEmail, string claimId)
        {
            try
            {
                var claim = _context.ClaimsInvestigation.Include(i => i.PolicyDetail).Where(c => c.ClaimsInvestigationId == claimId).FirstOrDefault();
                var companyUsers = _context.ClientCompanyApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claim.ClientCompanyId);

                var assessorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.ASSESSOR.ToString()));

                List<ClientCompanyApplicationUser> users = new List<ClientCompanyApplicationUser>();
                foreach (var user in companyUsers)
                {
                    var isAssessor = await userManager.IsInRoleAsync(user, assessorRole?.Name);
                    if (isAssessor)
                    {
                        users.Add(user);
                    }
                }

                string claimsUrl = $"{BaseUrl + claimId}";

                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                var claimsInvestigation = _context.ClaimsInvestigation
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = assessorRole,
                    Company = company,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                foreach (var user in users)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == user.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Message = MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png"),
                        Created = DateTime.Now,
                        Subject = $"Case # {claimsInvestigation.PolicyDetail.ContractNumber}:{claimsInvestigation.InvestigationCaseSubStatus.Name}",
                        SenderEmail = senderUserEmail,
                        Priority = ContactMessagePriority.NORMAL,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = senderUserEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimReportSubmitToVendorSupervisor(string senderUserEmail, string claimId)
        {
            try
            {
                var supervisorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.SUPERVISOR.ToString()));
                var agencyAdminRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENCY_ADMIN.ToString()));

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == senderUserEmail);

                var vendorUsers = _context.VendorApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorUser.VendorId);

                List<VendorApplicationUser> users = new List<VendorApplicationUser>();

                foreach (var user in vendorUsers)
                {
                    var isAdmin = await userVendorManager.IsInRoleAsync(user, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        users.Add(user);
                    }
                    var isSupervisor = await userVendorManager.IsInRoleAsync(user, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        users.Add(user);
                    }
                }
                string claimsUrl = $"{AgencyBaseUrl + claimId}";
                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                var claimsInvestigation = _context.ClaimsInvestigation
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);


                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                foreach (var user in users)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == user.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Message = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png"),
                        Created = DateTime.Now,
                        Subject = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}:{claimsInvestigation.InvestigationCaseSubStatus.Name}",
                        SenderEmail = senderUserEmail,
                        Priority = ContactMessagePriority.NORMAL,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = senderUserEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifySubmitQueryToAgency(string senderUserEmail, string claimId)
        {
            try
            {
                var claimsInvestigation = _context.ClaimsInvestigation
               .Include(i => i.Vendor)
               .Include(i => i.PolicyDetail)
               .Include(i => i.InvestigationCaseSubStatus)
               .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);

                //1. get vendor admin and supervisor email

                var supervisorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.SUPERVISOR.ToString()));
                var agencyAdminRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENCY_ADMIN.ToString()));

                var vendorUsers = _context.VendorApplicationUser.Include(c => c.Country).Where(u => u.VendorId == claimsInvestigation.Vendor.VendorId);

                List<VendorApplicationUser> userEmailsToSend = new();

                foreach (var assignedUser in vendorUsers)
                {
                    var isAdmin = await userVendorManager.IsInRoleAsync(assignedUser, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                    var isSupervisor = await userVendorManager.IsInRoleAsync(assignedUser, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                }

                string claimsUrl = $"{AgencyBaseUrl + claimId}";

                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == senderUserEmail);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Symbol = "fa fa-question",
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                foreach (var userEmailToSend in userEmailsToSend)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == userEmailToSend.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Created = DateTime.Now,
                        Message = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png"),
                        Subject = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}:{claimsInvestigation.InvestigationCaseSubStatus.Name}.",
                        SenderEmail = senderUserEmail,
                        Priority = ContactMessagePriority.URGENT,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = senderUserEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email} ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifySubmitReplyToCompany(string senderUserEmail, string claimId)
        {
            try
            {
                var claim = _context.ClaimsInvestigation.Include(i => i.PolicyDetail).Where(c => c.ClaimsInvestigationId == claimId).FirstOrDefault();
                var companyUsers = _context.ClientCompanyApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claim.ClientCompanyId);

                var assessorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.ASSESSOR.ToString()));

                List<ClientCompanyApplicationUser> users = new List<ClientCompanyApplicationUser>();
                foreach (var user in companyUsers)
                {
                    var isAssessor = await userManager.IsInRoleAsync(user, assessorRole?.Name);
                    if (isAssessor)
                    {
                        users.Add(user);
                    }
                }

                string claimsUrl = $"{BaseUrl + claimId}";

                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                var claimsInvestigation = _context.ClaimsInvestigation
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimId);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = assessorRole,
                    Company = company,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                foreach (var user in users)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == user.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Message = MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png"),
                        Created = DateTime.Now,
                        Subject = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}:{claimsInvestigation.InvestigationCaseSubStatus.Name}.",
                        SenderEmail = senderUserEmail,
                        Priority = ContactMessagePriority.NORMAL,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = senderUserEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                        .Replace("[username]", recepientMailbox.Name)
                        .Replace("[email]", recepientMailbox.Name)
                        .Replace("[url]", claimsUrl)
                        .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                        .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                        .Replace("[logo]",
                        claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                        : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}. ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimAllocationToVendor(string userEmail, string policy, long claimsInvestigationId, long vendorId)
        {
            try
            {
                var supervisorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.SUPERVISOR.ToString()));
                var agencyAdminRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENCY_ADMIN.ToString()));

                var vendorUsers = _context.VendorApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId);

                List<VendorApplicationUser> userEmailsToSend = new();

                foreach (var assignedUser in vendorUsers)
                {
                    var isAdmin = await userVendorManager.IsInRoleAsync(assignedUser, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                    var isSupervisor = await userVendorManager.IsInRoleAsync(assignedUser, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                }

                string claimsUrl = $"{AgencyBaseUrl + claimsInvestigationId}";

                var claimsInvestigation = _context.CaseVerification
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .FirstOrDefault(v => v.CaseVerificationId == claimsInvestigationId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);
                claimsInvestigation.Notifications.Add(notification);
                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var userEmailToSend in userEmailsToSend)
                {
                    var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == userEmailToSend.Email);
                    var contactMessage = new InboxMessage
                    {
                        //ReceipientEmail = userEmailToSend,
                        Created = DateTime.Now,
                        Message = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png"),
                        Subject = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}:{claimsInvestigation.InvestigationCaseSubStatus.Name}.",
                        SenderEmail = userEmail,
                        Priority = ContactMessagePriority.URGENT,
                        SendDate = DateTime.Now,
                        Updated = DateTime.Now,
                        Read = false,
                        UpdatedBy = userEmail,
                        ReceipientEmail = recepientMailbox.Name,
                        RawMessage = MailText
                            .Replace("[username]", recepientMailbox.Name)
                            .Replace("[email]", recepientMailbox.Name)
                            .Replace("[url]", claimsUrl)
                            .Replace("[stage]", claimsInvestigation.InvestigationCaseSubStatus.Name)
                            .Replace("[policy]", claimsInvestigation.PolicyDetail.ContractNumber)
                            .Replace("[logo]",
                            claimsInvestigation.PolicyDetail?.DocumentImage != null ?
                            string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claimsInvestigation.PolicyDetail?.DocumentImage))
                            : "/img/no-image.png")
                    };
                    recepientMailbox?.Inbox.Add(contactMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{policy} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email}, ";
                        message += $"{smsBaseUrl}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }


                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}