using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface INotificationService
    {
        Task ClearAll(string userEmail);

        Task MarkAsRead(long id, string userEmail);

        Task<List<StatusNotification>> GetNotifications(string userEmail);
    }

    internal class NotificationService(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager) : INotificationService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;

        public async Task<List<StatusNotification>> GetNotifications(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail && c.ClientCompanyId > 0);
            var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail && c.VendorId > 0);
            ApplicationRole? role = null!;
            ClientCompany? company = null!;
            Vendor? agency = null!;
            if (companyUser != null)
            {
                role = await _roleManager.FindByNameAsync(companyUser.Role.ToString()!);
                company = await _context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                var notifications = _context.Notifications.AsNoTracking().Where(n => n.ClientCompanyId == company!.ClientCompanyId && (!n.IsReadByCreator || !n.IsReadByManager || !n.IsReadByAssessor));
                if (role?.Name == ASSESSOR.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => n.RoleId == role.Id && !n.IsReadByAssessor);
                }
                else if (role?.Name == MANAGER.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => !n.IsReadByManager);
                }
                else if (role?.Name == CREATOR.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => (n.RoleId == role.Id && n.NotifierUserEmail == userEmail) && !n.IsReadByCreator);
                }
                else if (role?.Name == COMPANY_ADMIN.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => n.RoleId == role.Id && !n.IsReadByCompanyAdmin);
                }
                return await notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();
            }
            else if (vendorUser != null)
            {
                role = await _roleManager.FindByNameAsync(vendorUser.Role.ToString()!);
                agency = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);
                var notifications = _context.Notifications.AsNoTracking().Where(n => n.VendorId == vendorUser.VendorId && (!n.IsReadByVendor || !n.IsReadByVendorAgent));
                if (role?.Name == AGENT.DISPLAY_NAME)
                {
                    notifications = notifications.Where(n => n.AgentUserEmail == userEmail);
                }
                else
                {
                    if (role?.Name == SUPERVISOR.DISPLAY_NAME)
                    {
                        notifications = notifications.Where(n => (!n.IsReadByVendor && n.NotifierUserEmail == userEmail && (n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR || n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)) || (!n.IsReadByVendor && n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR));
                    }
                    else
                    {
                        notifications = notifications.Where(n => (!n.IsReadByVendor));
                    }
                }
                return await notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();
            }
            return await _context.Notifications.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        public async Task MarkAsRead(long id, string userEmail)
        {
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail && c.ClientCompanyId > 0);
            var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail && c.VendorId > 0);

            ApplicationRole? role = null!;
            ClientCompany? company = null!;
            Vendor? agency = null!;
            if (companyUser != null)
            {
                role = await _roleManager.FindByNameAsync(companyUser.Role.ToString()!);
                company = await _context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                var notification = await _context.Notifications.FirstOrDefaultAsync(s => s.StatusNotificationId == id);
                if (notification == null)
                {
                    return;
                }
                if (role?.Name == ASSESSOR.DISPLAY_NAME)
                {
                    notification.IsReadByAssessor = true;
                }
                else if (role?.Name == MANAGER.DISPLAY_NAME)
                {
                    notification.IsReadByManager = true;
                }
                else if (role?.Name == CREATOR.DISPLAY_NAME)
                {
                    notification.IsReadByCreator = true;
                }
                else if (role?.Name == COMPANY_ADMIN.DISPLAY_NAME)
                {
                    notification.IsReadByCompanyAdmin = true;
                }
                _context.Notifications.Update(notification);
                var rows = await _context.SaveChangesAsync(null, false);
            }
            else if (vendorUser != null)
            {
                role = await _roleManager.FindByNameAsync(vendorUser.Role.ToString()!);
                agency = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);
                var notification = await _context.Notifications.FirstOrDefaultAsync(s => s.VendorId == vendorUser.VendorId && s.StatusNotificationId == id);
                if (notification == null)
                {
                    return;
                }
                if (role?.Name == AGENCY_ADMIN.DISPLAY_NAME || role?.Name == SUPERVISOR.DISPLAY_NAME)
                {
                    notification.IsReadByVendor = true;
                }
                else if (role?.Name == AGENT.DISPLAY_NAME)
                {
                    notification.IsReadByVendorAgent = true;
                }
                _context.Notifications.Update(notification);
                var rows = await _context.SaveChangesAsync(null, false);
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