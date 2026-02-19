using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Controllers.Creator;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers.Company
{
    public class SessionController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ILogger<RatingController> logger;

        public SessionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RatingController> logger)
        {
            this.context = context;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> KeepSessionAlive([FromBody] KeepSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Invalid request." });

                if (User?.Identity == null || !User.Identity.IsAuthenticated)
                    return Unauthorized(new { message = "User is logged out due to inactivity or authentication failure." });

                var email = User.Identity.Name;
                var user = await signInManager.UserManager.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                    return Unauthorized(new { message = "User not found." });

                var now = DateTime.UtcNow;

                // Update user's last activity
                user.LastActivityDate = now;

                // Update or create UserSessionAlive
                var session = await context.UserSessionAlive
                    .Where(s => s.ActiveUser.Id == user.Id && !s.LoggedOut)
                    .OrderByDescending(s => s.Updated ?? s.Created)
                    .FirstOrDefaultAsync();

                if (session != null)
                {
                    session.Updated = now;
                    session.CurrentPage = request.CurrentPage;
                }
                else
                {
                    context.UserSessionAlive.Add(new UserSessionAlive
                    {
                        ActiveUser = user,
                        CreatedUser = email,
                        CurrentPage = request.CurrentPage,
                        Created = now
                    });
                }

                await context.SaveChangesAsync();

                // Optionally refresh the cookie sign-in
                await signInManager.RefreshSignInAsync(user);

                // Return minimal user info
                var userDetails = new
                {
                    name = user.UserName,
                    role = user.Role?.GetEnumDisplayName(),
                    lastActivity = user.LastActivityDate,
                    currentPage = request.CurrentPage
                };

                return Ok(userDetails);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in KeepSessionAlive for {UserEmail}", User?.Identity?.Name ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        [HttpGet]
        public async Task StreamTypingUpdates(string email, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";

            // Fetch user details for password change
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                await Response.WriteAsync($"data: ERROR_UserNotFound\n");
                await Response.WriteAsync($"data: done\n");
                await Response.Body.FlushAsync(cancellationToken);
                return;
            }

            // Send user details first
            var credentialModelJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                email = user.Email,
                currentPassword = user.Password
            });

            await Response.WriteAsync($"data: CREDENTIAL|{credentialModelJson}\n");
            await Response.Body.FlushAsync(cancellationToken);
            await Task.Delay(1000, cancellationToken); // Small delay to ensure UI updates first

            // Now, stream messages one by one
            var messages = new List<string>
            {
                $"Welcome ! {user.Email} First time user.",
                "Please update credential to continue.",
                "Remember credential for later."
            };

            foreach (var message in messages)
            {
                await Response.WriteAsync($"data: {message}\n");
                await Response.Body.FlushAsync(cancellationToken);
                await Task.Delay(1500, cancellationToken); // Simulate delay between messages
            }

            // Indicate completion
            await Response.WriteAsync($"data: done\n");
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}