using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class ConfirmController : Controller
    {
        private readonly INotificationService notificationService;
        private readonly ICaseInvestigationService claimsInvestigationService;

        public ConfirmController(INotificationService notificationService, ICaseInvestigationService claimsInvestigationService)
        {
            this.notificationService = notificationService;
            this.claimsInvestigationService = claimsInvestigationService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sms2Customer(SmsModel model)
        {
            var currentUser = HttpContext.User.Identity.Name;
            var customerName = await notificationService.SendSms2Customer(currentUser, model.CaseId, model.Message);
            if (string.IsNullOrEmpty(customerName))
            {
                return BadRequest("Error !!!");
            }

            return Ok(new { message = "Message Sent: Success", customerName = customerName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sms2Beneficiary(SmsModel model)
        {
            var currentUser = HttpContext.User.Identity.Name;
            var customerName = await notificationService.SendSms2Beneficiary(currentUser, model.CaseId, model.Message);
            if (string.IsNullOrEmpty(customerName))
            {
                return BadRequest("Error !!!");
            }
            return Ok(new { message = "Message Sent: Success", customerName = customerName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNotes(SmsModel model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    return Unauthorized("Error !!!");
                }

                var smsSent = await claimsInvestigationService.SubmitNotes(currentUserEmail, model.CaseId, model.Message);
                if (smsSent)
                {
                    return Ok(new { message = "Notes added: Success" });
                }
                return BadRequest("Error !!!");
            }
            catch (Exception)
            {
                return Unauthorized("Error !!!");
            }
        }
    }
}