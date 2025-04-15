using Microsoft.AspNetCore.SignalR;

namespace risk.control.system.Services
{
    public class NotificationHub : Hub
    {
        public async Task SendIdleWarning()
        {
            await Clients.Caller.SendAsync("ReceiveIdleWarning");
        }

        public async Task ForceLogout()
        {
            await Clients.Caller.SendAsync("ReceiveForceLogout");
        }
    }
}
