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
    public class BeneficiaryRelationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;

        public BeneficiaryRelationController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }

        // GET: BeneficiaryRelation
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Beneficairy Relation ")]
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult GetBeneficiaryRelations()
        {
            var data = _context.BeneficiaryRelation
                .Select(c => new
                {
                    c.BeneficiaryRelationId,
                    c.Name,
                    c.Code,
                    Updated = c.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")
                }).ToList();

            return Json(new { data });
        }
        // GET: BeneficiaryRelation/Details/5
        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1 || _context.BeneficiaryRelation == null)
            {
                notifyService.Error("Beneficiary relation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var beneficiaryRelation = await _context.BeneficiaryRelation
                .FirstOrDefaultAsync(m => m.BeneficiaryRelationId == id);
            if (beneficiaryRelation == null)
            {
                notifyService.Error("Beneficiary relation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(beneficiaryRelation);
        }

        // GET: BeneficiaryRelation/Create
        [Breadcrumb("Add  New", FromAction = "Profile")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: BeneficiaryRelation/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeneficiaryRelation beneficiaryRelation)
        {
            if (beneficiaryRelation is not null)
            {
                beneficiaryRelation.Updated = DateTime.Now;
                beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(beneficiaryRelation);
                await _context.SaveChangesAsync();
                notifyService.Success("Beneficiary relation created successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(beneficiaryRelation);
        }

        // GET: BeneficiaryRelation/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1 || _context.BeneficiaryRelation == null)
            {
                notifyService.Error("Beneficiary relation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var beneficiaryRelation = await _context.BeneficiaryRelation.FindAsync(id);
            if (beneficiaryRelation == null)
            {
                notifyService.Error("Beneficiary relation Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(beneficiaryRelation);
        }

        // POST: BeneficiaryRelation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, BeneficiaryRelation beneficiaryRelation)
        {
            if (id != beneficiaryRelation.BeneficiaryRelationId)
            {
                notifyService.Error("Beneficiary relation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            if (beneficiaryRelation is not null)
            {
                try
                {
                    beneficiaryRelation.Updated = DateTime.Now;
                    beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(beneficiaryRelation);
                    await _context.SaveChangesAsync();
                    notifyService.Warning("Beneficiary relation edited successfully!");
                    return RedirectToAction(nameof(Profile));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    notifyService.Error("Error editing Beneficiary relation!");
                    return RedirectToAction(nameof(Profile));
                }

            }
            return View(beneficiaryRelation);
        }

        // POST: BeneficiaryRelation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Beneficiary relation Not found!" });
            }
            var beneficiaryRelation = await _context.BeneficiaryRelation.FindAsync(id);
            if (beneficiaryRelation != null)
            {
                beneficiaryRelation.Updated = DateTime.Now;
                beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.BeneficiaryRelation.Remove(beneficiaryRelation);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Beneficiary relation deleted successfully!" });
        }
    }
}