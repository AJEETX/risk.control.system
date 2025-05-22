using Microsoft.AspNetCore.Mvc;

namespace risk.control.system.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
        public IActionResult HTTP(int statusCode)
        {
            ViewBag.StatusCode = statusCode;

            string message = statusCode switch
            {
                404 => "Page not found.",
                500 => "Server error.",
                403 => "Access denied.",
                _ => "An unexpected error occurred."
            };

            ViewBag.Message = message;
            return View();
        }

    }
}