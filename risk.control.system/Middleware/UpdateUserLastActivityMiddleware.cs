using risk.control.system.Data;

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
            if (context.User.Identity.IsAuthenticated)
            {
                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                var user = dbContext.ApplicationUser.FirstOrDefault(u => u.Email == context.User.Identity.Name);
                if (user != null)
                {
                    user.LastActivityDate = DateTime.Now;
                    dbContext.SaveChanges();
                }
            }

            await _next(context);
        }
    }
}
