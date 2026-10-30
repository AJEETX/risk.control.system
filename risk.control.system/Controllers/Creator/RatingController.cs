using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class RatingController(
            ApplicationDbContext context,
            ILogger<RatingController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<RatingController> _logger = logger;

        public async Task<JsonResult> PostRating(int rating, long mid)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var existingRating = await _context.Ratings.FirstOrDefaultAsync(r => r.VendorId == mid && r.UserEmail == userEmail);

                if (existingRating != null)
                {
                    existingRating.Rate = rating;
                    _context.Ratings.Update(existingRating);
                    await _context.SaveChangesAsync();

                    return Json($"You rated again {rating} star(s)");
                }

                var rt = new AgencyRating
                {
                    Rate = rating,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    VendorId = mid,
                    UserEmail = userEmail
                };

                _context.Ratings.Add(rt);
                await _context.SaveChangesAsync();

                return Json($"You rated this {rating} star(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while posting rating. VendorId={VendorId}, Rating={Rating}, User={UserEmail}",
                    mid, rating, userEmail);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while submitting your rating. Please try again."
                });
            }
        }

        public async Task<JsonResult> PostDetailRating(int rating, long vendorId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Json(new { success = false, message = "You must be logged in to rate." });
            }

            try
            {
                double avgRating = await SaveRatingAndGetAverageAsync(rating, vendorId, currentUserEmail);

                return Json(new
                {
                    success = true,
                    message = $"You rated {rating} star(s)",
                    avgRating
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while posting detailed rating. VendorId={VendorId}, Rating={Rating}, User={UserEmail}",
                    vendorId, rating, currentUserEmail);

                return Json(new
                {
                    success = false,
                    message = "Unable to save your rating at the moment. Please try again later."
                });
            }
        }
        private async Task<double> SaveRatingAndGetAverageAsync(int rating, long vendorId, string userEmail)
        {
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.VendorId == vendorId && r.UserEmail == userEmail);

            if (existingRating != null)
            {
                existingRating.Rate = rating;
            }
            else
            {
                _context.Ratings.Add(new AgencyRating
                {
                    VendorId = vendorId,
                    Rate = rating,
                    UserEmail = userEmail,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                });
            }

            await _context.SaveChangesAsync();

            // Simplified average calculation
            return await _context.Ratings
                .Where(r => r.VendorId == vendorId)
                .Select(r => (double?)r.Rate)
                .AverageAsync() ?? 0.0;
        }
    }
}