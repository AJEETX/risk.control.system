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
        Task NotifyClaimCreation(string userEmail, ClaimsInvestigation claimsInvestigation, string url = "");

        Task NotifyClaimAllocationToVendor(string userEmail, string policy, string claimsInvestigationId, long vendorId, string url = "");

        Task NotifyClaimAssignmentToAssigner(string userEmail, List<string> claims, string url = "");

        Task NotifyClaimWithdrawlToCompany(string senderUserEmail, string claimId, long vendorId, string url = "");

        Task NotifyClaimAssignmentToVendorAgent(string senderUserEmail, string claimId, string agentEmail, long vendorId, string url = "");

        Task NotifyClaimReportSubmitToVendorSupervisor(string senderUserEmail, string claimIdd, string url = "");

        Task NotifyClaimReportSubmitToCompany(string senderUserEmail, string claimId, string url = "");

        Task NotifyClaimReportProcess(string senderUserEmail, string claimId, string url = "");
        Task NotifySubmitQueryToAgency(string senderUserEmail, string claimId, string url = "");
        Task NotifySubmitReplyToCompany(string senderUserEmail, string claimId, string url = "");
        Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<string> autoAllocatedCases, List<string> notAutoAllocatedCases, string url = "");
        Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url);
    }

    public class MailboxService : IMailboxService
    {
        private const string TEST_PHONE = "61432854196";
        private static string BaseUrl = string.Empty;
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
        }

        public async Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url)
        {
            try
            {
                var created = _context.InvestigationCaseSubStatus.FirstOrDefault(
                                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                if(file.Completed.GetValueOrDefault())
                {
                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = applicationUser.ClientCompany,
                        Symbol = "fa fa-info i-blue",
                        Message = $"Upload of {file.RecordCount} cases finished",
                        Status = $"{created.Name}",
                        NotifierUserEmail = senderUserEmail
                    };
                    _context.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, ";
                        message += $"Upload of {file.RecordCount} cases finished ";
                        message += $"Thanks, ";
                        message += $"{applicationUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                    }
                }
                else
                {
                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = applicationUser.ClientCompany,
                        Symbol = "fa fa-times i-orangered",
                        Message = $"Upload Error",
                        Status = $"{created.Name}",
                        NotifierUserEmail = senderUserEmail
                    };
                    _context.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, ";
                        message += $"JobId: {file.Id} Upload Error. ";
                        message += $"Thanks, ";
                        message += $"{applicationUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
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
        public async Task NotifyClaimAllocationToVendor(string userEmail, string policy, string claimsInvestigationId, long vendorId, string url = "")
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

                //string claimsUrl = $"{AgencyBaseUrl + claimsInvestigationId}";

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
                //claimsInvestigation.Notifications.Add(notification);
                //StreamReader str = new StreamReader(FilePath);
                //string MailText = str.ReadToEnd();
                //str.Close();

                var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var userEmailToSend in userEmailsToSend)
                {
                   
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{policy} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
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

        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<string> claims, string url = "")
        {
            try
            {
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                var claimsInvestigations = _context.ClaimsInvestigation
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .Where(v => claims.Contains(v.ClaimsInvestigationId));

                foreach (var claimsInvestigation in claimsInvestigations)
                {
                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = applicationUser.ClientCompany,
                        Symbol = "fa fa-info i-blue",
                        Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                        Status = claimsInvestigation.InvestigationCaseSubStatus.Name,
                        NotifierUserEmail = senderUserEmail
                    };

                    _context.Notifications.Add(notification);
                    //claimsInvestigation.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}. ";
                        message += $"Thanks, ";
                        message += $"{applicationUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = applicationUser.Email, RecepicientPhone = applicationUser.PhoneNumber, Message = message });
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
        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<string> autoAllocatedCases, List<string> notAutoAllocatedCases, string url = "")
        {
            try
            {
                var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                var autoAllocatedCasesData = _context.ClaimsInvestigation
                    .Include(i => i.PolicyDetail)
                    .Include(i => i.InvestigationCaseSubStatus)
                    .Where(v => autoAllocatedCases.Contains(v.ClaimsInvestigationId));

                var notification = new StatusNotification
                {
                    Role = creatorRole,
                    Company = applicationUser.ClientCompany,
                    Symbol = "fa fa-info i-blue",
                    Message = $"Assigning of {autoAllocatedCases.Count + notAutoAllocatedCases.Count} cases finshed",
                    Status = $"{assigned.Name}={autoAllocatedCases.Count}",
                    NotifierUserEmail = senderUserEmail
                };

                _context.Notifications.Add(notification);
                //SEND SMS
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {applicationUser.Email}, ";
                    message += $"Assigning finished of {autoAllocatedCases.Count + notAutoAllocatedCases.Count} cases, Auto-assigned count = {autoAllocatedCases.Count}. ";
                    message += $"Thanks, ";
                    message += $"{applicationUser.Email}, ";
                    message += $"{url}";
                    await smsService.DoSendSmsAsync(applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                }

                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimWithdrawlToCompany(string senderUserEmail, string claimId, long vendorId, string url = "")
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
                //claim.Notifications.Add(vendorNotification);

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
                //claim.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claim.PolicyDetail.ContractNumber} : {claim.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claim.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
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

        public async Task NotifyClaimAssignmentToVendorAgent(string userEmail, string claimId, string agentEmail, long vendorId, string url = "")
        {
            try
            {
                var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));

                var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == agentEmail);
                var recepientUser = _context.VendorApplicationUser.Include(c => c.Country).FirstOrDefault(c => c.Email == agentEmail);

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
                //claimsInvestigation.Notifications.Add(notification);
                
                var rows = await _context.SaveChangesAsync();
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {recepientUser.Email}, ";
                    message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                    message += $"Thanks, ";
                    message += $"{userEmail}, ";
                    message += $"{url}";
                    await smsService.DoSendSmsAsync(recepientUser.Country.ISDCode + recepientUser.PhoneNumber, message);
                    //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = recepientUser.Email, RecepicientPhone = recepientUser.PhoneNumber, Message = message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimCreation(string userEmail, ClaimsInvestigation claimsInvestigation, string url = "")
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

        public async Task NotifyClaimReportProcess(string senderUserEmail, string claimId, string url = "")
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

                foreach (var agencyUser in vendorUsers)
                {
                    var isAgencyUser = await userVendorManager.IsInRoleAsync(agencyUser, vendorRole?.Name);
                    if (isAgencyUser)
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
                //claimsInvestigation.Notifications.Add(vendorNotification);
                _context.Notifications.Add(vendorNotification);

                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
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

        public async Task NotifyClaimReportSubmitToCompany(string senderUserEmail, string claimId, string url = "")
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
                //claimsInvestigation.Notifications.Add(notification);
                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}. ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
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

        public async Task NotifyClaimReportSubmitToVendorSupervisor(string senderUserEmail, string claimId, string url = "")
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
                
                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}. ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
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

        public async Task NotifySubmitQueryToAgency(string senderUserEmail, string claimId, string url = "")
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
                
                foreach (var userEmailToSend in userEmailsToSend)
                {
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}. ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
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

        public async Task NotifySubmitReplyToCompany(string senderUserEmail, string claimId, string url = "")
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
                
                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.InvestigationCaseSubStatus.Name}. ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
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
    }
}