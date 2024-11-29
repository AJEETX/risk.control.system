using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ConfirmController : Controller
    {
        private readonly INotificationService notificationService;
        private readonly IClaimsInvestigationService claimsInvestigationService;

        public ConfirmController(INotificationService notificationService, IClaimsInvestigationService claimsInvestigationService)
        {
            this.notificationService = notificationService;
            this.claimsInvestigationService = claimsInvestigationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string id)
        {
            var currentUrl = HttpContext.Request.GetDisplayUrl();
            string tempUrl = currentUrl.Replace(HttpContext.Request.Path, "");
            int index = tempUrl.IndexOf("?");
            string baseUrl = tempUrl.Substring(0, index);

            var claimMessage = await notificationService.GetClaim(baseUrl, id);

            return View(claimMessage);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SendSms2Customer(string claimId, string name)
        {
            var currentUser = HttpContext.User.Identity.Name;
            var customerName =await notificationService.SendSms2Customer(currentUser, claimId, name);
            if(string.IsNullOrEmpty(customerName))
            {
                return BadRequest("Error !!!");
            }

            return Ok(new { message = "Message Sent: Success", customerName = customerName });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SendSms2Beneficiary(string claimId, string name)
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
        public async Task<IActionResult> SubmitNotes(string claimId, string name)
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                    return Unauthorized("Error !!!");
            }
        }
    }
}