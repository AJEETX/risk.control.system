using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class ConfirmController(ISmsNotificationService notificationService,
            ICaseNotesService caseNotesService,
            IAgentMeetingSetupService agentMeetingSetupService,
            IEncryptionService encryptionService,
            ILogger<ConfirmController> logger) : Controller
    {
        private readonly ISmsNotificationService _notificationService = notificationService;
        private readonly ICaseNotesService _caseNotesService = caseNotesService;
        private readonly IAgentMeetingSetupService _agentMeetingSetupService = agentMeetingSetupService;
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly ILogger<ConfirmController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> GetSmsHistory(long caseId, bool isCustomer)
        {
            var messages = await _notificationService.GetSmsHistory(caseId, isCustomer);

            return Ok(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sms2Customer(SmsModel model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var customerName = await _notificationService.SendSms2Customer(userEmail, model.CaseId, model.Message!);
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
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var beneficiaryName = await _notificationService.SendSms2Beneficiary(userEmail, model.CaseId, model.Message!);
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
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var (smsSent, count) = await _caseNotesService.SubmitNotes(userEmail, model.CaseId, model.Message!);
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Schedule([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid confirmation link.");
            }

            try
            {
                var decryptedCaseId = _encryptionService.UnprotectId(token);
                if (decryptedCaseId <= 0)
                {
                    return BadRequest("The confirmation link is invalid or has expired.");
                }
                var schedule = await _agentMeetingSetupService.SetSchedule(decryptedCaseId);
                return View(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while confirming schedule with token {Token}", token);
                return BadRequest("The confirmation link is invalid or has expired.");
            }
        }
    }
}