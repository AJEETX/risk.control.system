using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IAgentService
    {
        Task<VendorApplicationUser> GetAgent(string mobile, bool sendSMS = false);

        Task<VendorApplicationUser> ResetUid(string mobile, string portal_base_url, bool sendSMS = false);
        Task<VendorApplicationUser> GetPin(string agentEmail, string portal_base_url);
    }

    internal class AgentService : IAgentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ISmsService smsService;
        private readonly UserManager<VendorApplicationUser> userVendorManager;

        public AgentService(ApplicationDbContext context,
             IHttpContextAccessor httpContextAccessor,
             ISmsService smsService,
            UserManager<VendorApplicationUser> userVendorManager)
        {
            this._context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.smsService = smsService;
            this.userVendorManager = userVendorManager;
        }

        public async Task<VendorApplicationUser> GetAgent(string mobile, bool sendSMS = false)
        {
            var agentRole = await _context.ApplicationRole.FirstOrDefaultAsync(r => r.Name.Contains(AppRoles.AGENT.ToString()));

            var user2Onboard = await _context.VendorApplicationUser.FirstOrDefaultAsync(u => u.PhoneNumber == mobile && !string.IsNullOrWhiteSpace(u.MobileUId));

            var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
            if (isAgent)
                return user2Onboard;
            return null!;
        }

        public async Task<VendorApplicationUser> GetPin(string agentEmail, string portal_base_url)
        {
            var agentRole = await _context.ApplicationRole.FirstOrDefaultAsync(r => r.Name.Contains(AppRoles.AGENT.ToString()));

            var user2Onboard = await _context.VendorApplicationUser.FirstOrDefaultAsync(u => u.Email == agentEmail);

            var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
            if (isAgent)
                return user2Onboard;
            return null!;
        }
        public async Task<VendorApplicationUser> ResetUid(string mobile, string portal_base_url, bool sendSMS = false)
        {
            var agentRole = await _context.ApplicationRole.FirstOrDefaultAsync(r => r.Name.Contains(AppRoles.AGENT.ToString()));

            var user2Onboards = _context.VendorApplicationUser.Include(c => c.Country).Where(
                u => u.Country.ISDCode + u.PhoneNumber.TrimStart('+') == mobile.TrimStart('+') && !string.IsNullOrWhiteSpace(u.MobileUId));

            foreach (var user2Onboard in user2Onboards)
            {
                var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
                if (isAgent)
                {
                    user2Onboard.MobileUId = string.Empty;
                    user2Onboard.SecretPin = string.Empty;
                    _context.VendorApplicationUser.Update(user2Onboard);
                    _context.SaveChanges();

                    if (sendSMS)
                    {
                        //SEND SMS
                        string message = $"Dear {user2Onboard.Email}\n";
                        message += $"Uid reset for mobile: {mobile}\n";
                        message += $"{portal_base_url}";
                        await smsService.DoSendSmsAsync(user2Onboard.Country.Code, mobile, message);
                    }
                    return user2Onboard;
                }
            }
            return null!;
        }
    }
}