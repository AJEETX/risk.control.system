using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using static risk.control.system.AppConstant.Applicationsettings;
namespace risk.control.system.Controllers
{

    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class HomeController : Controller
    {
        private readonly INotyfService notifyService;
        public HomeController(INotyfService notifyService)
        {
            this.notifyService = notifyService;
        }
        public IActionResult Index()
        {
            notifyService.Success($"Welcome <b>{GUEST.DISPLAY_NAME}</b>, Login successful");
            return View();
        }
    }
}
