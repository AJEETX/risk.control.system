using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context; // Assuming EF Core

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult CreateForm()
        {
            var existingFields = _context.FormFields.ToList();
            return View(existingFields);
        }

        [HttpPost]
        public IActionResult CreateForm(List<FormField> fields)
        {
            if (fields != null && fields.Any())
            {
                // Optional: Clear old fields if you want this to overwrite the previous form structure
                var oldFields = _context.FormFields.ToList();
                _context.FormFields.RemoveRange(oldFields);

                // Add the new fields
                _context.FormFields.AddRange(fields);
                _context.SaveChanges();

                // Set a success flag/message for the UI
                ViewBag.SuccessMessage = "Form structure saved successfully!";
            }

            // Return the same view with the updated fields list
            return View(fields);
        }

        public IActionResult FormCreatedSuccess() => View();
    }
}
