using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class RatingController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ILogger<RatingController> logger;

        public RatingController(
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

        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        public async Task<JsonResult> PostRating(int rating, long mid)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var existingRating = await context.Ratings.FirstOrDefaultAsync(r => r.VendorId == mid && r.UserEmail == currentUserEmail);

                if (existingRating != null)
                {
                    existingRating.Rate = rating;
                    context.Ratings.Update(existingRating);
                    await context.SaveChangesAsync();

                    return Json($"You rated again {rating} star(s)");
                }

                var rt = new AgencyRating
                {
                    Rate = rating,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    VendorId = mid,
                    UserEmail = currentUserEmail
                };

                context.Ratings.Add(rt);
                await context.SaveChangesAsync();

                return Json($"You rated this {rating} star(s)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error while posting rating. VendorId={VendorId}, Rating={Rating}, User={UserEmail}",
                    mid, rating, currentUserEmail);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while submitting your rating. Please try again."
                });
            }
        }

        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        public async Task<JsonResult> PostDetailRating(int rating, long vendorId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Json(new { success = false, message = "You must be logged in to rate." });
            }

            try
            {
                var existingRating = await context.Ratings
                    .FirstOrDefaultAsync(r => r.VendorId == vendorId && r.UserEmail == currentUserEmail);

                if (existingRating != null)
                {
                    existingRating.Rate = rating;
                    context.Ratings.Update(existingRating);
                }
                else
                {
                    context.Ratings.Add(new AgencyRating
                    {
                        VendorId = vendorId,
                        Rate = rating,
                        UserEmail = currentUserEmail,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });
                }

                await context.SaveChangesAsync();

                var ratings = context.Ratings.Where(r => r.VendorId == vendorId);
                var avgRating = ratings.Any() ? ratings.Average(r => r.Rate) : 0;

                return Json(new
                {
                    success = true,
                    message = $"You rated {rating} star(s)",
                    avgRating
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error while posting detailed rating. VendorId={VendorId}, Rating={Rating}, User={UserEmail}",
                    vendorId, rating, currentUserEmail);

                return Json(new
                {
                    success = false,
                    message = "Unable to save your rating at the moment. Please try again later."
                });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KeepSessionAlive([FromBody] KeepSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request." });
                }
                if (User is null || User.Identity is null)
                {
                    return Unauthorized(new { message = "User is logged out due to inactivity or authentication failure." });
                }

                if (User.Identity.IsAuthenticated)
                {
                    var user = await signInManager.UserManager.GetUserAsync(User);

                    if (user != null)
                    {
                        await signInManager.RefreshSignInAsync(user);
                        var userDetails = new
                        {
                            name = user.UserName,
                            role = user.Role != null ? user.Role.GetEnumDisplayName() : null!,
                            cookieExpiry = user.LastActivityDate ?? user.Updated,
                            currentPage = request.CurrentPage
                        };

                        var userSessionAlive = new UserSessionAlive
                        {
                            Updated = DateTime.Now,
                            ActiveUser = user,
                            CurrentPage = request.CurrentPage,
                        };
                        context.UserSessionAlive.Add(userSessionAlive);
                        await context.SaveChangesAsync(null, false);
                        return Ok(userDetails);
                    }
                }
                await signInManager.SignOutAsync();
                return Unauthorized(new { message = "User is logged out due to inactivity or authentication failure." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in KeepSessionAlive for {UserEmail}", User.Identity.Name ?? "Anonymous");
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