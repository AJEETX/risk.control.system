using Microsoft.AspNetCore.SignalR;

using System.Collections.Concurrent;

namespace risk.control.system.Helpers
{

    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> Users = new ConcurrentDictionary<string, string>();
        private readonly IHttpContextAccessor ctx;

        public ChatHub(IHttpContextAccessor ctx)
        {
            this.ctx = ctx;
        }
        public override async Task OnConnectedAsync()
        {
            var currentUser = ctx.HttpContext.User.Identity.Name;
            string userId = Context.ConnectionId;
            string userName = Context.GetHttpContext().Request.Query["user"];
            Users.TryAdd(userId, userName);
            if(!string.IsNullOrWhiteSpace( userId) && !string.IsNullOrWhiteSpace(userName))
            {
                if(currentUser != userName)
                {
                    await Clients.All.SendAsync("UserConnected", userName); 
                }
                await Clients.All.SendAsync("UpdateUsers", Users.Values);

                await base.OnConnectedAsync();
            }
            
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var currentUser = ctx.HttpContext.User.Identity.Name;
            string userId = Context.ConnectionId;
            Users.TryRemove(userId, out string userName);
            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(userName))
            {
                await Clients.All.SendAsync("UserDisconnected", userName);
                await Clients.All.SendAsync("UpdateUsers", Users.Values);

                await base.OnDisconnectedAsync(exception);
            }
               
        }

        public async Task SendMessage(string user, string message)
        {
            var currentUser = ctx.HttpContext.User.Identity.Name;
            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(message))
            {
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }
        }
    }

}
