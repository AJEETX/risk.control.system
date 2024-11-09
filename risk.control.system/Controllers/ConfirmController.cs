using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ConfirmController : Controller
    {
        private readonly INotificationService notificationService;

        public ConfirmController(INotificationService notificationService)
        {
            this.notificationService = notificationService;
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
    }
}