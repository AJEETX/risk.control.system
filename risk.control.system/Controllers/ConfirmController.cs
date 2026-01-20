using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;
using risk.control.system.AppConstant;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Sms2Customer(long claimId, string name)
        {
            var currentUser = HttpContext.User.Identity.Name;
            var customerName = await notificationService.SendSms2Customer(currentUser, claimId, name);
            if (string.IsNullOrEmpty(customerName))
            {
                return BadRequest("Error !!!");
            }

            return Ok(new { message = "Message Sent: Success", customerName = customerName });
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Sms2Beneficiary(long claimId, string name)
        {
            var currentUser = HttpContext.User.Identity.Name;
            var customerName = await notificationService.SendSms2Beneficiary(currentUser, claimId, name);
            if (string.IsNullOrEmpty(customerName))
            {
                return BadRequest("Error !!!");
            }
            return Ok(new { message = "Message Sent: Success", customerName = customerName });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AddNotes(long claimId, string name)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    return Unauthorized("Error !!!");
                }


                var model = await claimsInvestigationService.SubmitNotes(currentUserEmail, claimId, name);
                if (model)
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