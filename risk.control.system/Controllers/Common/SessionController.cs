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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RatingController> _logger;

        public SessionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RatingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> KeepSessionAlive([FromBody] KeepSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(new { message = "Invalid request." });

                var user = await GetAuthenticatedUserAsync();
                if (user == null) return Unauthorized(new { message = "User session invalid." });

                var now = DateTime.UtcNow;
                await UpdateOrCrateSessionRecord(user, request.CurrentPage!, now);

                await _signInManager.RefreshSignInAsync(user);

                return Ok(new
                {
                    name = user.UserName,
                    role = user.Role?.GetEnumDisplayName(),
                    lastActivity = now,
                    currentPage = request.CurrentPage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KeepSessionAlive for {UserEmail}", User?.Identity?.Name ?? "Anonymous");
                return StatusCode(500, new { message = "An error occurred." });
            }
        }

        private async Task<ApplicationUser?> GetAuthenticatedUserAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;

            return await _signInManager.UserManager.Users
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
        }

        private async Task UpdateOrCrateSessionRecord(ApplicationUser user, string page, DateTime now)
        {
            var session = await _context.UserSessionAlive
                .Where(s => s.ActiveUser.Id == user.Id && !s.LoggedOut)
                .OrderByDescending(s => s.Updated ?? s.Created)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                session.UpdatedBy = user.Email;
                session.Updated = now;
                session.CurrentPage = page;
            }
            else
            {
                _context.UserSessionAlive.Add(new UserSessionAlive
                {
                    ActiveUser = user,
                    CreatedUser = user.Email,
                    CurrentPage = page,
                    Created = now
                });
            }
            await _context.SaveChangesAsync(null, false);
        }

        [HttpGet]
        public async Task StreamTypingUpdates(string email, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Connection", "keep-alive");
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                await Response.WriteAsync("data: ERROR_UserNotFound\n\n", cancellationToken: cancellationToken); // Added \n\n
                await Response.Body.FlushAsync(cancellationToken);
                return;
            }

            var messages = new List<string>
            {
                $"Welcome! {user.Email}",
                "Please update credential to continue.",
                "Remember credential for later."
            };

            foreach (var message in messages)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await Response.WriteAsync($"data: {message}\n\n");
                await Response.Body.FlushAsync(cancellationToken);

                await Task.Delay(1500, cancellationToken);
            }

            await Response.WriteAsync("data: done\n\n");
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}