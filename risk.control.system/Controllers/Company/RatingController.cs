using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Company
{
    public class RatingController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<RatingController> logger;

        public RatingController(
            ApplicationDbContext context,
            ILogger<RatingController> logger)
        {
            this.context = context;
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
    }
}