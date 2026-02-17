using Microsoft.EntityFrameworkCore;

using risk.control.system.Models;

namespace risk.control.system.Middleware
{
    public class UpdateUserLastActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UpdateUserLastActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                var email = context.User.Identity.Name;

                var user = await db.ApplicationUser.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    user.LastActivityDate = DateTime.UtcNow;

                    var session = await db.UserSessionAlive
                        .Where(x => x.ActiveUser.Id == user.Id && !x.LoggedOut)
                        .OrderByDescending(x => x.Updated ?? x.Created)
                        .FirstOrDefaultAsync();

                    if (session != null)
                    {
                        session.Updated = DateTime.UtcNow;
                        session.CurrentPage = context.Request.Path;
                    }
                    else
                    {
                        db.UserSessionAlive.Add(new UserSessionAlive
                        {
                            ActiveUser = user,
                            CurrentPage = context.Request.Path,
                            CreatedUser = email
                        });
                    }

                    await db.SaveChangesAsync();
                }
            }

            await _next(context);
        }
    }
}