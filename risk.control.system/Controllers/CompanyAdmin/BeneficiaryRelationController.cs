using System.Globalization;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class BeneficiaryRelationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<BeneficiaryRelationController> logger;

        public BeneficiaryRelationController(ApplicationDbContext context, INotyfService notifyService, ILogger<BeneficiaryRelationController> logger)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
        }

        // GET: BeneficiaryRelation
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Beneficiary Relation ")]
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
                    Updated = c.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm"),
                    UpdateBy = c.UpdatedBy
                }).ToList();

            return Json(new { data });
        }
        // GET: BeneficiaryRelation/Details/5
        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1)
            {
                notifyService.Error("Beneficiary Relation Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var beneficiaryRelation = await _context.BeneficiaryRelation.FirstOrDefaultAsync(m => m.BeneficiaryRelationId == id);
                if (beneficiaryRelation == null)
                {
                    notifyService.Error("Beneficiary Relation Not found!");
                    return RedirectToAction(nameof(Profile));
                }
                return View(beneficiaryRelation);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error to get Beneficiary Relation !");
                return RedirectToAction(nameof(Profile));
            }
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
            if (!ModelState.IsValid)
            {
                notifyService.Error("Beneficiary Relation Empty!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {

                beneficiaryRelation.Code = beneficiaryRelation.Code?.ToUpper(CultureInfo.InvariantCulture);

                // Check for duplicate code before saving
                bool exists = await _context.BeneficiaryRelation.AnyAsync(x => x.Code == beneficiaryRelation.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Beneficiary Relation Code already exists.");
                    notifyService.Error("Beneficiary Relation Code already exists!");
                    return View(beneficiaryRelation);
                }
                beneficiaryRelation.Updated = DateTime.UtcNow;
                beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(beneficiaryRelation);
                await _context.SaveChangesAsync();
                notifyService.Success("Beneficiary Relation created successfully!");
                return RedirectToAction(nameof(Profile));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error to create Beneficiary Relation !");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, long? id)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            bool exists = await _context.BeneficiaryRelation.AnyAsync(x => x.Code.ToUpper() == code.ToUpper(CultureInfo.InvariantCulture) && (!id.HasValue || x.BeneficiaryRelationId != id.Value));

            return Json(exists);
        }
        // GET: BeneficiaryRelation/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Error("Beneficiary relation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var beneficiaryRelation = await _context.BeneficiaryRelation.FindAsync(id);
            if (beneficiaryRelation == null)
            {
                notifyService.Error("Beneficiary Relation Not found!");
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
            if (id < 1 || !ModelState.IsValid)
            {
                notifyService.Error("Beneficiary Relation Null!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                beneficiaryRelation.Code = beneficiaryRelation.Code?.ToUpper(CultureInfo.InvariantCulture);

                // Check for duplicate code before saving
                bool exists = await _context.BeneficiaryRelation.AnyAsync(x => x.Code == beneficiaryRelation.Code && x.BeneficiaryRelationId != id);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Beneficiary Relation Code already exists.");
                    notifyService.Error("Beneficiary Relation Code already exists!");
                    return View(beneficiaryRelation);
                }
                beneficiaryRelation.Updated = DateTime.UtcNow;
                beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(beneficiaryRelation);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Beneficiary Relation edited successfully!", 3, "orange", "far fa-edit");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error editing Beneficiary Relation!");
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: BeneficiaryRelation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Beneficiary Relation Not found!" });
            }
            try
            {
                var beneficiaryRelation = await _context.BeneficiaryRelation.FindAsync(id);
                if (beneficiaryRelation == null)
                {
                    return Json(new { success = false, message = "Beneficiary Relation Not found!" });
                }
                beneficiaryRelation.Updated = DateTime.UtcNow;
                beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.BeneficiaryRelation.Remove(beneficiaryRelation);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Beneficiary Relation deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error deleting Beneficiary Relation !");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}