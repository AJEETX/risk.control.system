using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
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
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ILogger<MailService> logger;
        private readonly ISmsService smsService;
        private readonly IFeatureManager featureManager;

        public MailService(IDbContextFactory<ApplicationDbContext> contextFactory,
            RoleManager<ApplicationRole> roleManager,
            ILogger<MailService> logger,
            ISmsService SmsService,
            IFeatureManager featureManager)
        {
            _contextFactory = contextFactory;
            this.roleManager = roleManager;
            this.logger = logger;
            smsService = SmsService;
            this.featureManager = featureManager;
        }

        public async Task NotifyFileUpload(string senderUserEmail, FileOnFileSystemModel file, string url)
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch the user (sender) details
                var applicationUser = await _context.ApplicationUser
                    .AsNoTracking()
                    .Include(i => i.Country)
                    .FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                if (applicationUser == null) return;

                // 2. Resolve the role
                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

                // 3. Determine status-specific values
                bool isCompleted = file.Completed.GetValueOrDefault();
                string statusMsg = isCompleted
                    ? $"Upload of {file.RecordCount} cases finished"
                    : $"JobId: {file.Id} Upload Error";

                string dbStatus = isCompleted
                    ? CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR
                    : CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_ERR;

                // 4. Use the Internal Engine
                // Note: For file uploads, the recipient of the SMS is the sender themselves
                var smsRecipients = new List<ApplicationUser> { applicationUser };

                await SendNotificationInternal(
                    caseId: 0, // No specific case ID yet for a bulk upload
                    senderUserEmail: senderUserEmail,
                    targetRoleId: creatorRole.Id,
                    clientCompanyId: applicationUser.ClientCompanyId,
                    vendorId: null,
                    symbol: isCompleted ? BlueSymbol : WarningSymbol,
                    status: dbStatus,
                    message: statusMsg,
                    url: url,
                    smsRecipients: smsRecipients
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "File Upload Notification Error for {UserEmail}", senderUserEmail);
            }
        }

        public async Task NotifyCaseAllocationToVendorAndManager(string userEmail, string policy, long caseId, long vendorId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch Case and Sender data
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var applicationUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                if (applicationUser == null || caseTask == null) return;

                // 2. Resolve Roles
                var managerRole = await roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                // 3. Get SMS Recipients (Vendor Admins & Supervisors)
                var vendorRecipients = await GetUsersByRoleAsync(
                    companyId: null,
                    vendorId: vendorId,
                    roleIds: new[] { agencyAdminRole.Id, supervisorRole.Id }
                );

                // 4. Trigger Internal Engine for Vendor Notification
                // This handles the Supervisor DB record and the batch SMS
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: userEmail,
                    targetRoleId: supervisorRole.Id,
                    clientCompanyId: null,
                    vendorId: vendorId,
                    symbol: null, // Uses default BlueSymbol inside engine
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: vendorRecipients
                );

                // 5. Trigger Internal Engine for Manager Notification
                // This handles the Client Manager DB record (No SMS required here based on your logic)
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: userEmail,
                    targetRoleId: managerRole.Id,
                    clientCompanyId: applicationUser.ClientCompanyId,
                    vendorId: null,
                    symbol: BlueSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: null // Managers don't get SMS in this specific workflow
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Allocation Notification Error for Case: {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseAllocationToVendor(string userEmail, string policy, long caseId, long vendorId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch Case and Sender data
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var senderUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                if (caseTask == null || senderUser == null) return;

                // 2. Resolve Role IDs
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                // 3. Get SMS Recipients using helper (Vendor Admins & Supervisors)
                var recipients = await GetUsersByRoleAsync(
                    companyId: null,
                    vendorId: vendorId,
                    roleIds: new[] { agencyAdminRole.Id, supervisorRole.Id }
                );

                // 4. Fire the Internal Engine
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: userEmail,
                    targetRoleId: supervisorRole.Id,
                    clientCompanyId: null,
                    vendorId: vendorId,
                    symbol: BlueSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}", // or use the 'policy' param
                    url: url,
                    smsRecipients: recipients
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Allocation Notification Error for User: {UserEmail}", userEmail);
            }
        }

        public async Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> caseIds, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch shared dependencies once
                var applicationUser = await _context.ApplicationUser.AsNoTracking()
                    .Include(i => i.Country)
                    .FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                if (applicationUser == null) return;

                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

                // 2. Fetch all cases in one trip
                var caseTasks = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .Where(v => caseIds.Contains(v.Id))
                    .ToListAsync();

                // 3. Process each case through the Engine
                // We create a list of tasks to run the internal saves and SMS in parallel
                var notificationTasks = caseTasks.Select(caseTask =>
                    SendNotificationInternal(
                        caseId: caseTask.Id,
                        senderUserEmail: senderUserEmail,
                        targetRoleId: creatorRole?.Id,
                        clientCompanyId: applicationUser.ClientCompanyId,
                        vendorId: null,
                        symbol: BlueSymbol,
                        status: caseTask.SubStatus,
                        message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                        url: url,
                        smsRecipients: new List<ApplicationUser> { applicationUser } // Sending SMS to the assigner
                    )
                );

                // Wait for all database records and SMS tasks to complete
                await Task.WhenAll(notificationTasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Batch Assignment Notification Error for {UserEmail}", senderUserEmail);
            }
        }

        public async Task NotifyCaseAssignmentToAssigner(string senderUserEmail, List<long> autoAllocatedCases, List<long> notAutoAllocatedCases, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch user data (Sender and Recipient are the same here)
                var applicationUser = await _context.ApplicationUser.AsNoTracking()
                    .Include(i => i.Country)
                    .FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                if (applicationUser == null)
                {
                    logger.LogWarning("Notification failed: User {Email} not found.", senderUserEmail);
                    return;
                }

                // 2. Resolve Role
                var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

                // 3. Calculate Totals
                int totalCases = (autoAllocatedCases?.Count ?? 0) + (notAutoAllocatedCases?.Count ?? 0);
                int autoCount = autoAllocatedCases?.Count ?? 0;

                // 4. Call the Internal Engine
                // We pass the summary details to the shared logic
                await SendNotificationInternal(
                    caseId: 0, // Summary notification, not tied to a single case
                    senderUserEmail: senderUserEmail,
                    targetRoleId: creatorRole?.Id,
                    clientCompanyId: applicationUser.ClientCompanyId,
                    vendorId: null,
                    symbol: BlueSymbol,
                    status: $"{CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER}={autoCount}",
                    message: $"Assigning of {totalCases} cases finished",
                    url: url,
                    smsRecipients: new List<ApplicationUser> { applicationUser }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification Error for {UserEmail}", senderUserEmail);
            }
        }

        public async Task NotifyCaseWithdrawlToCompany(string senderUserEmail, long caseId, long vendorId, string url = "")
        {
            using var _context = await _contextFactory.CreateDbContextAsync();
            var caseTask = await _context.Investigations.AsNoTracking().FirstOrDefaultAsync(v => v.Id == caseId);
            var creatorRole = await roleManager.FindByNameAsync(CREATOR.DISPLAY_NAME);

            var recipients = await _context.ApplicationUser.AsNoTracking().Include(u => u.Country)
                .Where(u => u.ClientCompanyId == caseTask.ClientCompanyId)
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == creatorRole.Id))
                .ToListAsync();

            await SendNotificationInternal(caseId, senderUserEmail, creatorRole.Id, caseTask.ClientCompanyId, null,
                WarningSymbol, caseTask.SubStatus, $"Case #{caseId} Withdrawn", url, recipients);
        }

        public async Task NotifyCaseAssignmentToVendorAgent(string senderUserEmail, long caseId, string agentEmail, long vendorId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch Case and Agent details in one trip
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var recipientUser = await _context.ApplicationUser.AsNoTracking()
                    .Include(c => c.Country)
                    .FirstOrDefaultAsync(c => c.Email == agentEmail);

                if (caseTask == null || recipientUser == null)
                {
                    logger.LogWarning("Assignment notification aborted: Case {CaseId} or Agent {Email} not found.", caseId, agentEmail);
                    return;
                }

                // 2. Resolve Role
                var agentRole = await roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);

                // 3. Fire the Internal Engine
                // We pass the agentEmail to the engine.
                // Note: Ensure SendNotificationInternal is updated to map 'agentEmail'
                // to the 'AgenctUserEmail' property in the StatusNotification object.
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: senderUserEmail,
                    targetRoleId: agentRole?.Id,
                    clientCompanyId: null, // This is a vendor-side notification
                    vendorId: vendorId,
                    symbol: BlueSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: new List<ApplicationUser> { recipientUser },
                    agentEmail: agentEmail // Pass the specific agent email
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification Error for Agent {AgentEmail} from {Sender}", agentEmail, senderUserEmail);
            }
        }

        public async Task NotifyCaseReportProcess(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch Case data
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                if (caseTask == null) return;

                // 2. Resolve Roles
                var managerRole = await roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                // 3. Determine Dynamic Symbol
                string statusSymbol = caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR
                    ? "far fa-thumbs-up i-green"
                    : "far fa-thumbs-down i-orangered";

                // 4. Fetch All Recipients (Both Agency Admins and Managers)
                // We use our helper method to keep the query logic clean
                var recipients = await GetUsersByRoleAsync(
                    companyId: caseTask.ClientCompanyId,
                    vendorId: caseTask.VendorId,
                    roleIds: new[] { managerRole.Id, agencyAdminRole.Id }
                );

                // 5. Trigger Engine for Agency Admin
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: senderUserEmail,
                    targetRoleId: agencyAdminRole?.Id,
                    clientCompanyId: null,
                    vendorId: caseTask.VendorId,
                    symbol: statusSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: recipients.Where(u => u.VendorId == caseTask.VendorId).ToList()
                );

                // 6. Trigger Engine for Client Manager
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: senderUserEmail,
                    targetRoleId: managerRole?.Id,
                    clientCompanyId: caseTask.ClientCompanyId,
                    vendorId: null,
                    symbol: statusSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: recipients.Where(u => u.ClientCompanyId == caseTask.ClientCompanyId).ToList()
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Report Process Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseReportSubmitToCompany(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch Case data
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                if (caseTask == null) return;

                // 2. Resolve the target Role
                var assessorRole = await roleManager.FindByNameAsync(ASSESSOR.DISPLAY_NAME);

                // 3. Get Recipients (Assessors for this specific company)
                var recipients = await GetUsersByRoleAsync(
                    companyId: caseTask.ClientCompanyId,
                    vendorId: null,
                    roleIds: new[] { assessorRole.Id }
                );

                // 4. Trigger the Internal Engine
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: senderUserEmail,
                    targetRoleId: assessorRole?.Id,
                    clientCompanyId: caseTask.ClientCompanyId,
                    vendorId: null,
                    symbol: BlueSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: recipients
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Report Submission Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifyCaseReportSubmitToVendorSupervisor(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch Case and Sender data
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var senderUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == senderUserEmail);

                if (caseTask == null || senderUser == null)
                {
                    logger.LogWarning("Notification aborted: Case {CaseId} or Sender {Email} not found.", caseId, senderUserEmail);
                    return;
                }

                // 2. Resolve Role IDs
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                // 3. Get All Recipients (Both Admins and Supervisors for this Vendor)
                var recipients = await GetUsersByRoleAsync(
                    companyId: null,
                    vendorId: senderUser.VendorId,
                    roleIds: new[] { supervisorRole.Id, agencyAdminRole.Id }
                );

                // 4. Trigger the Internal Engine
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: senderUserEmail,
                    targetRoleId: supervisorRole?.Id,
                    clientCompanyId: null,
                    vendorId: senderUser.VendorId,
                    symbol: BlueSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: recipients
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vendor Supervisor Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifySubmitQueryToAgency(string senderUserEmail, long caseId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch dependencies
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var senderUser = await _context.ApplicationUser.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == senderUserEmail);

                if (caseTask == null || senderUser == null) return;

                // 2. Resolve target roles
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                // 3. Get Recipients (Supervisors and Admins for the specific vendor)
                var recipients = await GetUsersByRoleAsync(
                    companyId: null,
                    vendorId: caseTask.VendorId,
                    roleIds: new[] { supervisorRole.Id, agencyAdminRole.Id }
                );

                // 4. Fire the Internal Engine
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: senderUserEmail,
                    targetRoleId: supervisorRole?.Id,
                    clientCompanyId: null,
                    vendorId: caseTask.VendorId,
                    symbol: "fa fa-question", // Specific query symbol
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: recipients
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Query Notification Error for Case {CaseId}", caseId);
            }
        }

        public async Task NotifySubmitReplyToCompany(string senderUserEmail, long caseId, string url = "")
        {
            using var _context = await _contextFactory.CreateDbContextAsync();
            var caseTask = await _context.Investigations.AsNoTracking().FirstOrDefaultAsync(v => v.Id == caseId);
            var assessorRole = await roleManager.FindByNameAsync(ASSESSOR.DISPLAY_NAME);

            var recipients = await _context.ApplicationUser.AsNoTracking().Include(u => u.Country)
                .Where(u => u.ClientCompanyId == caseTask.ClientCompanyId)
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == assessorRole.Id))
                .ToListAsync();

            await SendNotificationInternal(caseId, senderUserEmail, assessorRole.Id, caseTask.ClientCompanyId, null,
                BlueSymbol, caseTask.SubStatus, $"Reply Submitted for Case #{caseId}", url, recipients);
        }

        public async Task NotifyCaseWithdrawlFromAgent(string senderUserEmail, long caseId, long vendorId, string url = "")
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                // 1. Fetch case info
                var caseTask = await _context.Investigations.AsNoTracking()
                    .Include(i => i.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);
                if (caseTask == null) return;

                // 2. Resolve target roles
                var supervisorRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                var agencyAdminRole = await roleManager.FindByNameAsync(AGENCY_ADMIN.DISPLAY_NAME);

                // 3. Get recipients using helper
                var recipients = await GetUsersByRoleAsync(null, vendorId, supervisorRole.Id, agencyAdminRole.Id);

                // 4. Fire the internal engine
                await SendNotificationInternal(
                    caseId: caseId,
                    senderUserEmail: senderUserEmail,
                    targetRoleId: supervisorRole.Id, // Notification assigned to Supervisor role
                    clientCompanyId: null,
                    vendorId: vendorId,
                    symbol: WarningSymbol,
                    status: caseTask.SubStatus,
                    message: $"Case #{caseTask.PolicyDetail?.ContractNumber}",
                    url: url,
                    smsRecipients: recipients
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Withdrawal Notification Error for Case {CaseId}", caseId);
            }
        }

        private async Task<List<ApplicationUser>> GetUsersByRoleAsync(long? companyId, long? vendorId, params long[] roleIds)
        {
            using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ApplicationUser.AsNoTracking()
                .Include(u => u.Country)
                .Where(u => (companyId.HasValue && u.ClientCompanyId == companyId) ||
                            (vendorId.HasValue && u.VendorId == vendorId))
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId)))
                .ToListAsync();
        }

        private async Task SendNotificationInternal(
            long caseId,
            string senderUserEmail,
            long? targetRoleId,
            long? clientCompanyId,
            long? vendorId,
            string symbol,
            string status,
            string message,
            string url,
            List<ApplicationUser> smsRecipients,
            string agentEmail = null) // Added as optional parameter
        {
            using var _context = await _contextFactory.CreateDbContextAsync();

            var notification = new StatusNotification
            {
                RoleId = targetRoleId,
                ClientCompanyId = clientCompanyId,
                VendorId = vendorId,
                AgenctUserEmail = agentEmail, // Now the engine knows where to put it
                Symbol = symbol ?? BlueSymbol,
                Message = message,
                Status = status,
                NotifierUserEmail = senderUserEmail,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. Handle SMS in Parallel
            if (smsRecipients != null && smsRecipients.Any() && await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
            {
                var smsTasks = smsRecipients.Select(async user =>
                {
                    try
                    {
                        string smsBody = $"Dear {user.Email},\n{message} : {status}.\nThanks\n{senderUserEmail},\n{url}";
                        await smsService.DoSendSmsAsync(user.Country.Code, user.Country.ISDCode + user.PhoneNumber, smsBody);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("SMS failed for {Email}: {Msg}", user.Email, ex.Message);
                    }
                });
                await Task.WhenAll(smsTasks);
            }
        }
    }
}