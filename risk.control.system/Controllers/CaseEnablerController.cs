using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class CaseEnablerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;

        public CaseEnablerController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Reason To Verify ")]
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult GetCaseEnablers()
        {
            var data = _context.CaseEnabler
                .Select(c => new
                {
                    c.CaseEnablerId,
                    c.Name,
                    c.Code,
                    Updated = c.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")
                }).ToList();

            return Json(new { data });
        }
        // GET: CaseEnabler/Details/5
        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details(int id)
        {
            if (id < 1 || _context.CaseEnabler == null)
            {
                notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var caseEnabler = await _context.CaseEnabler
                .FirstOrDefaultAsync(m => m.CaseEnablerId == id);
            if (caseEnabler == null)
            {
                notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(caseEnabler);
        }

        // GET: CaseEnabler/Create
        [Breadcrumb("Add  New", FromAction = "Profile")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: CaseEnabler/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaseEnabler caseEnabler)
        {
            if (caseEnabler is not null)
            {
                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(caseEnabler);
                await _context.SaveChangesAsync();
                notifyService.Success("Reason created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            return View(caseEnabler);
        }

        // GET: CaseEnabler/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1 || _context.CaseEnabler == null)
            {
                notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var caseEnabler = await _context.CaseEnabler.FindAsync(id);
            if (caseEnabler == null)
            {
                notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(caseEnabler);
        }

        // POST: CaseEnabler/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, CaseEnabler caseEnabler)
        {
            if (id != caseEnabler.CaseEnablerId)
            {
                notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(caseEnabler);
                await _context.SaveChangesAsync();
                notifyService.Success("Reason edited successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("Error editing Reason!");
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: CaseEnabler/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Reason Not found!" });
            }
            var caseEnabler = await _context.CaseEnabler.FindAsync(id);
            if (caseEnabler != null)
            {
                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CaseEnabler.Remove(caseEnabler);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Reason deleted successfully!" });
        }
    }
}