using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Creator;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class DeleteCaseController : Controller
    {
        private readonly IDeleteCaseService _deleteService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<DeleteCaseController> _logger;

        public DeleteCaseController(
            IDeleteCaseService deleteService,
            INotyfService notifyService,
            ILogger<DeleteCaseController> logger)
        {
            _deleteService = deleteService;
            _notifyService = notifyService;
            _logger = logger;
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(long id)
        {
            var userEmail = User.Identity?.Name;
            try
            {
                var (success, message) = await _deleteService.SoftDeleteCaseAsync(id, userEmail);

                if (!success)
                {
                    _notifyService.Error(message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                        ? "Not Found!!!..Contact Admin"
                        : message);
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting case {Id} for {User}", id, userEmail);
                return Json(new { success = false, message = "Error deleting case. Try again." });
            }
        }

        [HttpPost, ActionName("DeleteCases")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCases([FromBody] DeleteRequestModel request)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid request." });

            var userEmail = User.Identity?.Name;
            try
            {
                var (success, message) = await _deleteService.SoftDeleteBulkCasesAsync(request.claims, userEmail);

                if (!success) return Json(new { success = false, message });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk delete for {User}", userEmail);
                return Json(new { success = false, message = "An error occurred during bulk deletion." });
            }
        }
    }
}