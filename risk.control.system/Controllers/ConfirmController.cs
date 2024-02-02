using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
            var path = Path.Combine(webHostEnvironment.WebRootPath, "form", "ConfirmAcountRegister.html");

            var subject = "Verify Your E-mail Address ";
            string HtmlBody = "";
            using (StreamReader stream = System.IO.File.OpenText(path))
            {
                HtmlBody = stream.ReadToEnd();
            }
            var claim = await notificationService.GetClaim(id);

            return View();
        }
    }
}