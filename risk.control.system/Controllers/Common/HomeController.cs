using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Common
{
    public class HomeController : Controller
    {
        [Breadcrumb("Error")]
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            // Get the exception details if needed for logging/display
            var exceptionDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            // Log the error specifically if it wasn't caught elsewhere
            if (exceptionDetails != null)
            {
                // Serilog will pick this up via your existing configuration
                Log.Error(exceptionDetails.Error, "Unhandled exception at {Path}", exceptionDetails.Path);
            }

            return View();
        }
    }
}