using Microsoft.AspNetCore.Mvc;

namespace risk.control.system.Controllers
{
    public class ClaimsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
