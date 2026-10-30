using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class DeleteCaseController(
            IDeleteCaseService deleteService,
            IEncryptionService encryptionService,
            INotyfService notifyService,
            ILogger<DeleteCaseController> logger) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly IDeleteCaseService _deleteService = deleteService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<DeleteCaseController> _logger = logger;

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = User.Identity?.Name!;
            try
            {
                var (success, message) = await _deleteService.SoftDeleteCaseAsync(id, userEmail);

                if (!success)
                {
                    _notifyService.Error(message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                        ? "Not Found!!!..Contact Admin"
                        : message);
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
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
            if (!ModelState.IsValid || request.EncryptedCaseIds == null || !request.EncryptedCaseIds.Any())
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            var userEmail = User.Identity?.Name!;
            try
            {
                var decryptedCaseIds = new List<long>();

                foreach (var encryptedId in request.EncryptedCaseIds)
                {
                    var decryptedId = _encryptionService.UnprotectId(encryptedId);
                    if (decryptedId > 0)
                    {
                        decryptedCaseIds.Add(decryptedId);
                    }
                }

                if (decryptedCaseIds.Count == 0)
                {
                    return Json(new { success = false, message = "No valid Cases found." });
                }

                var (success, message) = await _deleteService.SoftDeleteBulkCasesAsync(decryptedCaseIds, userEmail);

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