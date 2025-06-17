using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IMailService
    {
        Task NotifyClaimCreation(string userEmail, InvestigationTask claimsInvestigation, string url = "");

        Task NotifyClaimAllocationToVendorAndManager(string userEmail, string policy, long claimsInvestigationId, long vendorId, string url = "");
        Task NotifyClaimAllocationToVendor(string userEmail, string policy, long claimsInvestigationId, long vendorId, string url = "");

        Task NotifyClaimAssignmentToAssigner(string userEmail, List<long> claims, string url = "");

        Task NotifyClaimWithdrawlToCompany(string senderUserEmail, long claimId, long vendorId, string url = "");
        Task NotifyClaimWithdrawlFromAgent(string senderUserEmail, long claimId, long vendorId, string url = "");

        Task NotifyClaimAssignmentToVendorAgent(string senderUserEmail, long claimId, string agentEmail, long vendorId, string url = "");

        Task NotifyClaimReportSubmitToVendorSupervisor(string senderUserEmail, long claimIdd, string url = "");

        Task NotifyClaimReportSubmitToCompany(string senderUserEmail, long claimId, string url = "");

        Task NotifyClaimReportProcess(string senderUserEmail, long claimId, string url = "");
        Task NotifySubmitQueryToAgency(string senderUserEmail, long claimId, string url = "");
        Task NotifySubmitReplyToCompany(string senderUserEmail, long claimId, string url = "");
        Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "");
        Task NotifyClaimDirectAssignmentToAssigner(string senderUserEmail, int autoAllocatedCases, int notAutoAllocatedCases, string url = "");
        Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url);
        Task NotifyFileUploadAutoAssign(string senderUserEmail, FileOnFileSystemModel file, string url);
    }

    public class MailService : IMailService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService smsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly UserManager<VendorApplicationUser> userVendorManager;
        private readonly IFeatureManager featureManager;

        public MailService(ApplicationDbContext context,
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
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();

            this.userManager = userManager;
            this.userVendorManager = userVendorManager;
        }
        public async Task NotifyFileUploadAutoAssign(string senderUserEmail, FileOnFileSystemModel file, string url)
        {
            try
            {
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                if (file.Completed.GetValueOrDefault())
                {
                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = applicationUser.ClientCompany,
                        Symbol = "fa fa-info i-blue",
                        Message = $"Assign of {file.RecordCount} cases finished",
                        Status = $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR}",
                        NotifierUserEmail = senderUserEmail
                    };
                    _context.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, ";
                        message += $"Assign of {file.RecordCount} cases finished ";
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
                        Message = $"Assign Error",
                        Status = $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR}",
                        NotifierUserEmail = senderUserEmail
                    };
                    _context.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, ";
                        message += $"JobId: {file.Id} Assign Error. ";
                        message += $"Thanks, ";
                        message += $"{applicationUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }

        }
        public async Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url)
        {
            try
            {
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                if (file.Completed.GetValueOrDefault())
                {
                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = applicationUser.ClientCompany,
                        Symbol = "fa fa-info i-blue",
                        Message = $"Upload of {file.RecordCount} cases finished",
                        Status = $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR}",
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
                        Status = $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR}",
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

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }

        }

        public async Task NotifyClaimAllocationToVendorAndManager(string userEmail, string policy, long claimsInvestigationId, long vendorId, string url = "")
        {
            try
            {
                var managerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.MANAGER.ToString()));
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == userEmail);
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

                var claimsInvestigation = _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefault(v => v.Id == claimsInvestigationId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);
                var managerNotification = new StatusNotification
                {
                    Role = managerRole,
                    Company = applicationUser.ClientCompany,
                    Symbol = "fa fa-info i-blue",
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(managerNotification);

                var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var userEmailToSend in userEmailsToSend)
                {

                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{policy} : {claimsInvestigation.SubStatus}, ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public async Task NotifyClaimAllocationToVendor(string userEmail, string policy, long claimsInvestigationId, long vendorId, string url = "")
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

                var claimsInvestigation = _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefault(v => v.Id == claimsInvestigationId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
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
                        message += $"Case #{policy} : {claimsInvestigation.SubStatus}, ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<long> claims, string url = "")
        {
            try
            {
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                var claimsInvestigations = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(v => claims.Contains(v.Id));

                foreach (var claimsInvestigation in claimsInvestigations)
                {
                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = applicationUser.ClientCompany,
                        Symbol = "fa fa-info i-blue",
                        Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                        Status = claimsInvestigation.SubStatus,
                        NotifierUserEmail = senderUserEmail
                    };

                    _context.Notifications.Add(notification);
                    //claimsInvestigation.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. ";
                        message += $"Thanks, ";
                        message += $"{applicationUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = applicationUser.Email, RecepicientPhone = applicationUser.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }


                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public async Task NotifyClaimDirectAssignmentToAssigner(string senderUserEmail, int autoAllocatedCasesCount, int notAutoAllocatedCasesCount, string url = "")
        {
            try
            {
                var assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                var notification = new StatusNotification
                {
                    Role = creatorRole,
                    Company = applicationUser.ClientCompany,
                    Symbol = "fa fa-info i-blue",
                    Message = $"Assigning of {autoAllocatedCasesCount + notAutoAllocatedCasesCount} cases finshed",
                    Status = $"{assigned}={autoAllocatedCasesCount}",
                    NotifierUserEmail = senderUserEmail
                };

                _context.Notifications.Add(notification);
                //SEND SMS
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {applicationUser.Email}, ";
                    message += $"Assigning finished of {autoAllocatedCasesCount + notAutoAllocatedCasesCount} cases, Auto-assigned count = {autoAllocatedCasesCount}. ";
                    message += $"Thanks, ";
                    message += $"{applicationUser.Email}, ";
                    message += $"{url}";
                    await smsService.DoSendSmsAsync(applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "")
        {
            try
            {
                var applicationUser = _context.ClientCompanyApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefault(c => c.Email == senderUserEmail);

                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));

                var notification = new StatusNotification
                {
                    Role = creatorRole,
                    Company = applicationUser.ClientCompany,
                    Symbol = "fa fa-info i-blue",
                    Message = $"Assigning of {autoAllocatedCases.Count + notAutoAllocatedCases.Count} cases finshed",
                    Status = $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER}={autoAllocatedCases.Count}",
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

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimWithdrawlToCompany(string senderUserEmail, long claimId, long vendorId, string url = "")
        {
            try
            {
                var claim = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefault(v => v.Id == claimId);

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
                    Status = claim.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(vendorNotification);

                var notification = new StatusNotification
                {
                    Role = creatorRole,
                    Company = company,
                    Symbol = "fa fa-times i-orangered",
                    Message = $"Case #{claim.PolicyDetail.ContractNumber}",
                    Status = claim.SubStatus,
                    NotifierUserEmail = claim.CreatedUser
                };
                _context.Notifications.Add(notification);
                //claim.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claim.PolicyDetail.ContractNumber} : {claim.SubStatus}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claim.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimAssignmentToVendorAgent(string userEmail, long claimId, string agentEmail, long vendorId, string url = "")
        {
            try
            {
                var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));

                var recepientUser = _context.VendorApplicationUser.Include(c => c.Country).FirstOrDefault(c => c.Email == agentEmail);

                var claimsInvestigation = _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefault(v => v.Id == claimId);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = agentRole,
                    Agency = claimsInvestigation.Vendor,
                    AgenctUserEmail = agentEmail,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);
                //claimsInvestigation.Notifications.Add(notification);

                var rows = await _context.SaveChangesAsync(null, false);
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {recepientUser.Email}, ";
                    message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}, ";
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

        public async Task NotifyClaimCreation(string userEmail, InvestigationTask claimsInvestigation, string url = "")
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

            try
            {
                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task NotifyClaimReportProcess(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = _context.Investigations
                .Include(i => i.PolicyDetail)
                .Include(i => i.Vendor)
                .FirstOrDefault(v => v.Id == claimId);
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
                    Symbol = claimsInvestigation.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ? "far fa-thumbs-up i-green" : "far fa-thumbs-down i-orangered",
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus
                };
                _context.Notifications.Add(vendorNotification);

                var notification = new StatusNotification
                {
                    Role = managerRole,
                    Company = company,
                    Symbol = claimsInvestigation.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ? "far fa-thumbs-up i-green" : "far fa-thumbs-down i-orangered",
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };

                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimReportSubmitToCompany(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = _context.Investigations
               .Include(i => i.Vendor)
               .Include(i => i.PolicyDetail)
               .FirstOrDefault(v => v.Id == claimId);
                var companyUsers = _context.ClientCompanyApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claimsInvestigation.ClientCompanyId);

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

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = assessorRole,
                    Company = company,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);
                //claimsInvestigation.Notifications.Add(notification);
                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimReportSubmitToVendorSupervisor(string senderUserEmail, long claimId, string url = "")
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

                var claimsInvestigation = _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefault(v => v.Id == claimId);
                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);


                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifySubmitQueryToAgency(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = _context.Investigations
               .Include(i => i.Vendor)
               .Include(i => i.PolicyDetail)
               .FirstOrDefault(v => v.Id == claimId);

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
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);

                foreach (var userEmailToSend in userEmailsToSend)
                {
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. ";
                        message += $"Thanks, ";
                        message += $"{clientCompanyUser.Email}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = userEmailToSend.Email, RecepicientPhone = userEmailToSend.PhoneNumber, Message = message });
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifySubmitReplyToCompany(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = _context.Investigations
                   .Include(i => i.PolicyDetail)
                   .FirstOrDefault(v => v.Id == claimId);
                var companyUsers = _context.ClientCompanyApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claimsInvestigation.ClientCompanyId);

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


                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = assessorRole,
                    Company = company,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claimsInvestigation.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task NotifyClaimWithdrawlFromAgent(string senderUserEmail, long claimId, long vendorId, string url = "")
        {
            try
            {
                var claim = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefault(v => v.Id == claimId);

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);

                var users = _context.VendorApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId && u.UserRole == AgencyRole.AGENCY_ADMIN || u.UserRole == AgencyRole.SUPERVISOR);

                var vendorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.SUPERVISOR.ToString()));

                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorId);
                var vendorNotification = new StatusNotification
                {
                    Role = vendorRole,
                    Agency = vendor,
                    Symbol = "fa fa-times i-orangered",
                    Message = $"Case #{claim.PolicyDetail.ContractNumber}",
                    Status = claim.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(vendorNotification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, ";
                        message += $"Case #{claim.PolicyDetail.ContractNumber} : {claim.SubStatus}, ";
                        message += $"Thanks, ";
                        message += $"{senderUserEmail}, ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.ISDCode + user.PhoneNumber, message);
                        //claim.SmsNotifications.Add(new SmsNotification { RecepicientEmail = user.Email, RecepicientPhone = user.PhoneNumber, Message = message });
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}