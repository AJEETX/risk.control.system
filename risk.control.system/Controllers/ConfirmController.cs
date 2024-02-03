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
        private readonly IWebHostEnvironment webHostEnvironment;

        public ConfirmController(INotificationService notificationService, IWebHostEnvironment webHostEnvironment)
        {
            this.notificationService = notificationService;
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
    }
}