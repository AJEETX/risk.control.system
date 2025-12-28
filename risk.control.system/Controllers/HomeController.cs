using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;
namespace risk.control.system.Controllers
{

    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var page = new MvcBreadcrumbNode("Index", "Home", "Home");
            ViewData["BreadcrumbNode"] = page;

            return View();
        }
    }
}
