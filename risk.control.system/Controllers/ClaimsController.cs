using Microsoft.AspNetCore.Mvc;

namespace risk.web.MVC.Controllers
{
public class ClaimsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
}