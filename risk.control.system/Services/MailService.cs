using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
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

    internal class MailService : IMailService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ILogger<MailService> logger;
        private readonly ISmsService smsService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly UserManager<ApplicationUser> userVendorManager;
        private readonly IFeatureManager featureManager;

        public MailService(ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            ILogger<MailService> logger,
            ISmsService SmsService,
            UserManager<ApplicationUser> userManager,
            IFeatureManager featureManager,
            UserManager<ApplicationUser> userVendorManager)
        {
            this._context = context;
            this.roleManager = roleManager;
            this.logger = logger;
            smsService = SmsService;
            this.featureManager = featureManager;
            this.userManager = userManager;
            this.userVendorManager = userVendorManager;
        }
        public async Task NotifyFileUploadAutoAssign(string senderUserEmail, FileOnFileSystemModel file, string url)
        {
            try
            {
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

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
                        string message = $"Dear {applicationUser.Email}, \n";
                        message += $"Assign of {file.RecordCount} cases finished \n";
                        message += $"Thanks \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.Code, applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
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
                        string message = $"Dear {applicationUser.Email},\n ";
                        message += $"JobId: {file.Id} Assign Error. \n";
                        message += $"Thanks \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.Code, applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }

        }
        public async Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url)
        {
            try
            {
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

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
                        string message = $"Dear {applicationUser.Email},\n ";
                        message += $"Upload of {file.RecordCount} cases finished \n";
                        message += $"Thanks \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.Code, applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
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
                        Status = $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_ERR}",
                        NotifierUserEmail = senderUserEmail
                    };
                    _context.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email},\n ";
                        message += $"JobId: {file.Id} Upload Error. \n";
                        message += $"Thanks \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.Code, applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimAllocationToVendorAndManager(string userEmail, string policy, long claimsInvestigationId, long vendorId, string url = "")
        {
            try
            {
                var managerRole = await roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                var vendorUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId);

                List<ApplicationUser> userEmailsToSend = new();

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
                var claimsInvestigation = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == claimsInvestigationId);

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

                var clientCompanyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var userEmailToSend in userEmailsToSend)
                {

                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, \n";
                        message += $"Case #{policy} : {claimsInvestigation.SubStatus}, \n";
                        message += $"Thanks \n";
                        message += $"{clientCompanyUser.Email},\n ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.Code, userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimAllocationToVendor(string userEmail, string policy, long claimsInvestigationId, long vendorId, string url = "")
        {
            try
            {
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                var vendorUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId);

                List<ApplicationUser> userEmailsToSend = new();

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

                var claimsInvestigation = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == claimsInvestigationId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = claimsInvestigation.Vendor,
                    Message = $"Case #{claimsInvestigation.PolicyDetail.ContractNumber}",
                    Status = claimsInvestigation.SubStatus,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);

                var clientCompanyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var userEmailToSend in userEmailsToSend)
                {

                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, \n";
                        message += $"Case #{policy} : {claimsInvestigation.SubStatus}, \n";
                        message += $"Thanks \n";
                        message += $"{clientCompanyUser.Email},\n ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.Code, userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<long> claims, string url = "")
        {
            try
            {
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

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
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, \n";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. \n";
                        message += $"Thanks \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(applicationUser.Country.Code, applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                    }
                    //SMS ::END
                }
                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimDirectAssignmentToAssigner(string senderUserEmail, int autoAllocatedCasesCount, int notAutoAllocatedCasesCount, string url = "")
        {
            try
            {
                var assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

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
                    string message = $"Dear {applicationUser.Email}, \n";
                    message += $"Assigning finished of {autoAllocatedCasesCount + notAutoAllocatedCasesCount} cases, Auto-assigned count = {autoAllocatedCasesCount}. \n";
                    message += $"Thanks \n";
                    message += $"{url}";
                    await smsService.DoSendSmsAsync(applicationUser.Country.Code, applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "")
        {
            try
            {
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

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
                    string message = $"Dear {applicationUser.Email}, \n";
                    message += $"Assigning finished of {autoAllocatedCases.Count + notAutoAllocatedCases.Count} cases, Auto-assigned count = {autoAllocatedCases.Count}.\n ";
                    message += $"Thanks \n";
                    message += $"{url}";
                    await smsService.DoSendSmsAsync(applicationUser.Country.Code, applicationUser.Country.ISDCode + applicationUser.PhoneNumber, message);
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimWithdrawlToCompany(string senderUserEmail, long claimId, long vendorId, string url = "")
        {
            try
            {
                var claim = await _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == claimId);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claim.ClientCompanyId);

                var companyUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claim.ClientCompanyId);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);
                var vendorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);

                List<ApplicationUser> users = new List<ApplicationUser>();

                foreach (var companyUser in companyUsers)
                {
                    var isCeatorr = await userManager.IsInRoleAsync(companyUser, creatorRole?.Name);
                    if (isCeatorr)
                    {
                        users.Add(companyUser);
                    }
                }

                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == vendorId);
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

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{claim.PolicyDetail.ContractNumber} : {claim.SubStatus}, \n";
                        message += $"Thanks \n";
                        message += $"{senderUserEmail}, \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimAssignmentToVendorAgent(string userEmail, long claimId, string agentEmail, long vendorId, string url = "")
        {
            try
            {
                var agentRole = await roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);

                var recepientUser = await _context.ApplicationUser.Include(c => c.Country).FirstOrDefaultAsync(c => c.Email == agentEmail);

                var claimsInvestigation = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == claimId);
                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

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

                var rows = await _context.SaveChangesAsync(null, false);
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {recepientUser.Email}, \n";
                    message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}, \n";
                    message += $"Thanks \n";
                    message += $"{userEmail},\n ";
                    message += $"{url}";
                    await smsService.DoSendSmsAsync(recepientUser.Country.Code, recepientUser.Country.ISDCode + recepientUser.PhoneNumber, message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimCreation(string userEmail, InvestigationTask claimsInvestigation, string url = "")
        {
            var applicationUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            List<string> userEmailsToSend = new();

            var clientCompanyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == applicationUser.Email);
            if (clientCompanyUser == null)
            {
                userEmailsToSend.Add(userEmail);
            }
            else
            {
                var managerRole = await roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);

                var companyUsers = _context.ApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimReportProcess(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = await _context.Investigations
                .Include(i => i.PolicyDetail)
                .Include(i => i.Vendor)
                .FirstOrDefaultAsync(v => v.Id == claimId);
                var companyUsers = _context.ApplicationUser
                                    .Include(u => u.ClientCompany)
                                    .Include(u => u.Country)
                                    .Where(u => u.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var managerRole = await roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);
                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

                List<ApplicationUser> users = new List<ApplicationUser>();
                foreach (var user in companyUsers)
                {
                    var isManager = await userManager.IsInRoleAsync(user, managerRole?.Name);
                    if (isManager)
                    {
                        users.Add(user);
                    }
                }
                var vendorRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var vendorUsers = _context.ApplicationUser.Where(u => u.VendorId == claimsInvestigation.VendorId);

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
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}, \n";
                        message += $"Thanks \n";
                        message += $"{senderUserEmail}, \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimReportSubmitToCompany(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = await _context.Investigations
               .Include(i => i.Vendor)
               .Include(i => i.PolicyDetail)
               .FirstOrDefaultAsync(v => v.Id == claimId);
                var companyUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var assessorRole = await roleManager.FindByNameAsync(ASSESSOR.DISPLAY_NAME);

                List<ApplicationUser> users = new List<ApplicationUser>();
                foreach (var user in companyUsers)
                {
                    var isAssessor = await userManager.IsInRoleAsync(user, assessorRole?.Name);
                    if (isAssessor)
                    {
                        users.Add(user);
                    }
                }

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

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
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. \n";
                        message += $"Thanks \n";
                        message += $"{senderUserEmail},\n ";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimReportSubmitToVendorSupervisor(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == senderUserEmail);

                var vendorUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorUser.VendorId);

                List<ApplicationUser> users = new List<ApplicationUser>();

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

                var claimsInvestigation = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == claimId);
                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);


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
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. \n";
                        message += $"Thanks \n";
                        message += $"{senderUserEmail}, \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifySubmitQueryToAgency(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = await _context.Investigations
               .Include(i => i.Vendor)
               .Include(i => i.PolicyDetail)
               .FirstOrDefaultAsync(v => v.Id == claimId);

                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                var vendorUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == claimsInvestigation.Vendor.VendorId);

                List<ApplicationUser> userEmailsToSend = new();

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

                var clientCompanyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);

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
                        string message = $"Dear {userEmailToSend.Email}, \n";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. \n";
                        message += $"Thanks \n";
                        message += $"{clientCompanyUser.Email}, \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(userEmailToSend.Country.Code, userEmailToSend.Country.ISDCode + userEmailToSend.PhoneNumber, message);
                    }
                    //SMS ::END
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifySubmitReplyToCompany(string senderUserEmail, long claimId, string url = "")
        {
            try
            {
                var claimsInvestigation = await _context.Investigations
                   .Include(i => i.PolicyDetail)
                   .FirstOrDefaultAsync(v => v.Id == claimId);
                var companyUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == claimsInvestigation.ClientCompanyId);

                var assessorRole = await roleManager.FindByNameAsync(ASSESSOR.DISPLAY_NAME);

                List<ApplicationUser> users = new List<ApplicationUser>();
                foreach (var user in companyUsers)
                {
                    var isAssessor = await userManager.IsInRoleAsync(user, assessorRole?.Name);
                    if (isAssessor)
                    {
                        users.Add(user);
                    }
                }

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

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
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{claimsInvestigation.PolicyDetail.ContractNumber} : {claimsInvestigation.SubStatus}. \n";
                        message += $"Thanks \n";
                        message += $"{senderUserEmail}, \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }

        public async Task NotifyClaimWithdrawlFromAgent(string senderUserEmail, long claimId, long vendorId, string url = "")
        {
            try
            {
                var claim = await _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == claimId);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claim.ClientCompanyId);

                var users = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId && u.Role == AppRoles.AGENCY_ADMIN || u.Role == AppRoles.SUPERVISOR);

                var vendorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);

                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == vendorId);
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
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{claim.PolicyDetail.ContractNumber} : {claim.SubStatus}, \n";
                        message += $"Thanks \n";
                        message += $"{senderUserEmail}, \n";
                        message += $"{url}";
                        await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, message);
                    }
                }

                var rows = await _context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }
    }
}