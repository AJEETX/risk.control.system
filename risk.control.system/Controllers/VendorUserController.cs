using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Agency")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorUserController : Controller
    {
        public IActionResult Index(string id)
        {
            ViewData["vendorId"] = id;
            var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies") { Parent = agencysPage };
            var agencyPage2 = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage2, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View();
        }
    }
}