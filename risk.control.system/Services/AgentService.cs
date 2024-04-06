using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IAgentService
    {
        Task<VendorApplicationUser> GetAgent(string mobile, bool sendSMS = false);

        Task<VendorApplicationUser> ResetUid(string mobile, bool sendSMS = false);
    }

    public class AgentService : IAgentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<VendorApplicationUser> userVendorManager;

        public AgentService(ApplicationDbContext context,
             IHttpContextAccessor httpContextAccessor,
            UserManager<VendorApplicationUser> userVendorManager)
        {
            this._context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.userVendorManager = userVendorManager;
        }

        public async Task<VendorApplicationUser> GetAgent(string mobile, bool sendSMS = false)
        {
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));

            var user2Onboard = _context.VendorApplicationUser.FirstOrDefault(
                u => u.PhoneNumber == mobile && !string.IsNullOrWhiteSpace(u.MobileUId));

            var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
            if (isAgent)
                return user2Onboard;
            return null!;
        }

        public async Task<VendorApplicationUser> ResetUid(string mobile, bool sendSMS = false)
        {
            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Agent.ToString()));

            var user2Onboard = _context.VendorApplicationUser.FirstOrDefault(
                u => u.PhoneNumber == mobile && !string.IsNullOrWhiteSpace(u.MobileUId));

            if (user2Onboard == null)
                return null!;

            var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
            if (!isAgent)
                return null!;

            user2Onboard.MobileUId = string.Empty;
            user2Onboard.SecretPin = string.Empty;
            _context.VendorApplicationUser.Update(user2Onboard);
            _context.SaveChanges();

            if (sendSMS)
            {
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}/{Applicationsettings.WEBSITE_SITE_MENU_LOGO}";
                //SEND SMS
                string message = $"Dear {user2Onboard.Email}";
                message += $"Uid reset for mobile: {user2Onboard.PhoneNumber}";
                message += $"                                          ";
                message += $"Thanks";
                message += $"                                          ";
                message += $"{BaseUrl}";
                var response = SmsService.SendSingleMessage(mobile, message, sendSMS);
            }
            return user2Onboard;
        }
    }
}