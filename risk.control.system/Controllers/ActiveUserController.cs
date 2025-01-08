using Microsoft.AspNetCore.Mvc;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Admin Settings ")]
    public class ActiveUserController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Active");
        }

        [Breadcrumb("Active Users")]
        public ActionResult Active()
        {
            return View();
        }
    }
}
