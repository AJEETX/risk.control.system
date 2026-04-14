using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface ICaseNotificationService
    {
        Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url);

        Task NotifyCaseAllocationToVendorAndManager(string userEmail, string policy, long caseId, long vendorId, string url = "");

        Task NotifyCaseAllocationToVendor(string userEmail, string policy, long caseId, long vendorId, string url = "");

        Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "");

        Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> caseIds, string url = "");

        Task NotifyCaseWithdrawlByCompany(string senderUserEmail, string policyNumber, long caseId, long vendorId, string url = "");

        Task NotifyCaseDeclineByAgency(string senderUserEmail, string policyNumber, long caseId, long vendorId, string url = "");

        Task NotifyCaseWithdrawlFromAgent(string senderUserEmail, long caseId, long vendorId, string url = "");

        Task NotifyCaseAssignmentToVendorAgent(string senderUserEmail, long caseId, string agentEmail, long vendorId, string url = "");

        Task NotifyCaseReportSubmitToVendorSupervisor(string senderUserEmail, long caseId, string url = "");

        Task NotifyCaseReportSubmitToCompany(string senderUserEmail, long caseId, string url = "");

        Task NotifyCaseReportProcess(string senderUserEmail, long caseId, string url = "");

        Task NotifySubmitQueryToAgency(string senderUserEmail, long caseId, string url = "");

        Task NotifySubmitReplyToCompany(string senderUserEmail, long caseId, string url = "");
    }

    internal class CaseNotificationService(IDbContextFactory<ApplicationDbContext> contextFactory,
        RoleManager<ApplicationRole> roleManager,
        ILogger<CaseNotificationService> logger,
        ISmsService SmsService,
        IFeatureManager featureManager) : ICaseNotificationService
    {
        private const string BlueSymbol = "fa fa-info i-blue";
        private const string WarningSymbol = "fa fa-times i-orangered";
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly ILogger<CaseNotificationService> _logger = logger;
        private readonly ISmsService _smsService = SmsService;
        private readonly IFeatureManager _featureManager = featureManager;

        public async Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                var applicationUser = await _context.ApplicationUser.AsNoTracking().Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);
                if (applicationUser == null) return;
                var creatorRole = await _roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);
                bool isCompleted = file.Completed.GetValueOrDefault();
                string statusMsg = isCompleted ? $"Upload of {file.RecordCount} cases finished" : $"JobId: {file.Id} Upload Error";
                string dbStatus = isCompleted ? CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR : CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_ERR;
                var smsRecipients = new List<ApplicationUser> { applicationUser };
                await SendNotificationInternal(0, senderUserEmail, creatorRole!.Id, applicationUser.ClientCompanyId, null, isCompleted ? BlueSymbol : WarningSymbol, dbStatus, statusMsg, url, smsRecipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File Upload Notification Error for {UserEmail}", senderUserEmail);
            }
        }

        public async Task NotifyCaseAllocationToVendorAndManager(string userEmail, string policy, long caseId, long vendorId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                userEmail = userEmail.Replace("\n", "").Replace("\r", "").Trim();
                var applicationUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
                if (applicationUser == null || caseTask == null) return;
                var managerRole = await _roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);
                var supervisorRole = await _roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var vendorRecipients = await GetUsersByRoleAsync(companyId: null, vendorId, new[] { agencyAdminRole!.Id, supervisorRole!.Id });
                await SendNotificationInternal(caseId, userEmail, supervisorRole.Id, null, vendorId, null!, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, vendorRecipients);
                await SendNotificationInternal(caseId, userEmail, managerRole!.Id, applicationUser.ClientCompanyId, null, BlueSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allocation Notification Error for Case: {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseAllocationToVendor(string userEmail, string policy, long caseId, long vendorId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                userEmail = userEmail.Replace("\n", "").Replace("\r", "").Trim();
                var senderUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
                if (caseTask == null || senderUser == null) return;
                var supervisorRole = await _roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var recipients = await GetUsersByRoleAsync(companyId: null, vendorId: vendorId, roleIds: new[] { agencyAdminRole!.Id, supervisorRole!.Id });
                await SendNotificationInternal(caseId, userEmail, supervisorRole.Id, null, vendorId, BlueSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allocation Notification Error for User: {UserEmail}", userEmail);
            }
        }

        public async Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> caseIds, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                var applicationUser = await _context.ApplicationUser.AsNoTracking().Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);
                if (applicationUser == null) return;
                var creatorRole = await _roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);
                var caseTasks = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).Where(v => caseIds.Contains(v.Id)).ToListAsync();
                var notificationTasks = caseTasks.Select(caseTask => SendNotificationInternal(caseTask.Id, senderUserEmail, creatorRole?.Id, applicationUser.ClientCompanyId, null, BlueSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, smsRecipients: new List<ApplicationUser> { applicationUser }));
                await Task.WhenAll(notificationTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch Assignment Notification Error for {UserEmail}", senderUserEmail);
            }
        }

        public async Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                var applicationUser = await _context.ApplicationUser.AsNoTracking().Include(i => i.Country).FirstOrDefaultAsync(c => c.Email == senderUserEmail);
                if (applicationUser == null)
                {
                    _logger.LogWarning("Notification failed: User {Email} not found.", senderUserEmail);
                    return;
                }
                var creatorRole = await _roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);
                int totalCases = (autoAllocatedCases?.Count ?? 0) + (notAutoAllocatedCases?.Count ?? 0);
                int autoCount = autoAllocatedCases?.Count ?? 0;
                await SendNotificationInternal(0, senderUserEmail, creatorRole?.Id, applicationUser.ClientCompanyId, null, BlueSymbol, $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER}={autoCount}", $"Assigning of {totalCases} cases finished", url, smsRecipients: new List<ApplicationUser> { applicationUser });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification Error for {UserEmail}", senderUserEmail);
            }
        }

        public async Task NotifyCaseWithdrawlByCompany(string senderUserEmail, string policyNumber, long caseId, long vendorId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().FirstOrDefaultAsync(v => v.Id == caseId);
                var creatorRole = await _roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var recipients = await _context.ApplicationUser.AsNoTracking().Include(u => u.Country).Where(u => u.ClientCompanyId == caseTask!.ClientCompanyId).Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == creatorRole!.Id)).ToListAsync();
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                var agencyAdmin = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(v => v.VendorId == vendorId && v.IsVendorAdmin);
                await SendNotificationInternal(caseId, senderUserEmail, creatorRole!.Id, caseTask!.ClientCompanyId, null, WarningSymbol, caseTask.SubStatus, $"Case #{policyNumber}", url, recipients);
                await SendNotificationInternal(caseId, agencyAdmin!.Email!, agencyAdminRole!.Id, null, vendorId, WarningSymbol, caseTask.SubStatus, $"Case #{policyNumber}", url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Withdrawal Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseDeclineByAgency(string senderUserEmail, string policyNumber, long caseId, long vendorId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().FirstOrDefaultAsync(v => v.Id == caseId);
                var creatorRole = await _roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var recipients = await _context.ApplicationUser.AsNoTracking().Include(u => u.Country).Where(u => u.ClientCompanyId == caseTask!.ClientCompanyId).Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == creatorRole!.Id)).ToListAsync();
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                await SendNotificationInternal(caseId, senderUserEmail, agencyAdminRole!.Id, null, vendorId, WarningSymbol, caseTask!.SubStatus, $"Case #{policyNumber}", url, recipients);
                await SendNotificationInternal(caseId, caseTask.CreatedUser!, creatorRole!.Id, caseTask!.ClientCompanyId, null, WarningSymbol, caseTask.SubStatus, $"Case #{policyNumber}", url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decline Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseAssignmentToVendorAgent(string senderUserEmail, long caseId, string agentEmail, long vendorId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                var recipientUser = await _context.ApplicationUser.AsNoTracking().Include(c => c.Country).FirstOrDefaultAsync(c => c.Email == agentEmail);
                if (caseTask == null || recipientUser == null)
                {
                    _logger.LogWarning("Assignment notification aborted: Case {CaseId} or Agent {Email} not found.", caseId, agentEmail);
                    return;
                }
                var agentRole = await _roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                agentEmail = agentEmail.Replace("\n", "").Replace("\r", "").Trim();
                await SendNotificationInternal(caseId, senderUserEmail, agentRole?.Id, null, vendorId, BlueSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, smsRecipients: new List<ApplicationUser> { recipientUser }, agentEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification Error for Agent {AgentEmail} from {Sender}", agentEmail, senderUserEmail);
            }
        }

        public async Task NotifyCaseReportProcess(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                if (caseTask == null) return;
                var managerRole = await _roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                string statusSymbol = caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR ? "far fa-thumbs-up i-green" : "far fa-thumbs-down i-orangered";
                var recipients = await GetUsersByRoleAsync(companyId: caseTask.ClientCompanyId, vendorId: caseTask.VendorId, roleIds: new[] { managerRole!.Id, agencyAdminRole!.Id });
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                await SendNotificationInternal(caseId, senderUserEmail, agencyAdminRole?.Id, null, caseTask.VendorId, statusSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, smsRecipients: recipients.Where(u => u.VendorId == caseTask.VendorId).ToList());
                await SendNotificationInternal(caseId, senderUserEmail, managerRole?.Id, caseTask.ClientCompanyId, null, statusSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, smsRecipients: recipients.Where(u => u.ClientCompanyId == caseTask.ClientCompanyId).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report Process Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseReportSubmitToCompany(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                if (caseTask == null) return;
                var assessorRole = await _roleManager.FindByNameAsync(ASSESSOR.DISPLAY_NAME);
                var recipients = await GetUsersByRoleAsync(companyId: caseTask.ClientCompanyId, vendorId: null, roleIds: new[] { assessorRole!.Id });
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                await SendNotificationInternal(caseId, senderUserEmail, assessorRole?.Id, caseTask.ClientCompanyId, null, BlueSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report Submission Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseReportSubmitToVendorSupervisor(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                var senderUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == senderUserEmail);
                if (caseTask == null || senderUser == null)
                {
                    _logger.LogWarning("Notification aborted: Case {CaseId} or Sender {Email} not found.", caseId, senderUserEmail);
                    return;
                }
                var supervisorRole = await _roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var recipients = await GetUsersByRoleAsync(companyId: null, vendorId: senderUser.VendorId, roleIds: new[] { supervisorRole!.Id, agencyAdminRole!.Id });
                await SendNotificationInternal(caseId, senderUserEmail, supervisorRole?.Id, null, senderUser.VendorId, BlueSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vendor Supervisor Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifySubmitQueryToAgency(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                var senderUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == senderUserEmail);
                if (caseTask == null || senderUser == null) return;
                var supervisorRole = await _roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var recipients = await GetUsersByRoleAsync(companyId: null, vendorId: caseTask.VendorId, roleIds: new[] { supervisorRole!.Id, agencyAdminRole!.Id });
                await SendNotificationInternal(caseId, senderUserEmail, supervisorRole?.Id, null, caseTask.VendorId, "fa fa-question", caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifySubmitReplyToCompany(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().FirstOrDefaultAsync(v => v.Id == caseId);
                var assessorRole = await _roleManager.FindByNameAsync(ASSESSOR.DISPLAY_NAME);
                var recipients = await _context.ApplicationUser.AsNoTracking().Include(u => u.Country).Where(u => u.ClientCompanyId == caseTask!.ClientCompanyId)
                    .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == assessorRole!.Id)).ToListAsync();
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                await SendNotificationInternal(caseId, senderUserEmail, assessorRole!.Id, caseTask!.ClientCompanyId, null, BlueSymbol, caseTask.SubStatus, $"Reply Submitted for Case #{caseId}", url, recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enquiry Reply Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseWithdrawlFromAgent(string senderUserEmail, long caseId, long vendorId, string url = "")
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var caseTask = await _context.Investigations.AsNoTracking().Include(i => i.PolicyDetail).FirstOrDefaultAsync(v => v.Id == caseId);
                if (caseTask == null) return;
                var supervisorRole = await _roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await _roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);
                var recipients = await GetUsersByRoleAsync(null, vendorId, supervisorRole!.Id, agencyAdminRole!.Id);
                senderUserEmail = senderUserEmail.Replace("\n", "").Replace("\r", "").Trim();
                await SendNotificationInternal(caseId, senderUserEmail, supervisorRole.Id, null, vendorId, WarningSymbol, caseTask.SubStatus, $"Case #{caseTask.PolicyDetail?.ContractNumber}", url, recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Withdrawal Notification Error for Case {CaseId}", caseId);
            }
        }

        private async Task<List<ApplicationUser>> GetUsersByRoleAsync(long? companyId, long? vendorId, params long[] roleIds)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ApplicationUser.AsNoTracking().Include(u => u.Country).Where(u => (companyId.HasValue && u.ClientCompanyId == companyId) ||
                            (vendorId.HasValue && u.VendorId == vendorId)).Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId))).ToListAsync();
        }

        private async Task SendNotificationInternal(long caseId, string senderUserEmail, long? targetRoleId, long? clientCompanyId, long? vendorId, string symbol, string status, string message, string url, List<ApplicationUser>? smsRecipients = null, string? agentEmail = null) // Added as optional parameter
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var notification = new StatusNotification
            {
                RoleId = targetRoleId,
                ClientCompanyId = clientCompanyId,
                VendorId = vendorId,
                AgentUserEmail = agentEmail, // Now the engine knows where to put it
                Symbol = symbol ?? BlueSymbol,
                Message = message,
                Status = status,
                NotifierUserEmail = senderUserEmail,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            if (smsRecipients != null && smsRecipients.Any() && await _featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
            {
                var smsTasks = smsRecipients.Select(async user =>
                {
                    try
                    {
                        string smsBody = $"Dear {user.Email},\n{message} : {status}.\nThanks\n{senderUserEmail},\n{url}";
                        await _smsService.DoSendSmsAsync(user.Country!.Code, user.Country.ISDCode + user.PhoneNumber, smsBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("SMS failed for {Email}: {Msg}", user.Email, ex.Message);
                    }
                });
                await Task.WhenAll(smsTasks);
            }
        }
    }
}