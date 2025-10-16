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
    public class AnnualIncomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        public AnnualIncomeController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Income Type")]
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult GetAnnualIncomes()
        {
            var data = _context.AnnualIncome
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    Updated = c.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")
                }).ToList();

            return Json(new { data });
        }
        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details(int id)
        {
            if (id < 1)
            {
                notifyService.Error("Income Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var income = await _context.AnnualIncome
                .FirstOrDefaultAsync(m => m.Id == id);
            if (income == null)
            {
                notifyService.Error("Income Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(income);
        }

        [Breadcrumb("Add  New", FromAction = "Profile")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnualIncome income)
        {
            if (income is not null)
            {
                income.Updated = DateTime.Now;
                income.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(income);
                await _context.SaveChangesAsync();
                notifyService.Success("Income Type created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            return View(income);
        }

        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Error("Income Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var income = await _context.AnnualIncome.FindAsync(id);
            if (income == null)
            {
                notifyService.Error("Income Type Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(income);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, AnnualIncome income)
        {
            if (id != income.Id)
            {
                notifyService.Error("Income Type Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                income.Updated = DateTime.Now;
                income.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(income);
                await _context.SaveChangesAsync();
                notifyService.Warning("Income Type edited successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("Error editing Income Type!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Income Type Not found!" });
            }
            var income = await _context.AnnualIncome.FindAsync(id);
            if (income != null)
            {
                income.Updated = DateTime.Now;
                income.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.AnnualIncome.Remove(income);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Income Type deleted successfully!" });
        }
    }
}
