using Microsoft.AspNetCore.Mvc;

namespace risk.control.system.Controllers.Common
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }
    }
}