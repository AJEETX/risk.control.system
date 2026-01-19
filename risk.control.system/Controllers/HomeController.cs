using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace risk.control.system.Controllers
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
