using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface INotificationService
    {
        Task ClearAll(string userEmail);
        Task MarkAsRead(int id, string userEmail);
        Task<List<StatusNotification>> GetNotifications(string userEmail);
        Task<string> SendSms2Customer(string currentUser, long claimId, string sms);

        Task<string> SendSms2Beneficiary(string currentUser, long claimId, string sms);
    }

    internal class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext context;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private static string logo = "https://icheckify.co.in";

        public NotificationService(ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService)
        {
            this.context = context;
            this.roleManager = roleManager;
            smsService = SmsService;
        }

        public async Task<string> SendSms2Customer(string currentUser, long claimId, string sms)
        {
            var claim = await context.Investigations
            .Include(c => c.CaseMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == claimId);

            var mobile = claim.CustomerDetail.PhoneNumber.ToString();
            var user = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == currentUser);
            var isdCode = claim.CustomerDetail.Country.ISDCode;
            var isInsurerUser = user.ClientCompanyId > 0;
            var isVendorUser = user.VendorId > 0;

            string entityName = string.Empty;
            ApplicationUser insurerUser;
            ApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = (ApplicationUser)user;
                var entity = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == insurerUser.ClientCompanyId);
                entityName = entity?.Name ?? string.Empty;
            }
            else if (isVendorUser)
            {
                agencyUser = (ApplicationUser)user;
                var entity = await context.Vendor.FirstOrDefaultAsync(v => v.VendorId == agencyUser.VendorId);
                entityName = entity?.Name ?? string.Empty;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {claim.CustomerDetail.Name}\n\n";
            message += $"{sms}\n\n";
            message += $"Thanks\n\n";
            message += $"{user.FirstName} {user.LastName}\n\n";
            message += $"Policy #:{claim.PolicyDetail.ContractNumber}\n\n";
            message += $"{entityName}\n\n";
            message += $"{logo}";

            var scheduleMessage = new CaseMessage
            {
                Message = message,
                InvestigationTaskId = claimId,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.Now
            };
            claim.CaseMessages.Add(scheduleMessage);
            await context.SaveChangesAsync(null, false);
            await smsService.DoSendSmsAsync(claim.CustomerDetail.Country.Code, "+" + isdCode + mobile, message);
            return claim.CustomerDetail.Name;
        }

        public async Task<string> SendSms2Beneficiary(string currentUser, long claimId, string sms)
        {
            var beneficiary = await context.BeneficiaryDetail
                .Include(b => b.Country)
                .Include(b => b.InvestigationTask)
                .ThenInclude(c => c.PolicyDetail)
               .FirstOrDefaultAsync(c => c.InvestigationTaskId == claimId);

            var mobile = beneficiary.PhoneNumber.ToString();
            var user = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == currentUser);
            var isdCode = beneficiary.Country.ISDCode;

            var isInsurerUser = user is ApplicationUser;
            var isVendorUser = user is ApplicationUser;

            string entityName = string.Empty;
            ApplicationUser insurerUser;
            ApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = (ApplicationUser)user;
                var entity = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == insurerUser.ClientCompanyId);
                entityName = entity.Name;
            }
            else if (isVendorUser)
            {
                agencyUser = (ApplicationUser)user;
                var entity = await context.Vendor.FirstOrDefaultAsync(v => v.VendorId == agencyUser.VendorId);
                entityName = entity.Name;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {beneficiary.Name}\n\n";
            message += $"{sms}\n\n";
            message += $"Thanks\n\n";
            message += $"{user.FirstName} {user.LastName}\n\n";
            message += $"Policy #:{beneficiary.InvestigationTask.PolicyDetail.ContractNumber}\n\n";
            message += $"{entityName}\n\n";
            message += $"{logo}";

            var scheduleMessage = new CaseMessage
            {
                Message = message,
                InvestigationTaskId = claimId,
                RecepicientEmail = beneficiary.Name,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.Now
            };
            var claim = await context.Investigations
            .Include(c => c.CaseMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
            .FirstOrDefaultAsync(c => c.Id == claimId);
            claim.CaseMessages.Add(scheduleMessage);
            await context.SaveChangesAsync(null, false);
            await smsService.DoSendSmsAsync(beneficiary.Country.Code, "+" + isdCode + mobile, message);
            return beneficiary.Name;
        }

        public async Task<List<StatusNotification>> GetNotifications(string userEmail)
        {

            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail && c.ClientCompanyId > 0);
            var vendorUser = await context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail && c.VendorId > 0);

            ApplicationRole role = null!;
            ClientCompany company = null!;
            Vendor agency = null!;
            if (companyUser != null)
            {
                role = await roleManager.FindByNameAsync(companyUser.Role.ToString());

                company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                var notifications = context.Notifications.Where(n => n.Company == company && (!n.IsReadByCreator || !n.IsReadByManager || !n.IsReadByAssessor));
                if (role.Name == ASSESSOR.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => n.Role == role && !n.IsReadByAssessor);
                }
                else if (role.Name == MANAGER.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => !n.IsReadByManager);
                }

                else if (role.Name == CREATOR.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => (n.Role == role && n.NotifierUserEmail == userEmail) && !n.IsReadByCreator);
                }

                var activeNotifications = await notifications
                    .OrderByDescending(n => n.CreatedAt).ToListAsync();
                return activeNotifications;
            }
            else if (vendorUser != null)
            {
                role = await roleManager.FindByNameAsync(vendorUser.Role.ToString());
                agency = await context.Vendor.FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);

                var notifications = context.Notifications.Where(n => n.Agency == agency && (!n.IsReadByVendor || !n.IsReadByVendorAgent));

                if (role.Name == AGENT.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => n.AgenctUserEmail == userEmail);
                }
                else
                {
                    var superRole = await roleManager.FindByNameAsync(SUPERVISOR.DISPLAY_NAME);
                    if (role.Name == SUPERVISOR.DISPLAY_NAME)
                    {
                        notifications = notifications.Where(n =>
                        (!n.IsReadByVendor && n.NotifierUserEmail == userEmail &&
                        (n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR || n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR))
                        ||
                        (!n.IsReadByVendor && n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
                        );
                    }
                    else
                    {
                        notifications = notifications.Where(n => (!n.IsReadByVendor));
                    }
                }

                var activeNotifications = await notifications
                     .OrderByDescending(n => n.CreatedAt).ToListAsync();
                return activeNotifications;
            }
            var allNotifications = await context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return allNotifications;
        }

        public async Task MarkAsRead(int id, string userEmail)
        {
            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendorUser = await context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            ApplicationRole role = null!;
            ClientCompany company = null!;
            Vendor agency = null!;
            if (companyUser != null)
            {
                role = await roleManager.FindByNameAsync(companyUser.Role.ToString());
                company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                var notification = await context.Notifications.FirstOrDefaultAsync(s => s.StatusNotificationId == id);
                if (notification == null)
                {
                    return;
                }
                if (role.Name == ASSESSOR.DISPLAY_NAME)
                {
                    notification.IsReadByAssessor = true;
                }
                else if (role.Name == MANAGER.DISPLAY_NAME)
                {
                    notification.IsReadByManager = true;
                }

                else if (role.Name == CREATOR.DISPLAY_NAME)
                {
                    notification.IsReadByCreator = true;
                }
                context.Notifications.Update(notification);
                var rows = await context.SaveChangesAsync(null, false);
            }
            else if (vendorUser != null)
            {
                role = await roleManager.FindByNameAsync(vendorUser.Role.ToString());
                agency = await context.Vendor.FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);
                var notification = await context.Notifications.FirstOrDefaultAsync(s => s.Agency == agency && s.StatusNotificationId == id);
                if (notification == null)
                {
                    return;
                }
                if (role.Name == AGENCY_ADMIN.DISPLAY_NAME || role.Name == SUPERVISOR.DISPLAY_NAME)
                {
                    notification.IsReadByVendor = true;
                }

                else if (role.Name == AGENT.DISPLAY_NAME)
                {
                    notification.IsReadByVendorAgent = true;
                }
                context.Notifications.Update(notification);
                var rows = await context.SaveChangesAsync(null, false);
            }
        }

        public async Task ClearAll(string userEmail)
        {
            var notifications = await GetNotifications(userEmail);
            foreach (var notification in notifications)
            {
                await MarkAsRead(notification.StatusNotificationId, userEmail);
            }
        }
    }
}