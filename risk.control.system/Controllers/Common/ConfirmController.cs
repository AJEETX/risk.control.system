using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class ConfirmController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ICaseNotesService _caseNotesService;
        private readonly ILogger<ConfirmController> _logger;

        public ConfirmController(INotificationService notificationService, ICaseNotesService caseNotesService, ILogger<ConfirmController> logger)
        {
            _notificationService = notificationService;
            _caseNotesService = caseNotesService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sms2Customer(SmsModel model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var customerName = await _notificationService.SendSms2Customer(userEmail, model.CaseId, model.Message);
                if (string.IsNullOrEmpty(customerName))
                {
                    return BadRequest("SMS Error !!! No Custmer found.");
                }

                return Ok(new { message = "Message Sent: Success", customerName = customerName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred sending SMS to Customer for Case {Id} for user {UserEmail}", model.CaseId, userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sms2Beneficiary(SmsModel model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var beneficiaryName = await _notificationService.SendSms2Beneficiary(userEmail, model.CaseId, model.Message);
                if (string.IsNullOrEmpty(beneficiaryName))
                {
                    return BadRequest("SMS Error !!! No Beneficiary found.");
                }
                return Ok(new { message = "Message Sent: Success", beneficiaryName = beneficiaryName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred sending SMS to Beneficiary for Case {Id} for user {UserEmail}", model.CaseId, userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNotes(SmsModel model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var (smsSent, count) = await _caseNotesService.SubmitNotes(userEmail, model.CaseId, model.Message);
                if (smsSent)
                {
                    return Ok(new { message = "Notes added: Success", newCount = count });
                }
                return BadRequest("Notes Add Error !!!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred adding notes for Case {Id} for user {UserEmail}", model.CaseId, userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}