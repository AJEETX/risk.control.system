using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public class IdleUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IdleUserService(UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> hubContext)
        {
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public async Task CheckIdleUsers()
        {
            var idleTimeLimit = TimeSpan.FromMinutes(10); // 10-minute idle limit
            var idleUsers = _userManager.Users
                .Where(u => u.LastActivityDate < DateTime.UtcNow.Subtract(idleTimeLimit))
                .ToList();

            foreach (var user in idleUsers)
            {
                // Notify the user via SignalR
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveIdleWarning");

                // Force logout after 1 minute if no action is taken
                BackgroundJob.Schedule(() => ForceLogoutUser(user.Id.ToString()), TimeSpan.FromMinutes(1));
            }
        }

        public async Task ForceLogoutUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveForceLogout");
            }
        }
    }
}
