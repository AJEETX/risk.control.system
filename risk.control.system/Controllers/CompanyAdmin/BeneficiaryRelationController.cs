using System.Globalization;
using System.Net;
using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class BeneficiaryRelationController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        INotyfService notifyService,
        ILogger<BeneficiaryRelationController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<BeneficiaryRelationController> _logger = logger;


        // GET: BeneficiaryRelation
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("Beneficiary Relation ")]
        public IActionResult Profile()
        {
            return View();
        }

        public async Task<IActionResult> GetBeneficiaryRelations()
        {
            var data = await _context.BeneficiaryRelation
                .Select(c => new
                {
                    BeneficiaryRelationId = _encryptionService.ProtectId(c.BeneficiaryRelationId),
                    c.Name,
                    c.Code,
                    Updated = c.Updated ?? c.Created,
                    UpdateBy = c.UpdatedBy
                }).OrderByDescending(o => o.Updated)
                .ToListAsync();

            return Json(new { data });
        }

        // GET: BeneficiaryRelation/Details/5
        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id < 1)
            {
                _notifyService.Error("Beneficiary Relation Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var beneficiaryRelation = await _context.BeneficiaryRelation.FirstOrDefaultAsync(m => m.BeneficiaryRelationId == id);
                if (beneficiaryRelation == null)
                {
                    _notifyService.Error("Beneficiary Relation Not found!");
                    return RedirectToAction(nameof(Profile));
                }
                return View(beneficiaryRelation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error to get Beneficiary Relation !");
                return RedirectToAction(nameof(Profile));
            }
        }

        // GET: BeneficiaryRelation/Create
        [Breadcrumb("Add  New", FromAction = nameof(Profile))]
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
                _notifyService.Error("Beneficiary Relation Empty!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                beneficiaryRelation.Code = beneficiaryRelation.Code!.ToUpper(CultureInfo.InvariantCulture);

                // Check for duplicate code before saving
                bool exists = await _context.BeneficiaryRelation.AnyAsync(x => x.Code == beneficiaryRelation.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Beneficiary Relation Code already exists.");
                    _notifyService.Error("Beneficiary Relation Code already exists!");
                    return View(beneficiaryRelation);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                beneficiaryRelation.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(beneficiaryRelation.Name.ToLower()));

                beneficiaryRelation.Updated = DateTime.UtcNow;
                beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(beneficiaryRelation);
                await _context.SaveChangesAsync();
                _notifyService.Success($"Beneficiary Relation Code <b>{beneficiaryRelation.Code}</b> created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error to create Beneficiary Relation !");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, [ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long? id)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            bool exists = await _context.BeneficiaryRelation.AnyAsync(x => x.Code.ToUpper() == code.ToUpper(CultureInfo.InvariantCulture) && (!id.HasValue || x.BeneficiaryRelationId != id.Value));

            return Json(exists);
        }

        // GET: BeneficiaryRelation/Edit/5
        [Breadcrumb("Edit ", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id < 1)
            {
                _notifyService.Error("Beneficiary relation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var beneficiaryRelation = await _context.BeneficiaryRelation.FindAsync(id);
            if (beneficiaryRelation == null)
            {
                _notifyService.Error("Beneficiary Relation Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(beneficiaryRelation);
        }

        // POST: BeneficiaryRelation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, BeneficiaryRelation beneficiaryRelation)
        {
            if (id < 1 || !ModelState.IsValid)
            {
                _notifyService.Error("Beneficiary Relation Null!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                beneficiaryRelation.Code = beneficiaryRelation.Code!.ToUpper(CultureInfo.InvariantCulture);

                // Check for duplicate code before saving
                bool exists = await _context.BeneficiaryRelation.AnyAsync(x => x.Code == beneficiaryRelation.Code && x.BeneficiaryRelationId != id);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Beneficiary Relation Code already exists.");
                    _notifyService.Error("Beneficiary Relation Code already exists!");
                    return View(beneficiaryRelation);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                beneficiaryRelation.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(beneficiaryRelation.Name.ToLower()));
                beneficiaryRelation.Updated = DateTime.UtcNow;
                beneficiaryRelation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(beneficiaryRelation);
                await _context.SaveChangesAsync();
                _notifyService.Custom($"Beneficiary Relation Code <b>{beneficiaryRelation.Code}</b> edited successfully!", 3, "orange", "far fa-edit");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error editing Beneficiary Relation!");
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: BeneficiaryRelation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
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
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error deleting Beneficiary Relation !");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}