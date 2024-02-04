using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ConfirmController : Controller
    {
        private readonly INotificationService notificationService;
        private readonly INotyfService notifyService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ConfirmController(INotificationService notificationService, INotyfService notifyService, IWebHostEnvironment webHostEnvironment)
        {
            this.notificationService = notificationService;
            this.notifyService = notifyService;
            this.webHostEnvironment = webHostEnvironment;
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
            var customerName = notificationService.SendSms2Customer(currentUser, claimId, name);

            await Task.Delay(4000);
            return Ok(new { message = "Message Sent: Success", customerName = customerName });

            //notifyService.Custom($"SMS Sent to {customerName} by {currentUser}", 3, "green", "far fa-file-powerpoint");

            //return Redirect("/ClaimsInvestigation/Detail?id=" + claimId);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SendSms2Beneficiary(string claimId, string name)
        {
            var currentUser = HttpContext.User.Identity.Name;
            var customerName = notificationService.SendSms2Beneficiary(currentUser, claimId, name);
            //notifyService.Custom($"SMS Sent to {customerName} by {currentUser}", 3, "green", "far fa-file-powerpoint");
            //return Redirect("/ClaimsInvestigation/Detail?id=" + claimId);
            await Task.Delay(4000);
            return Ok(new { message = "Message Sent: Success", customerName = customerName });
        }
    }
}