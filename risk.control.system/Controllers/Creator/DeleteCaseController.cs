using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
    public class DeleteCaseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<DeleteCaseController> logger;

        public DeleteCaseController(ApplicationDbContext context,
            INotyfService notifyService,
            ILogger<DeleteCaseController> logger)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(long id)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request." });
            }
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var companyUser = await _context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                if (id <= 0)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var claimsInvestigation = await _context.Investigations.FindAsync(id);
                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = currentUserEmail;
                claimsInvestigation.Deleted = true;
                _context.Investigations.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Case deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting case {Id}. {UserEmail}", id, currentUserEmail);
                notifyService.Error("Error deleting case. Try again.");
                return Json(new { success = false, message = "Error deleting case. Try again." });
            }
        }

        [HttpPost, ActionName("DeleteCases")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCases([FromBody] DeleteRequestModel request)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid request." });
                }
                if (request.claims == null || request.claims.Count == 0)
                {
                    return Json(new { success = false, message = "No cases selected for deletion." });
                }

                foreach (var claim in request.claims)
                {
                    var claimsInvestigation = await _context.Investigations.FindAsync(claim);
                    if (claimsInvestigation == null)
                    {
                        notifyService.Error("Not Found!!!..Contact Admin");
                        return this.RedirectToAction<DashboardController>(x => x.Index());
                    }

                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = currentUserEmail;
                    claimsInvestigation.Deleted = true;
                    _context.Investigations.Update(claimsInvestigation);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting cases. {UserEmail}", currentUserEmail);
                notifyService.Error("Error deleting cases. Try again.");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}