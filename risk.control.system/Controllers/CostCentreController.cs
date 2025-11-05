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
    public class CostCentreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;

        public CostCentreController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }

        // GET: CostCentre
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Budget Centre")]
        public IActionResult Profile()
        {
            return View();
        }
        public IActionResult GetCostCentres()
        {
            var data = _context.CostCentre
                .Select(c => new
                {
                    c.CostCentreId,
                    c.Name,
                    c.Code,
                    Updated = c.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm"),
                    UpdateBy = c.UpdatedBy
                }).ToList();

            return Json(new { data });
        }
        // GET: CostCentre/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1 || _context.CostCentre == null)
            {
                notifyService.Error("Budget Centre Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var costCentre = await _context.CostCentre.FirstOrDefaultAsync(m => m.CostCentreId == id);
                if (costCentre == null)
                {
                    notifyService.Error("Budget Centre Not found!");
                    return RedirectToAction(nameof(Profile));
                }
                return View(costCentre);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("Error to get Budget Centre !");
                return RedirectToAction(nameof(Profile));
            }
        }

        // GET: CostCentre/Create
        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CostCentre costCentre)
        {
            if (costCentre is null || !ModelState.IsValid)
            {
                notifyService.Error("Budget Centre  Empty!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {

                costCentre.Code = costCentre.Code?.ToUpper();

                // Check for duplicate code before saving
                bool exists = await _context.CostCentre
                    .AnyAsync(x => x.Code == costCentre.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Budget Centre Code already exists.");
                    notifyService.Error("Budget Centre Code already exists!");
                    return View(costCentre);
                }
                costCentre.Updated = DateTime.Now;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(costCentre);
                await _context.SaveChangesAsync();
                notifyService.Success("Budget Centre created successfully!");
                return RedirectToAction(nameof(Profile));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("Error creating Budget Centre !");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, long? id)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            bool exists = await _context.CostCentre.AnyAsync(x => x.Code.ToUpper() == code.ToUpper() && (!id.HasValue || x.CostCentreId != id.Value));

            return Json(exists);
        }
        // GET: CostCentre/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("Budget Centre Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var costCentre = await _context.CostCentre.FindAsync(id);
            if (costCentre == null)
            {
                notifyService.Error("Budget Centre Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(costCentre);
        }

        // POST: CostCentre/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("CostCentreId,Name,Code,Created,Updated,UpdatedBy")] CostCentre costCentre)
        {
            if (id < 1 || !ModelState.IsValid)
            {
                notifyService.Error("Budget Centre Null!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                // Uppercase normalization
                costCentre.Code = costCentre.Code?.ToUpper();

                // Check for duplicate code before saving
                bool exists = await _context.CostCentre.AnyAsync(x => x.Code == costCentre.Code && x.CostCentreId != id);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Budget Centre Code already exists.");
                    notifyService.Error("Budget Centre Code already exists!");
                    return View(costCentre);
                }
                costCentre.Updated = DateTime.Now;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(costCentre);
                await _context.SaveChangesAsync();
                notifyService.Warning("Budget Centre edited successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("Error editing Budget Centre !");
                return RedirectToAction(nameof(Profile));
            }

        }

        // POST: CostCentre/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Budget Centre Not found!" });
            }
            try
            {
                var costCentre = await _context.CostCentre.FindAsync(id);
                if (costCentre == null)
                {
                    return Json(new { success = false, message = "Budget Centre Not found!" });
                }
                costCentre.Updated = DateTime.Now;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CostCentre.Remove(costCentre);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Budget Centre deleted successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("Error deleting Budget Centre !");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}