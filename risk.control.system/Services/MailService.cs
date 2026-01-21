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
        Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url);

        Task NotifyCaseAllocationToVendorAndManager(string userEmail, string policy, long caseId, long vendorId, string url = "");
        Task NotifyCaseAllocationToVendor(string userEmail, string policy, long caseId, long vendorId, string url = "");
        Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "");
        Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> caseIds, string url = "");

        Task NotifyCaseWithdrawlToCompany(string senderUserEmail, long caseId, long vendorId, string url = "");
        Task NotifyCaseWithdrawlFromAgent(string senderUserEmail, long caseId, long vendorId, string url = "");

        Task NotifyCaseAssignmentToVendorAgent(string senderUserEmail, long caseId, string agentEmail, long vendorId, string url = "");

        Task NotifyCaseReportSubmitToVendorSupervisor(string senderUserEmail, long caseId, string url = "");

        Task NotifyCaseReportSubmitToCompany(string senderUserEmail, long caseId, string url = "");

        Task NotifyCaseReportProcess(string senderUserEmail, long caseId, string url = "");
        Task NotifySubmitQueryToAgency(string senderUserEmail, long caseId, string url = "");
        Task NotifySubmitReplyToCompany(string senderUserEmail, long caseId, string url = "");
    }

    internal class MailService : IMailService
    {
        private const string BlueSymbol = "fa fa-info i-blue";
        private const string WarningSymbol = "fa fa-times i-orangered";
        private const string ErrorMessage = "Error sending message";
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ILogger<MailService> logger;
        private readonly ISmsService smsService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IFeatureManager featureManager;

        public MailService(ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            ILogger<MailService> logger,
            ISmsService SmsService,
            UserManager<ApplicationUser> userManager,
            IFeatureManager featureManager)
        {
            this._context = context;
            this.roleManager = roleManager;
            this.logger = logger;
            smsService = SmsService;
            this.featureManager = featureManager;
            this.userManager = userManager;
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
                        Symbol = BlueSymbol,
                        Message = $"Upload of {file.RecordCount} cases finished",
                        Status = $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR}",
                        NotifierUserEmail = senderUserEmail
                    };
                    _context.Notifications.Add(notification);
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
                        Symbol = WarningSymbol,
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseAllocationToVendorAndManager(string userEmail, string policy, long caseId, long vendorId, string url = "")
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
                    var isAdmin = await userManager.IsInRoleAsync(assignedUser, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                    var isSupervisor = await userManager.IsInRoleAsync(assignedUser, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                }
                var caseTask = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = caseTask.Vendor,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);
                var managerNotification = new StatusNotification
                {
                    Role = managerRole,
                    Company = applicationUser.ClientCompany,
                    Symbol = BlueSymbol,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(managerNotification);

                var clientCompanyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

                foreach (var userEmailToSend in userEmailsToSend)
                {

                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, \n";
                        message += $"Case #{policy} : {caseTask.SubStatus}, \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseAllocationToVendor(string userEmail, string policy, long caseId, long vendorId, string url = "")
        {
            try
            {
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                var vendorUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId);

                List<ApplicationUser> userEmailsToSend = new();

                foreach (var assignedUser in vendorUsers)
                {
                    var isAdmin = await userManager.IsInRoleAsync(assignedUser, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                    var isSupervisor = await userManager.IsInRoleAsync(assignedUser, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                }

                var caseTask = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = caseTask.Vendor,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = userEmail
                };
                _context.Notifications.Add(notification);

                var clientCompanyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

                foreach (var userEmailToSend in userEmailsToSend)
                {

                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, \n";
                        message += $"Case #{policy} : {caseTask.SubStatus}, \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> caseIds, string url = "")
        {
            try
            {
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

                var caseTasks = _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .Where(v => caseIds.Contains(v.Id));

                foreach (var caseTask in caseTasks)
                {
                    var notification = new StatusNotification
                    {
                        Role = creatorRole,
                        Company = applicationUser.ClientCompany,
                        Symbol = BlueSymbol,
                        Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                        Status = caseTask.SubStatus,
                        NotifierUserEmail = senderUserEmail
                    };

                    _context.Notifications.Add(notification);
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {applicationUser.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}. \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "")
        {
            try
            {
                var applicationUser = await _context.ApplicationUser.Include(i => i.ClientCompany).Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

                var notification = new StatusNotification
                {
                    Role = creatorRole,
                    Company = applicationUser.ClientCompany,
                    Symbol = BlueSymbol,
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseWithdrawlToCompany(string senderUserEmail, long caseId, long vendorId, string url = "")
        {
            try
            {
                var caseTask = await _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                var companyUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == caseTask.ClientCompanyId);

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
                    Symbol = WarningSymbol,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(vendorNotification);

                var notification = new StatusNotification
                {
                    Role = creatorRole,
                    Company = company,
                    Symbol = WarningSymbol,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = caseTask.CreatedUser
                };
                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}, \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseAssignmentToVendorAgent(string senderUserEmail, long caseId, string agentEmail, long vendorId, string url = "")
        {
            try
            {
                var agentRole = await roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);

                var recepientUser = await _context.ApplicationUser.Include(c => c.Country).FirstOrDefaultAsync(c => c.Email == agentEmail);

                var caseTask = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);
                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = agentRole,
                    Agency = caseTask.Vendor,
                    AgenctUserEmail = agentEmail,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);

                var rows = await _context.SaveChangesAsync(null, false);
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {recepientUser.Email}, \n";
                    message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}, \n";
                    message += $"Thanks \n";
                    message += $"{senderUserEmail},\n ";
                    message += $"{url}";
                    await smsService.DoSendSmsAsync(recepientUser.Country.Code, recepientUser.Country.ISDCode + recepientUser.PhoneNumber, message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseReportProcess(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                var caseTask = await _context.Investigations
                .Include(i => i.PolicyDetail)
                .Include(i => i.Vendor)
                .FirstOrDefaultAsync(v => v.Id == caseId);
                var companyUsers = _context.ApplicationUser
                                    .Include(u => u.ClientCompany)
                                    .Include(u => u.Country)
                                    .Where(u => u.ClientCompanyId == caseTask.ClientCompanyId);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

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
                var agencyUsers = _context.ApplicationUser.Where(u => u.VendorId == caseTask.VendorId);

                foreach (var agencyUser in agencyUsers)
                {
                    var isAgencyUser = await userManager.IsInRoleAsync(agencyUser, vendorRole?.Name);
                    if (isAgencyUser)
                    {
                        users.Add(agencyUser);
                    }
                }

                var vendorNotification = new StatusNotification
                {
                    Role = vendorRole,
                    Agency = caseTask.Vendor,
                    Symbol = caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ? "far fa-thumbs-up i-green" : "far fa-thumbs-down i-orangered",
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus
                };
                _context.Notifications.Add(vendorNotification);

                var notification = new StatusNotification
                {
                    Role = managerRole,
                    Company = company,
                    Symbol = caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ? "far fa-thumbs-up i-green" : "far fa-thumbs-down i-orangered",
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };

                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}, \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseReportSubmitToCompany(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                var caseTask = await _context.Investigations
               .Include(i => i.Vendor)
               .Include(i => i.PolicyDetail)
               .FirstOrDefaultAsync(v => v.Id == caseId);
                var companyUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == caseTask.ClientCompanyId);

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

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = assessorRole,
                    Company = company,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);
                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}. \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseReportSubmitToVendorSupervisor(string senderUserEmail, long caseId, string url = "")
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
                    var isAdmin = await userManager.IsInRoleAsync(user, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        users.Add(user);
                    }
                    var isSupervisor = await userManager.IsInRoleAsync(user, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        users.Add(user);
                    }
                }

                var caseTask = await _context.Investigations
                    .Include(i => i.Vendor)
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);
                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);


                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = caseTask.Vendor,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}. \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifySubmitQueryToAgency(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                var caseTask = await _context.Investigations
               .Include(i => i.Vendor)
               .Include(i => i.PolicyDetail)
               .FirstOrDefaultAsync(v => v.Id == caseId);

                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                var vendorUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == caseTask.Vendor.VendorId);

                List<ApplicationUser> userEmailsToSend = new();

                foreach (var assignedUser in vendorUsers)
                {
                    var isAdmin = await userManager.IsInRoleAsync(assignedUser, agencyAdminRole?.Name);
                    if (isAdmin)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                    var isSupervisor = await userManager.IsInRoleAsync(assignedUser, supervisorRole?.Name);
                    if (isSupervisor)
                    {
                        userEmailsToSend.Add(assignedUser);
                    }
                }

                var clientCompanyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                var notification = new StatusNotification
                {
                    Role = supervisorRole,
                    Agency = caseTask.Vendor,
                    Symbol = "fa fa-question",
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);

                foreach (var userEmailToSend in userEmailsToSend)
                {
                    //SEND SMS
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {userEmailToSend.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}. \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifySubmitReplyToCompany(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                var caseTask = await _context.Investigations
                   .Include(i => i.PolicyDetail)
                   .FirstOrDefaultAsync(v => v.Id == caseId);
                var companyUsers = _context.ApplicationUser.Include(c => c.Country).Where(u => u.ClientCompanyId == caseTask.ClientCompanyId);

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

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                var notification = new StatusNotification
                {
                    Role = assessorRole,
                    Company = company,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(notification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}. \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }

        public async Task NotifyCaseWithdrawlFromAgent(string senderUserEmail, long caseId, long vendorId, string url = "")
        {
            try
            {
                var caseTask = await _context.Investigations
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                var users = _context.ApplicationUser.Include(c => c.Country).Where(u => u.VendorId == vendorId && u.Role == AppRoles.AGENCY_ADMIN || u.Role == AppRoles.SUPERVISOR);

                var vendorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);

                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == vendorId);
                var vendorNotification = new StatusNotification
                {
                    Role = vendorRole,
                    Agency = vendor,
                    Symbol = WarningSymbol,
                    Message = $"Case #{caseTask.PolicyDetail.ContractNumber}",
                    Status = caseTask.SubStatus,
                    NotifierUserEmail = senderUserEmail
                };
                _context.Notifications.Add(vendorNotification);

                foreach (var user in users)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        string message = $"Dear {user.Email}, \n";
                        message += $"Case #{caseTask.PolicyDetail.ContractNumber} : {caseTask.SubStatus}, \n";
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
                logger.LogError(ex, ErrorMessage);
            }
        }
    }
}